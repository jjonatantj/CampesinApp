using System.Data;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;

using ExcelDataReader;
using UglyToad.PdfPig;

using CampesinApp.Repositories.Models;
using CampesinApp.ViewModels;
using CampesinApp.Services;

namespace CampesinApp.Controllers
{
    [Route("ImportarDatos")]
    [RolesPermitidos(RolUsuario.Administrador, Mensaje = "Acceso restringido: Solo el administrador del sistema puede importar datos del DANE y Corabastos.")]
    public class ImportarDatosController : Controller
    {
        private readonly Campesinapp3Context _context;

        public ImportarDatosController(Campesinapp3Context context)
        {
            _context = context;
        }

        // ---------------------------------------------------------
        // EQUIVALENCIAS DANE
        // La clave es el nombre de la variedad en tu base de datos.
        // El valor es el nombre exacto que aparece en el Excel.
        // ---------------------------------------------------------

        private static readonly Dictionary<string, string> MapeoDane =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["ahuyama"] = "Ahuyama",
                ["cebolla larga junca"] = "Cebolla junca",
                ["pimenton"] = "Pimentón",
                ["guayaba"] = "Guayaba*",
                ["limon comun"] = "Limón común",
                ["limon tahiti"] = "Limón Tahití",
                ["lulo"] = "Lulo",
                ["mango tommy"] = "Mango Tommy",
                ["maracuya"] = "Maracuyá",
                ["tomate de arbol"] = "Tomate de árbol",
                ["platano harton"] = "Plátano hartón verde"
            };

        // ---------------------------------------------------------
        // EQUIVALENCIAS CORABASTOS
        // Se buscan nombres concretos para evitar cruces incorrectos.
        // ---------------------------------------------------------

        private static readonly Dictionary<string, string> MapeoCorabastos =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["limon comun"] = "LIMON COMUN",
                ["limon tahiti"] = "LIMON TAHITI",

                ["naranja valencia"] = "NARANJA VALENCIA",
                ["naranja ombligona"] = "NARANJA OMBLIGONA",

                ["mandarina arrayana"] = "MANDARINA ARRAYANA",
                ["mandarina oneco"] = "MANDARINA ONECO",

                ["mango tommy"] = "MANGO TOMMY",

                ["papaya maradol"] = "PAPAYA MARADOL",
                ["papaya hawaiana"] = "PAPAYA HAWAIANA",

                ["maracuya"] = "MARACUYA",
                ["guayaba"] = "GUAYABA",
                ["lulo"] = "LULO",
                ["tomate de arbol"] = "TOMATE DE ARBOL",

                ["platano harton"] = "PLATANO HARTON",
                ["platano colicero"] = "PLATANO COLICERO",

                ["yuca armenia"] = "YUCA ARMENIA",
                ["yuca llanera"] = "YUCA LLANERA",

                ["tomate chonto"] = "TOMATE CHONTO",
                ["tomate larga vida"] = "TOMATE LARGA VIDA",

                ["cebolla cabezona roja"] = "CEBOLLA CABEZONA ROJA",
                ["cebolla larga junca"] = "CEBOLLA LARGA",

