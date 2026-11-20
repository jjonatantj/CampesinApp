using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ClosedXML.Excel;
using CampesinApp.Repositories.Models;
using CampesinApp.Services;
using CampesinApp.ViewModels;

namespace CampesinApp.Controllers
{
    /// <summary>
    /// Controlador exclusivo e independiente para CU-016: Consulta de Ofertadores.
    /// Permite a Compradores, Transportadores y Administradores consultar las demandas
    /// y ver el detalle de los campesinos que han aportado productos a cada una.
    /// </summary>
    [Route("ConsultaOfertadores")]
    [RolesPermitidos(RolUsuario.Comprador, RolUsuario.Transportador, RolUsuario.Administrador, Mensaje = "Acceso restringido: Su rol no tiene acceso a la consulta de ofertadores.")]
    public class ConsultaOfertadoresController : Controller
    {
        private readonly Campesinapp3Context _context;
        private readonly ILogService _log;

        public ConsultaOfertadoresController(Campesinapp3Context context, ILogService log)
        {
            _context = context;
            _log = log;
        }

        /// <summary>
        /// Vista principal de CU-016: Lista de demandas y visualización de ofertadores.
        /// Rutas admitidas:
        /// - /ConsultaOfertadores
        /// - /ConsultaOfertadores/Index
        /// - /ConsultaOfertadores/Index?idDemanda=X
        /// </summary>
        [HttpGet("")]
        [HttpGet("Index")]
        public async Task<IActionResult> Index(int? idDemanda)
        {
            // Validación de sesión activa
            int? idSesion = HttpContext.Session.GetInt32("IdUsuario");
            string? rolSesion = HttpContext.Session.GetString("RolUsuario");

            if (idSesion == null)
            {
                TempData["MensajeError"] = "Debe iniciar sesión para acceder a la consulta de ofertadores.";
                return RedirectToAction("Login", "InicioSesion");
            }

            try
            {
                // ==============================================================
                // REGLAS DE NEGOCIO RN-01, RN-02 Y RN-03 (PERMISOS DE CONSULTA)
                // ==============================================================
                IQueryable<Demandum> query = _context.Demanda
                    .Include(d => d.IdTipoProductoNavigation)
                    .Include(d => d.IdVariedadNavigation)
                    .Include(d => d.IdEstadoDemanNavigation)
                    .Include(d => d.IdUsuarioNavigation)
                    .Include(d => d.AporteCamps)
                        .ThenInclude(a => a.IdUsuarioNavigation)
                            .ThenInclude(u => u.IdMunicipioNavigation)
                    .Include(d => d.AporteCamps)
                        .ThenInclude(a => a.IdPuntoAcopioNavigation)
                            .ThenInclude(p => p.IdMunicipioNavigation);

                if (string.Equals(rolSesion, "Comprador", StringComparison.OrdinalIgnoreCase))
                {
                    // RN-01-CU016: Comprador solo consulta demandas creadas bajo su cuenta
                    query = query.Where(d => d.IdUsuario == idSesion.Value);
                }
                else if (string.Equals(rolSesion, "Transportador", StringComparison.OrdinalIgnoreCase))
                {
                    // RN-03-CU016: Transportador solo visualiza demandas con su camión asignado
                    var camionesDelTransportador = await _context.Camions
                        .Where(c => c.IdUsuario == idSesion.Value)
                        .Select(c => c.IdCamion)
                        .ToListAsync();

                    query = query.Where(d => d.IdCamion.HasValue && camionesDelTransportador.Contains(d.IdCamion.Value));
                }
                else if (string.Equals(rolSesion, "Administrador", StringComparison.OrdinalIgnoreCase))
                {
                    // RN-02-CU016: Administrador tiene acceso global sin restricciones
                }
                else
                {
                    // El rol Campesino o Invitado no tiene precondición para consultar este módulo de administración
                    TempData["MensajeError"] = "Tu rol no tiene acceso a la consulta de ofertadores de demandas.";
                    return RedirectToAction("Index", "PantallaPrincipal");
                }

                var demandasDb = await query
                    .OrderByDescending(d => d.IdDemanda)
                    .ToListAsync();

                // Validación estricta de seguridad: si se pasa un idDemanda por URL, asegurar que pertenezca a las autorizadas
                bool esDemandaValida = idDemanda.HasValue && demandasDb.Any(d => d.IdDemanda == idDemanda.Value);

                var viewModel = new ConsultaOfertadoresViewModel
                {
                    RolUsuario = rolSesion ?? "Usuario",
                    IdDemandaSeleccionada = esDemandaValida ? idDemanda : null
                };

                foreach (var d in demandasDb)
                {
                    int totalAportado = d.AporteCamps.Sum(a => a.CantAporte);
                    int faltante = Math.Max(0, d.CantidadDeman - totalAportado);

                    var itemDemanda = new DemandaOfertadorItemViewModel
                    {
                        IdDemanda = d.IdDemanda,
                        NombreProducto = d.IdTipoProductoNavigation?.Producto ?? "Producto Agrícola",
                        Variedad = d.IdVariedadNavigation?.VariedadProd ?? "Estándar",
                        CantidadTotal = d.CantidadDeman,
                        CantidadAportada = totalAportado,
                        CantidadRestante = faltante,
                        FechaLimiteOfertar = d.LimTiemDeman,
                        FechaEntrega = d.FechaEntrega,
                        TotalOfertadores = d.AporteCamps.Count,
                        CedulaComprador = d.IdUsuarioNavigation?.CedulaUsu.ToString() ?? "",
                        NombreComprador = $"{d.IdUsuarioNavigation?.NombreCom} {d.IdUsuarioNavigation?.ApellidoCom}".Trim(),
                        EstadoDemanda = d.IdEstadoDemanNavigation?.Estado ?? (faltante == 0 ? "Completada" : "Publicada")
                    };

                    // Mapeo de campesinos ofertadores
                    foreach (var ap in d.AporteCamps)
                    {
                        string ubicacion = "No especificada";
                        if (ap.IdPuntoAcopioNavigation != null)
                        {
                            string municipioPunto = ap.IdPuntoAcopioNavigation.IdMunicipioNavigation?.Municipio1 ?? "";
                            ubicacion = string.IsNullOrWhiteSpace(municipioPunto)
                                ? ap.IdPuntoAcopioNavigation.NombrePunto
                                : $"{ap.IdPuntoAcopioNavigation.NombrePunto} ({municipioPunto})";
                        }
                        else if (ap.IdUsuarioNavigation?.IdMunicipioNavigation != null)
                        {
                            ubicacion = ap.IdUsuarioNavigation.IdMunicipioNavigation.Municipio1;
                        }

                        itemDemanda.Ofertadores.Add(new OfertadorDetalleViewModel
                        {
                            IdAporte = ap.IdAporte,
                            IdCampesino = ap.IdUsuario,
                            NombreCampesino = $"{ap.IdUsuarioNavigation?.NombreCom} {ap.IdUsuarioNavigation?.ApellidoCom}".Trim(),
                            CantidadAportada = ap.CantAporte,
                            UbicacionOrigen = ubicacion
                        });
                    }

                    viewModel.Demandas.Add(itemDemanda);
                }

                return View("Index", viewModel);
            }
            catch (Exception ex)
            {
                // FE-02-CU016: Error de conexión a la base de datos
                await _log.RegistrarAsync(NivelesLog.Error, "CU-016 Consulta Ofertadores", $"{ex.GetType().Name}: {ex.Message}", idSesion);
                TempData["MensajeError"] = "No fue posible cargar la información. Intente nuevamente.";
                return RedirectToAction("Index", "PantallaPrincipal");
            }
        }