                ["pimenton"] = "PIMENTON",
                ["ahuyama"] = "AHUYAMA"
            };

        // ---------------------------------------------------------
        // PANTALLA PRINCIPAL
        // ---------------------------------------------------------

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            int? idUsuario = HttpContext.Session.GetInt32("IdUsuario");

            if (idUsuario == null)
                return RedirectToAction("Login", "InicioSesion");

            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u =>
                    u.IdUsuario == idUsuario.Value && u.Estado);

            if (usuario == null)
                return RedirectToAction("Login", "InicioSesion");

            return View("ImportarDatos", new ImportarDatosViewModel());
        }

        // ---------------------------------------------------------
        // PROCESAR ARCHIVOS Y GENERAR VISTA PREVIA
        // ---------------------------------------------------------

        [HttpPost("ProcesarArchivos")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcesarArchivos(
            ImportarDatosViewModel model)
        {
            int? idUsuario = HttpContext.Session.GetInt32("IdUsuario");

            if (idUsuario == null)
                return RedirectToAction("Login", "InicioSesion");

            // Validación en el servidor: los archivos son obligatorios aunque el navegador no lo haya comprobado.
            if (!ModelState.IsValid)
            {
                TempData["ErrorImportacion"] =
                    "Debes adjuntar los archivos del DANE y Corabastos.";

                return View("ImportarDatos", model);
            }

            if (model.ArchivoDane == null ||
                model.ArchivoCorabastos == null)
            {
                TempData["ErrorImportacion"] =
                    "Debes adjuntar los archivos del DANE y Corabastos.";

                return View("ImportarDatos", model);
            }

            string extensionDane =
                Path.GetExtension(model.ArchivoDane.FileName).ToLowerInvariant();

            string extensionCora =
                Path.GetExtension(model.ArchivoCorabastos.FileName).ToLowerInvariant();

            if (extensionDane != ".xlsx" &&
                extensionDane != ".xls" &&
                extensionDane != ".csv")
            {
                TempData["ErrorImportacion"] =
                    "El archivo del DANE debe ser Excel o CSV.";

                return View("ImportarDatos", model);
            }

            if (extensionCora != ".pdf")
            {
                TempData["ErrorImportacion"] =
                    "El archivo de Corabastos debe estar en formato PDF.";

                return View("ImportarDatos", model);
            }

            try
            {
                Encoding.RegisterProvider(
                    CodePagesEncodingProvider.Instance);

                model.EsVistaPrevia = true;
                model.ResultadosCruce = new List<DetalleCrucePrecioViewModel>();

                // Limpiar datos temporales de una carga anterior.
                TempData.Clear();

                // 1. Consultar variedades de la base de datos.

                var variedadesDb = await _context.Variedads
                    .Include(v => v.IdTipoProductoNavigation)
                    .OrderBy(v => v.IdVariedad)
                    .ToListAsync();

                // 2. Leer precios del DANE.

                Dictionary<string, decimal> preciosDane =
                    LeerPreciosDane(model.ArchivoDane, extensionDane);

                model.TotalFilasDane = preciosDane.Count;

                // 3. Leer el PDF de Corabastos.

                string contenidoCora =
                    await LeerPdfCorabastos(model.ArchivoCorabastos);

                model.TotalFilasCorabastos =
                    contenidoCora.Split(
                        new[] { '\r', '\n' },
                        StringSplitOptions.RemoveEmptyEntries
                    ).Length;

                // 4. Cruzar cada variedad de la base de datos.

                foreach (var variedad in variedadesDb)
                {
                    decimal? precioDane = null;
                    decimal? precioCora = null;

                    string nombreVariedad =
                        NormalizarTexto(variedad.VariedadProd);

                    // -----------------------------
                    // BUSCAR PRECIO DANE
                    // -----------------------------

                    if (MapeoDane.TryGetValue(
                        nombreVariedad,
                        out string? nombreDane)) // <-- Cambiado a string?
                    {
                        string claveDane = NormalizarTexto(nombreDane);

                        if (preciosDane.TryGetValue(
                            claveDane,
                            out decimal valorDane))
                        {
                            precioDane = valorDane;
                        }
                    }

                    // -----------------------------
                    // BUSCAR PRECIO CORABASTOS
                    // -----------------------------

                    if (MapeoCorabastos.TryGetValue(
                        nombreVariedad,
                        out string? nombreCorabastos)) // <-- Cambiado a string?
                    {
                        precioCora = ExtraerPrecioPorLinea(
                            contenidoCora,
                            nombreCorabastos
                        );
                    }

                    // -----------------------------
                    // AGREGAR A LA VISTA PREVIA
                    // -----------------------------

                    model.ResultadosCruce.Add(
                        new DetalleCrucePrecioViewModel
                        {
                            Producto =
                                variedad.IdTipoProductoNavigation?.Producto
                                ?? "Agrícola",

                            Variedad = variedad.VariedadProd,

                            PrecioDane = precioDane ?? 0m,

                            PrecioCorabastos = precioCora ?? 0m
                        }
                    );

                    // -----------------------------
                    // GUARDAR TEMPORALMENTE LOS PRECIOS
                    // -----------------------------

                    TempData[$"PrecioDane_{variedad.IdVariedad}"] =
                        precioDane.HasValue
                            ? Convert.ToInt32(precioDane.Value)
                                .ToString(CultureInfo.InvariantCulture)
                            : "";

                    TempData[$"PrecioCora_{variedad.IdVariedad}"] =
                        precioCora.HasValue
                            ? precioCora.Value.ToString(
                                CultureInfo.InvariantCulture)
                            : "";
                }

                TempData.Keep();

                TempData["MensajeExitoPrevia"] =
                    "Los archivos fueron procesados. Revisa los precios antes de guardar.";

                return View("ImportarDatos", model);
            }
            catch (Exception ex)
            {
                TempData["ErrorImportacion"] =
                    "Error al procesar los archivos: " + ex.Message;

                model.EsVistaPrevia = false;

                return View("ImportarDatos", model);
            }
        }

        // ---------------------------------------------------------
        // LEER EXCEL O CSV DEL DANE
        // ---------------------------------------------------------

        private Dictionary<string, decimal> LeerPreciosDane(
            IFormFile archivo,
            string extension)
        {
            var precios = new Dictionary<string, decimal>(
                StringComparer.OrdinalIgnoreCase);

            using var stream = archivo.OpenReadStream();

            using var reader = extension == ".csv"
                ? ExcelReaderFactory.CreateCsvReader(
                    stream,
                    new ExcelReaderConfiguration
                    {
                        FallbackEncoding = Encoding.UTF8
                    })
                : ExcelReaderFactory.CreateReader(stream);

            do
            {
                while (reader.Read())
                {
                    if (reader.FieldCount <= 3)
                        continue;

                    // Columna A: nombre del producto.
                    string producto =
                        Convert.ToString(
                            reader.GetValue(0),
                            CultureInfo.InvariantCulture
                        )?.Trim() ?? "";

                    // Columna D: precio Bogotá - Corabastos.
                    object valor = reader.GetValue(3);

                    if (string.IsNullOrWhiteSpace(producto) ||
                        valor == null)
                        continue;

                    if (TryParsePrecio(valor, out decimal precio) &&
                        precio > 0)
                    {
                        string clave = NormalizarTexto(producto);

                        precios[clave] = precio;
                    }
                }
            }
            while (reader.NextResult());

            return precios;
        }

        // ---------------------------------------------------------
        // LEER PDF DE CORABASTOS
        // ---------------------------------------------------------

        private async Task<string> LeerPdfCorabastos(IFormFile archivo)
        {
            using var memoryStream = new MemoryStream();

            await archivo.CopyToAsync(memoryStream);

            memoryStream.Position = 0;

            using var documentoPdf = PdfDocument.Open(memoryStream);

            var contenido = new StringBuilder();

            foreach (var pagina in documentoPdf.GetPages())
            {
                // Agrupar palabras por su posición vertical.
                var filas = pagina.GetWords()
                    .GroupBy(p => Math.Round(p.BoundingBox.Bottom, 0))
                    .OrderByDescending(g => g.Key);

                foreach (var fila in filas)
                {
                    // Ordenar las palabras de izquierda a derecha.
                    string textoFila = string.Join(" ",
                        fila.OrderBy(p => p.BoundingBox.Left)
                            .Select(p => p.Text));

                    contenido.AppendLine(textoFila);
                }
            }

            return contenido.ToString();
        }

        // ---------------------------------------------------------
        // EXTRAER PRECIO EXACTO DE UNA LÍNEA DEL PDF
        // ---------------------------------------------------------

        private decimal? ExtraerPrecioPorLinea(
            string contenidoArchivo,
            string nombreProducto)
        {
            string nombreBuscado = NormalizarTexto(nombreProducto);

            using var reader = new StringReader(contenidoArchivo);

            string? linea;

            while ((linea = reader.ReadLine()) != null)
            {
                string texto = linea.Trim();

                if (string.IsNullOrWhiteSpace(texto))
                    continue;

                string textoNormalizado = NormalizarTexto(texto);

                // La fila debe comenzar con el nombre exacto del producto.
                if (!textoNormalizado.StartsWith(
                    nombreBuscado,
                    StringComparison.OrdinalIgnoreCase))
                    continue;

                // Extraer los valores monetarios de la fila (con timeout para prevenir ReDoS - Regla SonarQube S6444).
                var precios = Regex.Matches(
                    texto,
                    @"\$\s*([\d.,]+)",
                    RegexOptions.None,
                    TimeSpan.FromMilliseconds(250)
                );

                if (precios.Count < 3)
                    continue;

                // El tercer precio es el precio por unidad:
                // calidad extra, calidad primera y precio por unidad.

                string valorTexto = precios[2]
                    .Groups[1]
                    .Value
                    .Replace(",", "")
                    .Replace(".", "");

                if (decimal.TryParse(
                    valorTexto,
                    NumberStyles.Number,
                    CultureInfo.InvariantCulture,
                    out decimal precio) && precio > 0)
                {
                    return precio;
                }
            }

            return null;
        }

        // ---------------------------------------------------------
        // CONVERTIR VALORES DEL EXCEL A DECIMAL
        // ---------------------------------------------------------

        private bool TryParsePrecio(
            object valor,
            out decimal precio)
        {
            precio = 0m;

            try
            {
                if (valor is double ||
                    valor is decimal ||
                    valor is int ||
                    valor is long)
                {
                    precio = Convert.ToDecimal(
                        valor,
                        CultureInfo.InvariantCulture);

                    return precio > 0;
                }

                string texto = valor.ToString()?.Trim() ?? "";

                if (decimal.TryParse(
                    texto,
                    NumberStyles.Any,
                    CultureInfo.InvariantCulture,
                    out precio))
                {
                    return precio > 0;
                }

                if (decimal.TryParse(
                    texto,
                    NumberStyles.Any,
                    new CultureInfo("es-CO"),
                    out precio))
                {
                    return precio > 0;
                }
            }
            catch
            {
                precio = 0m;
            }

            return false;
        }

        // ---------------------------------------------------------
        // NORMALIZAR NOMBRES
        // Quita tildes, convierte a minúsculas y limpia espacios.
        // ---------------------------------------------------------

        private string NormalizarTexto(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
                return string.Empty;

            string normalizado = texto.Normalize(
                NormalizationForm.FormD);

            var resultado = new StringBuilder();

            foreach (char caracter in normalizado)
            {
                UnicodeCategory categoria =
                    CharUnicodeInfo.GetUnicodeCategory(caracter);

                if (categoria != UnicodeCategory.NonSpacingMark)
                {
                    resultado.Append(caracter);
                }
            }

            string limpio = resultado
                .ToString()
                .Normalize(NormalizationForm.FormC)
                .ToLowerInvariant();

            // Unificar espacios repetidos (con timeout de protección ReDoS).
            limpio = Regex.Replace(limpio, @"\s+", " ", RegexOptions.None, TimeSpan.FromMilliseconds(250)).Trim();

            return limpio;
        }

        // ---------------------------------------------------------
        // CONFIRMAR Y GUARDAR EN BASE DE DATOS
        // ---------------------------------------------------------

        [HttpPost("ConfirmarCarga")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmarCarga(
            ImportarDatosViewModel? model)
        {
            int? idUsuario = HttpContext.Session.GetInt32("IdUsuario");

            if (idUsuario == null)
                return RedirectToAction("Login", "InicioSesion");

            // En este paso de confirmación, los archivos ya fueron procesados y están en TempData.
            // Removemos la validación de los archivos para que ModelState no falle por campos vacíos.
            ModelState.Remove(nameof(ImportarDatosViewModel.ArchivoDane));
            ModelState.Remove(nameof(ImportarDatosViewModel.ArchivoCorabastos));

            if (!ModelState.IsValid)
            {
                TempData["ErrorImportacion"] = "Los datos del modelo no son válidos.";
                return RedirectToAction("Index");
            }

            try
            {
                var variedadesDb = await _context.Variedads
                    .OrderBy(v => v.IdVariedad)
                    .ToListAsync();

                int registrosGuardados = 0;

                foreach (var variedad in variedadesDb)
                {
                    string claveDane =
                        $"PrecioDane_{variedad.IdVariedad}";

                    string claveCora =
                        $"PrecioCora_{variedad.IdVariedad}";

                    // Peek permite consultar TempData sin consumirlo.
                    string daneTexto =
                        TempData.Peek(claveDane)?.ToString() ?? "";

                    string coraTexto =
                        TempData.Peek(claveCora)?.ToString() ?? "";

                    int? precioDane = null;
                    decimal? precioCora = null;

                    // Convertir precio DANE.
                    if (int.TryParse(
                        daneTexto,
                        NumberStyles.Integer,
                        CultureInfo.InvariantCulture,
                        out int dane))
                    {
                        if (dane > 0)
                            precioDane = dane;
                    }

                    // Convertir precio Corabastos.
                    if (decimal.TryParse(
                        coraTexto,
                        NumberStyles.Number,
                        CultureInfo.InvariantCulture,
                        out decimal cora))
                    {
                        if (cora > 0)
                            precioCora = cora;
                    }

                    // No guardar registros sin ningún precio.
                    if (precioDane == null &&
                        precioCora == null)
                    {
                        continue;
                    }

                    var parametroDane = new SqlParameter(
                        "@precioDane",
                        SqlDbType.Int)
                    {
                        Value = (object?)precioDane ?? DBNull.Value
                    };

                    var parametroCora = new SqlParameter(
                        "@precioCorabastos",
                        SqlDbType.Decimal)
                    {
                        Precision = 18,
                        Scale = 2,
                        Value = (object?)precioCora ?? DBNull.Value
                    };

                    await _context.Database.ExecuteSqlRawAsync(
                        @"EXEC sp_RegistrarDatoDaneCorabastos
                            @idVariedad,
                            @precioDane,
                            @precioCorabastos",

                        new SqlParameter(
                            "@idVariedad",
                            variedad.IdVariedad),

                        parametroDane,
                        parametroCora
                    );

                    registrosGuardados++;
                }

                TempData["MensajeExitoFinal"] =
                    $"Carga finalizada correctamente. " +
                    $"{registrosGuardados} variedades fueron guardadas.";

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["ErrorImportacion"] =
                    "Error al guardar los precios: " + ex.Message;

                return RedirectToAction("Index");
            }
        }
    }
}