        /// <summary>
        /// Exportación a Excel nativo (.xlsx) estructurado, con logo oficial y estilos institucionales (FA-02-CU016).
        /// Resuelve la advertencia de formato de Excel, incrusta la imagen nativamente y garantiza los valores numéricos exactos.
        /// </summary>
        [HttpGet("ExportarExcel")]
        public async Task<IActionResult> ExportarExcel(int idDemanda, int? cantidadMinima)
        {
            int? idSesion = HttpContext.Session.GetInt32("IdUsuario");
            string? rolSesion = HttpContext.Session.GetString("RolUsuario");

            if (idSesion == null)
            {
                return RedirectToAction("Login", "InicioSesion");
            }

            var demanda = await _context.Demanda
                .Include(d => d.IdTipoProductoNavigation)
                .Include(d => d.IdVariedadNavigation)
                .Include(d => d.AporteCamps)
                    .ThenInclude(a => a.IdUsuarioNavigation)
                        .ThenInclude(u => u.IdMunicipioNavigation)
                .Include(d => d.AporteCamps)
                    .ThenInclude(a => a.IdPuntoAcopioNavigation)
                        .ThenInclude(p => p.IdMunicipioNavigation)
                .FirstOrDefaultAsync(d => d.IdDemanda == idDemanda);

            if (demanda == null)
            {
                return NotFound();
            }

            // Validación de permisos según rol (RN-01, RN-02, RN-03)
            if (string.Equals(rolSesion, "Comprador", StringComparison.OrdinalIgnoreCase) && demanda.IdUsuario != idSesion.Value)
            {
                return Forbid();
            }
            else if (string.Equals(rolSesion, "Transportador", StringComparison.OrdinalIgnoreCase))
            {
                bool camionAsignado = await _context.Camions.AnyAsync(c => c.IdUsuario == idSesion.Value && c.IdCamion == demanda.IdCamion);
                if (!camionAsignado) return Forbid();
            }

            // Filtrar aportes si el usuario aplicó filtro por cantidad mayor a X (FA-01)
            var aportesFiltrados = demanda.AporteCamps.AsEnumerable();
            if (cantidadMinima.HasValue && cantidadMinima.Value >= 0)
            {
                aportesFiltrados = aportesFiltrados.Where(a => a.CantAporte > cantidadMinima.Value);
            }
            var listaAportes = aportesFiltrados.ToList();

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add($"Demanda #{idDemanda}");
            ws.ShowGridLines = true;

            // 1. Insertar logo oficial si existe en wwwroot
            string rutaLogo = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "logo.jpeg");
            if (System.IO.File.Exists(rutaLogo))
            {
                try
                {
                    ws.AddPicture(rutaLogo)
                        .MoveTo(ws.Cell("A1"), 5, 5)
                        .WithSize(80, 80);
                }
                catch { }
            }

            // Alturas de filas iniciales para el logo
            ws.Row(1).Height = 22;
            ws.Row(2).Height = 20;
            ws.Row(3).Height = 18;
            ws.Row(4).Height = 15;

            // 2. Encabezado Institucional
            ws.Cell("B1").Value = "CAMPESINAPP";
            ws.Cell("B1").Style.Font.Bold = true;
            ws.Cell("B1").Style.Font.FontSize = 16;
            ws.Cell("B1").Style.Font.FontColor = XLColor.FromHtml("#2e7d32");

            ws.Cell("B2").Value = "Reporte Consolidado de Campesinos Ofertadores";
            ws.Cell("B2").Style.Font.Bold = true;
            ws.Cell("B2").Style.Font.FontSize = 11;
            ws.Cell("B2").Style.Font.FontColor = XLColor.FromHtml("#424242");

            ws.Cell("B3").Value = $"Fecha de generación: {DateTime.Now:dd/MM/yyyy HH:mm}";
            ws.Cell("B3").Style.Font.FontSize = 9;
            ws.Cell("B3").Style.Font.FontColor = XLColor.FromHtml("#757575");

            // 3. Tarjeta de Información de la Demanda
            int fila = 5;
            ws.Range(fila, 1, fila, 4).Merge();
            ws.Cell(fila, 1).Value = "INFORMACIÓN DE LA DEMANDA";
            ws.Cell(fila, 1).Style.Font.Bold = true;
            ws.Cell(fila, 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#2e7d32");
            ws.Cell(fila, 1).Style.Font.FontColor = XLColor.White;
            ws.Row(fila).Height = 24;
            ws.Cell(fila, 1).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

            int totalAportado = demanda.AporteCamps.Sum(a => a.CantAporte);
            int faltante = Math.Max(0, demanda.CantidadDeman - totalAportado);

            void AgregarFilaResumen(int numFila, string etq1, string val1, string etq2, string val2)
            {
                ws.Cell(numFila, 1).Value = etq1;
                ws.Cell(numFila, 1).Style.Font.Bold = true;
                ws.Cell(numFila, 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#e8f5e9");
                ws.Cell(numFila, 1).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                ws.Cell(numFila, 1).Style.Border.OutsideBorderColor = XLColor.FromHtml("#c8e6c9");

                ws.Cell(numFila, 2).Value = val1;
                ws.Cell(numFila, 2).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                ws.Cell(numFila, 2).Style.Border.OutsideBorderColor = XLColor.FromHtml("#c8e6c9");

                ws.Cell(numFila, 3).Value = etq2;
                ws.Cell(numFila, 3).Style.Font.Bold = true;
                ws.Cell(numFila, 3).Style.Fill.BackgroundColor = XLColor.FromHtml("#e8f5e9");
                ws.Cell(numFila, 3).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                ws.Cell(numFila, 3).Style.Border.OutsideBorderColor = XLColor.FromHtml("#c8e6c9");

                ws.Cell(numFila, 4).Value = val2;
                ws.Cell(numFila, 4).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                ws.Cell(numFila, 4).Style.Border.OutsideBorderColor = XLColor.FromHtml("#c8e6c9");
                ws.Row(numFila).Height = 20;
            }

            fila++;
            AgregarFilaResumen(fila, "N° Demanda:", $"#{demanda.IdDemanda}", "Cantidad Solicitada:", $"{demanda.CantidadDeman:N0} Kg");
            fila++;
            AgregarFilaResumen(fila, "Producto:", demanda.IdTipoProductoNavigation?.Producto ?? "Agrícola", "Cantidad Aportada:", $"{totalAportado:N0} Kg");
            fila++;
            AgregarFilaResumen(fila, "Variedad:", demanda.IdVariedadNavigation?.VariedadProd ?? "Estándar", "Cantidad Restante:", $"{faltante:N0} Kg");
            fila++;
            AgregarFilaResumen(fila, "Límite Ofertar:", demanda.LimTiemDeman.ToString("dd/MM/yyyy"), "Fecha Entrega:", demanda.FechaEntrega.ToString("dd/MM/yyyy"));

            // 4. Cabecera de la Tabla de Ofertadores
            fila += 2;
            ws.Range(fila, 1, fila, 4).Merge();
            ws.Cell(fila, 1).Value = "LISTADO DE CAMPESINOS POSTULADOS";
            ws.Cell(fila, 1).Style.Font.Bold = true;
            ws.Cell(fila, 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#2e7d32");
            ws.Cell(fila, 1).Style.Font.FontColor = XLColor.White;
            ws.Row(fila).Height = 24;
            ws.Cell(fila, 1).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

            fila++;
            string[] columnas = { "N°", "Nombre del Campesino", "Cantidad Aportada (Kg)", "Ubicación Origen / Centro de Acopio" };
            for (int c = 0; c < columnas.Length; c++)
            {
                var celda = ws.Cell(fila, c + 1);
                celda.Value = columnas[c];
                celda.Style.Font.Bold = true;
                celda.Style.Fill.BackgroundColor = XLColor.FromHtml("#388e3c");
                celda.Style.Font.FontColor = XLColor.White;
                celda.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                celda.Style.Alignment.Horizontal = c == 0 || c == 2 ? XLAlignmentHorizontalValues.Center : XLAlignmentHorizontalValues.Left;
                celda.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                celda.Style.Border.OutsideBorderColor = XLColor.FromHtml("#1b5e20");
            }
            ws.Row(fila).Height = 28;

            // 5. Filas de Ofertadores (con valor numérico puro y exacto)
            int itemIndex = 1;
            int totalKilosReporte = 0;

            foreach (var ap in listaAportes)
            {
                fila++;
                totalKilosReporte += ap.CantAporte;

                string ubicacion = "No especificada";
                if (ap.IdPuntoAcopioNavigation != null)
                {
                    string municipio = ap.IdPuntoAcopioNavigation.IdMunicipioNavigation?.Municipio1 ?? "";
                    ubicacion = string.IsNullOrWhiteSpace(municipio)
                        ? ap.IdPuntoAcopioNavigation.NombrePunto
                        : $"{ap.IdPuntoAcopioNavigation.NombrePunto} ({municipio})";
                }
                else if (ap.IdUsuarioNavigation?.IdMunicipioNavigation != null)
                {
                    ubicacion = ap.IdUsuarioNavigation.IdMunicipioNavigation.Municipio1;
                }

                string nombreCampesino = $"{ap.IdUsuarioNavigation?.NombreCom} {ap.IdUsuarioNavigation?.ApellidoCom}".Trim();

                // N°
                ws.Cell(fila, 1).Value = itemIndex++;
                ws.Cell(fila, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                // Nombre
                ws.Cell(fila, 2).Value = nombreCampesino;

                // Cantidad Aportada: Guardar el número exacto (ej. 2000) formateado con unidad Kg
                var celdaAporte = ws.Cell(fila, 3);
                celdaAporte.Value = ap.CantAporte;
                celdaAporte.Style.NumberFormat.Format = "#,##0 \"Kg\"";
                celdaAporte.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                celdaAporte.Style.Font.Bold = true;

                // Ubicación
                ws.Cell(fila, 4).Value = ubicacion;

                for (int c = 1; c <= 4; c++)
                {
                    ws.Cell(fila, c).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    ws.Cell(fila, c).Style.Border.OutsideBorderColor = XLColor.FromHtml("#e0e0e0");
                }
                ws.Row(fila).Height = 20;
            }

            // 6. Fila de Total
            fila++;
            ws.Range(fila, 1, fila, 2).Merge();
            ws.Cell(fila, 1).Value = "TOTAL APORTADO POR ESTOS OFERTADORES:";
            ws.Cell(fila, 1).Style.Font.Bold = true;
            ws.Cell(fila, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            ws.Cell(fila, 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#e8f5e9");

            var celdaGranTotal = ws.Cell(fila, 3);
            celdaGranTotal.Value = totalKilosReporte;
            celdaGranTotal.Style.NumberFormat.Format = "#,##0 \"Kg\"";
            celdaGranTotal.Style.Font.Bold = true;
            celdaGranTotal.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            celdaGranTotal.Style.Fill.BackgroundColor = XLColor.FromHtml("#e8f5e9");
            celdaGranTotal.Style.Font.FontColor = XLColor.FromHtml("#1b5e20");

            ws.Cell(fila, 4).Value = $"Total: {listaAportes.Count} campesino(s)";
            ws.Cell(fila, 4).Style.Fill.BackgroundColor = XLColor.FromHtml("#e8f5e9");
            ws.Cell(fila, 4).Style.Font.Italic = true;

            for (int c = 1; c <= 4; c++)
            {
                ws.Cell(fila, c).Style.Border.TopBorder = XLBorderStyleValues.Double;
                ws.Cell(fila, c).Style.Border.BottomBorder = XLBorderStyleValues.Double;
                ws.Cell(fila, c).Style.Border.TopBorderColor = XLColor.FromHtml("#2e7d32");
                ws.Cell(fila, c).Style.Border.BottomBorderColor = XLColor.FromHtml("#2e7d32");
            }
            ws.Row(fila).Height = 24;

            // Ajuste de anchos amplios de columnas para que todo el texto quepa holgadamente
            ws.Column(1).Width = 18;
            ws.Column(2).Width = 36;
            ws.Column(3).Width = 28;
            ws.Column(4).Width = 48;

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            string fechaHoy = DateTime.Now.ToString("yyyyMMdd");
            return File(stream.ToArray(), 
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", 
                        $"Reporte_Ofertadores_Demanda_{idDemanda}_{fechaHoy}.xlsx");
        }
    }
}

