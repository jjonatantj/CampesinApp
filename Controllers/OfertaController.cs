using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CampesinApp.Repositories.Models;
using CampesinApp.ViewModels;
using CampesinApp.Services;

namespace CampesinApp.Controllers
{
    /// <summary>
    /// Controlador para el Caso de Uso CU-013: Registro de Producto (Oferta a una demanda).
    /// Permite a los campesinos responder a las solicitudes de compra ofertando sus productos agrícolas.
    /// 
    /// DIRECTRICES Y REGLAS DE NEGOCIO IMPLEMENTADAS:
    /// - RNF-01: Protección de sesión y verificación estricta del rol 'Campesino'.
    /// - RN-01-CU013: La cantidad aportada no puede superar la cantidad faltante.
    /// - RN-02 y RN-03: Si el aporte cubre el 100%, se actualiza el estado de la demanda a 'Pedido Completo'.
    /// - RN-04-CU013: La cantidad debe ser igual o mayor al 10% del total demandado.
    /// - RN-05-CU013: Solo se permiten números enteros mayores a cero.
    /// - RN-06-CU013: Solo se permiten formatos de imagen JPG, JPEG, PNG y WebP con verificación en servidor.
    /// - FE01-FE05: Flujos de excepción con mensajes estandarizados.
    /// </summary>
    [Route("[controller]/[action]")]
    [Route("RegistroProducto/[action]")]
    public class OfertaController : Controller
    {
        private readonly Campesinapp3Context _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly NotificacionService _notificacionService;
        private readonly ILogService _logService;
        private readonly IConfiguration _configuration;

        // Inyección de dependencias por constructor
        public OfertaController(
            Campesinapp3Context context,
            IWebHostEnvironment webHostEnvironment,
            NotificacionService notificacionService,
            ILogService logService,
            IConfiguration configuration)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
            _notificacionService = notificacionService;
            _logService = logService;
            _configuration = configuration;
        }

        /// <summary>
        /// Carga los puntos de acopio disponibles para que el campesino elija dónde dejará su producto.
        /// Incluye sus propios puntos registrados y todos los puntos de campesinos.
        /// </summary>
        private async Task CargarPuntosAcopioParaOfertaAsync(OfertaProductoViewModel model, int idUsuarioCampesino)
        {
            model.GoogleMapsApiKey = _configuration["GoogleMaps:ApiKey"] ?? string.Empty;

            var puntos = await _context.PuntoAcopios
                .Include(p => p.IdMunicipioNavigation)
                    .ThenInclude(m => m.IdDepartamentoNavigation)
                .Where(p => p.TipoAcopio == false || p.IdUsuario == idUsuarioCampesino)
                .AsNoTracking()
                .OrderByDescending(p => p.IdUsuario == idUsuarioCampesino)
                .ThenBy(p => p.NombrePunto)
                .ToListAsync();

            model.PuntosAcopioDisponibles = puntos.Select(p => new PuntoAcopioItemViewModel
            {
                IdPuntoAcopio = p.IdPuntoAcopio,
                NombrePunto = p.NombrePunto,
                Direccion = p.Direccion,
                Municipio = p.IdMunicipioNavigation?.Municipio1 ?? "Sin municipio",
                Departamento = p.IdMunicipioNavigation?.IdDepartamentoNavigation?.Departamento1 ?? "Colombia",
                Telefono = p.Telefono,
                HorarioAtencion = p.HorarioAtencion,
                Latitud = p.Latitud,
                Longitud = p.Longitud,
                Capacidad = p.Capacidad,
                AccesoVehicular = p.AccesoVehicular,
                Descripcion = p.Descripcion,
                TipoAcopio = p.TipoAcopio,
                EsPropio = (p.IdUsuario == idUsuarioCampesino)
            }).ToList();
        }

        // =========================================================================
        // ACCIÓN GET: Mostrar el formulario de registro de oferta (Pasos 1 y 2 CU-013)
        // =========================================================================
        [HttpGet]
        public async Task<IActionResult> Registrar(int idDemanda)
        {
            // 1. Directriz de Seguridad RNF-01: Validar sesión y rol Campesino
            if (!ValidarCampesinoAutenticado(out int idUsuarioCampesino, out IActionResult? resultadoSeguridad))
            {
                return resultadoSeguridad!;
            }

            // 2. Validación de parámetro de entrada
            if (idDemanda <= 0)
            {
                TempData["MensajeError"] = "Identificador de demanda no válido.";
                return RedirectToAction("Index", "catalogo");
            }

            try
            {
                // 3. Consultar la demanda y sus datos relacionados
                var demanda = await _context.Demanda
                    .Include(d => d.IdTipoProductoNavigation)
                    .Include(d => d.IdVariedadNavigation)
                    .Include(d => d.IdPuntoAcopioNavigation)
                        .ThenInclude(p => p!.IdMunicipioNavigation)
                            .ThenInclude(m => m!.IdDepartamentoNavigation)
                    .Include(d => d.IdUsuarioNavigation)
                    .FirstOrDefaultAsync(d => d.IdDemanda == idDemanda);

                // FE04-CU013: Demanda inexistente o error en consulta
                if (demanda == null)
                {
                    TempData["MensajeError"] = "No fue posible consultar los datos de la demanda. Intente nuevamente.";
                    return RedirectToAction("Index", "catalogo");
                }

                // 4. Calcular cantidades acumuladas y faltantes
                int totalAportado = await _context.AporteCamps
                    .Where(a => a.IdDemanda == idDemanda)
                    .SumAsync(a => a.CantAporte);

                int cantidadFaltante = Math.Max(0, demanda.CantidadDeman - totalAportado);

                // FE02-CU013: La solicitud ya fue completada
                if (cantidadFaltante <= 0)
                {
                    TempData["MensajeError"] = "Lo sentimos, esta solicitud de compra acaba de completarse. Te invitamos a revisar otras ofertas.";
                    return RedirectToAction("Index", "catalogo");
                }

                // RN-03-CU011 / CU013: Validar que el plazo límite de oferta no haya vencido
                if (DateTime.Today > demanda.LimTiemDeman.Date)
                {
                    TempData["MensajeError"] = "Lo sentimos, el plazo límite para ofertar en esta demanda ha finalizado. Te invitamos a revisar otras ofertas.";
                    return RedirectToAction("Index", "catalogo");
                }

                // Consultar aportes previos que este mismo campesino ya realizó a esta demanda
                var aportePrevio = await _context.AporteCamps
                    .Include(a => a.IdPuntoAcopioNavigation)
                        .ThenInclude(p => p!.IdMunicipioNavigation)
                            .ThenInclude(m => m!.IdDepartamentoNavigation)
                    .Where(a => a.IdDemanda == idDemanda && a.IdUsuario == idUsuarioCampesino)
                    .OrderByDescending(a => a.IdAporte)
                    .FirstOrDefaultAsync();

                int aportePrevioCampesino = await _context.AporteCamps
                    .Where(a => a.IdDemanda == idDemanda && a.IdUsuario == idUsuarioCampesino)
                    .SumAsync(a => a.CantAporte);

                // RN-04-CU013: Cálculo del 10% mínimo permitido del total demandado
                int topeDiezPorciento = (int)Math.Ceiling(demanda.CantidadDeman * 0.10);

                // REGLA FLEXIBLE ACUMULATIVA:
                // Si el campesino ya aportó previamente el 10% (o más), puede ofertar cualquier cantidad entera desde 1 unidad.
                // Si aún no alcanza el 10%, debe ofertar al menos lo que falte para que su aporte acumulado alcance el 10%.
                int minimoPermitido;
                if (aportePrevioCampesino >= topeDiezPorciento)
                {
                    minimoPermitido = 1;
                }
                else
                {
                    minimoPermitido = Math.Max(1, topeDiezPorciento - aportePrevioCampesino);
                }

                // Si lo que falta para completar la demanda es menor al mínimo, se permite aportar desde 1 unidad
                if (minimoPermitido > cantidadFaltante)
                {
                    minimoPermitido = 1;
                }

                // 5. Construir el ViewModel para la vista
                var viewModel = new OfertaProductoViewModel
                {
                    IdDemanda = demanda.IdDemanda,
                    NombreProducto = demanda.IdTipoProductoNavigation?.Producto ?? "Producto agrícola",
                    VariedadProducto = demanda.IdVariedadNavigation?.VariedadProd ?? "Estándar",
                    Descripcion = demanda.DescripcionDeman,
                    CantidadTotal = demanda.CantidadDeman,
                    CantidadAportadaPrevia = totalAportado,
                    CantidadFaltante = cantidadFaltante,
                    TopeDiezPorciento = topeDiezPorciento,
                    AportePrevioCampesino = aportePrevioCampesino,
                    MinimoPermitido = minimoPermitido,
                    PrecioPropuesto = demanda.PrecioCampesino ?? demanda.PrecioDeman,
                    MunicipioEntrega = demanda.IdPuntoAcopioNavigation?.IdMunicipioNavigation?.Municipio1 ?? "Punto de acopio",
                    DepartamentoEntrega = demanda.IdPuntoAcopioNavigation?.IdMunicipioNavigation?.IdDepartamentoNavigation?.Departamento1 ?? "Colombia",
                    DireccionEntrega = demanda.IdPuntoAcopioNavigation?.Direccion ?? demanda.DireccionInicial,
                    CompradorNombre = $"{demanda.IdUsuarioNavigation?.NombreCom} {demanda.IdUsuarioNavigation?.ApellidoCom}".Trim()
                };

                if (aportePrevio != null && aportePrevio.IdPuntoAcopio.HasValue && aportePrevio.IdPuntoAcopio.Value > 0)
                {
                    viewModel.IdPuntoAcopioFijo = aportePrevio.IdPuntoAcopio.Value;
                    viewModel.IdPuntoAcopio = aportePrevio.IdPuntoAcopio.Value;
                    viewModel.NombrePuntoAcopioFijo = aportePrevio.IdPuntoAcopioNavigation?.NombrePunto ?? $"Punto #{aportePrevio.IdPuntoAcopio.Value}";
                    viewModel.DetallePuntoAcopioFijo = $"{aportePrevio.IdPuntoAcopioNavigation?.Direccion} ({aportePrevio.IdPuntoAcopioNavigation?.IdMunicipioNavigation?.Municipio1}, {aportePrevio.IdPuntoAcopioNavigation?.IdMunicipioNavigation?.IdDepartamentoNavigation?.Departamento1})";
                }

                await CargarPuntosAcopioParaOfertaAsync(viewModel, idUsuarioCampesino);
                return View(viewModel);
            }
            catch (Exception)
            {
                // FE04-CU013: Error de conexión a la base de datos
                TempData["MensajeError"] = "No fue posible consultar los datos de la demanda. Intente nuevamente.";
                return RedirectToAction("Index", "catalogo");
            }
        }

        // =========================================================================
        // ACCIÓN POST: Procesar la oferta del producto (Pasos 3, 4, 5 y 6 CU-013)
        // =========================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Registrar(OfertaProductoViewModel model)
        {
            // 1. Directriz de Seguridad RNF-01: Validar sesión y rol Campesino
            if (!ValidarCampesinoAutenticado(out int idUsuarioCampesino, out IActionResult? resultadoSeguridad))
            {
                return resultadoSeguridad!;
            }

            // 2. Consultar la demanda en tiempo real para validar estado y concurrencia
            var demanda = await _context.Demanda
                .Include(d => d.IdTipoProductoNavigation)
                .Include(d => d.IdVariedadNavigation)
                .Include(d => d.IdPuntoAcopioNavigation)
                    .ThenInclude(p => p!.IdMunicipioNavigation)
                        .ThenInclude(m => m!.IdDepartamentoNavigation)
                .Include(d => d.IdUsuarioNavigation)
                .FirstOrDefaultAsync(d => d.IdDemanda == model.IdDemanda);

            if (demanda == null)
            {
                TempData["MensajeError"] = "No fue posible consultar los datos de la demanda. Intente nuevamente.";
                return RedirectToAction("Index", "catalogo");
            }

            // Calcular cantidades actuales en base de datos
            int totalAportadoActual = await _context.AporteCamps
                .Where(a => a.IdDemanda == model.IdDemanda)
                .SumAsync(a => a.CantAporte);

            int cantidadFaltanteActual = Math.Max(0, demanda.CantidadDeman - totalAportadoActual);

            // Consultar aportes previos que este mismo campesino ya realizó a esta demanda
            var aportePrevio = await _context.AporteCamps
                .Include(a => a.IdPuntoAcopioNavigation)
                    .ThenInclude(p => p!.IdMunicipioNavigation)
                        .ThenInclude(m => m!.IdDepartamentoNavigation)
                .Where(a => a.IdDemanda == model.IdDemanda && a.IdUsuario == idUsuarioCampesino)
                .OrderByDescending(a => a.IdAporte)
                .FirstOrDefaultAsync();

            int aportePrevioCampesino = await _context.AporteCamps
                .Where(a => a.IdDemanda == model.IdDemanda && a.IdUsuario == idUsuarioCampesino)
                .SumAsync(a => a.CantAporte);

            // RN-04-CU013: Tope general del 10% del total demandado
            int topeDiezPorciento = (int)Math.Ceiling(demanda.CantidadDeman * 0.10);

            // REGLA FLEXIBLE ACUMULATIVA:
            // Si el campesino ya aportó previamente el 10% (o más), puede ofertar cualquier cantidad entera desde 1 unidad.
            // Si aún no alcanza el 10%, debe ofertar al menos lo que falte para que su aporte acumulado alcance el 10%.
            int minimoPermitido;
            if (aportePrevioCampesino >= topeDiezPorciento)
            {
                minimoPermitido = 1;
            }
            else
            {
                minimoPermitido = Math.Max(1, topeDiezPorciento - aportePrevioCampesino);
            }

            if (minimoPermitido > cantidadFaltanteActual)
            {
                minimoPermitido = 1;
            }

            // Re-poblar datos informativos del ViewModel en caso de retornar la vista
            model.NombreProducto = demanda.IdTipoProductoNavigation?.Producto ?? "Producto agrícola";
            model.VariedadProducto = demanda.IdVariedadNavigation?.VariedadProd ?? "Estándar";
            model.Descripcion = demanda.DescripcionDeman;
            model.CantidadTotal = demanda.CantidadDeman;
            model.CantidadAportadaPrevia = totalAportadoActual;
            model.CantidadFaltante = cantidadFaltanteActual;
            model.PrecioPropuesto = demanda.PrecioCampesino ?? demanda.PrecioDeman;
            model.MunicipioEntrega = demanda.IdPuntoAcopioNavigation?.IdMunicipioNavigation?.Municipio1 ?? "Punto de acopio";
            model.DepartamentoEntrega = demanda.IdPuntoAcopioNavigation?.IdMunicipioNavigation?.IdDepartamentoNavigation?.Departamento1 ?? "Colombia";
            model.DireccionEntrega = demanda.IdPuntoAcopioNavigation?.Direccion ?? demanda.DireccionInicial;
            model.CompradorNombre = $"{demanda.IdUsuarioNavigation?.NombreCom} {demanda.IdUsuarioNavigation?.ApellidoCom}".Trim();
            model.TopeDiezPorciento = topeDiezPorciento;
            model.AportePrevioCampesino = aportePrevioCampesino;
            model.MinimoPermitido = minimoPermitido;

            // FE02-CU013: Solicitud completada justo antes por otro campesino
            if (cantidadFaltanteActual <= 0)
            {
                TempData["MensajeError"] = "Lo sentimos, esta solicitud de compra acaba de completarse. Te invitamos a revisar otras ofertas.";
                return RedirectToAction("Index", "catalogo");
            }

            // RN-03-CU011 / CU013: Validar que el plazo límite de oferta no haya vencido
            if (DateTime.Today > demanda.LimTiemDeman.Date)
            {
                TempData["MensajeError"] = "Lo sentimos, el plazo límite para ofertar en esta demanda ha finalizado. Te invitamos a revisar otras ofertas.";
                return RedirectToAction("Index", "catalogo");
            }

            // 3. REGLA DE NEGOCIO RN-05-CU013 (y FE05): Cantidad debe ser entero mayor a cero
            if (!model.CantidadAporte.HasValue || model.CantidadAporte.Value <= 0)
            {
                ModelState.AddModelError("CantidadAporte", "Por favor ingrese solo números enteros mayores a cero en el campo de cantidad ofertada.");
            }

            if (model.CantidadAporte.HasValue)
            {
                int cantidad = model.CantidadAporte.Value;

                // REGLA DE NEGOCIO RN-04-CU013 (y FE03):
                // Si el campesino ya aportó previamente >= 10%, puede ofertar desde 1 unidad.
                // Si es su primer aporte o aún no acumula el 10%, debe aportar al menos el mínimo calculado.
                if (cantidad < minimoPermitido)
                {
                    if (aportePrevioCampesino >= topeDiezPorciento)
                    {
                        ModelState.AddModelError("CantidadAporte", "Debe registrar al menos 1 unidad para su aporte.");
                    }
                    else
                    {
                        ModelState.AddModelError("CantidadAporte", $"La cantidad ingresada es menor al mínimo permitido para esta oferta. Debes registrar al menos {minimoPermitido} unidades para alcanzar el 10% del total demandado.");
                    }
                }

                // REGLA DE NEGOCIO RN-01-CU013 (y FE01): No puede superar la cantidad faltante
                if (cantidad > cantidadFaltanteActual)
                {
                    ModelState.AddModelError("CantidadAporte", $"La cantidad ingresada supera lo solicitado. Solo puedes registrar hasta {cantidadFaltanteActual} cantidades para esta compra.");
                }
            }

            // 4. REGLA DE NEGOCIO RN-06-CU013: Validación estricta del archivo de imagen
            string nombreArchivoImagen = string.Empty;
            if (model.ImagenProducto == null || model.ImagenProducto.Length == 0)
            {
                ModelState.AddModelError("ImagenProducto", "Debe adjuntar una imagen del producto que va a ofertar.");
            }
            else
            {
                // Extensiones permitidas según RN-06
                var extensionesPermitidas = new[] { ".jpg", ".jpeg", ".png", ".webp" };
                var extension = Path.GetExtension(model.ImagenProducto.FileName).ToLowerInvariant();

                if (!extensionesPermitidas.Contains(extension))
                {
                    ModelState.AddModelError("ImagenProducto", "Los únicos formatos de documentos para las imágenes del producto serán JPG, JPEG, PNG y WebP.");
                }

                // Validación de seguridad adicional: ContentType válido
                var mimesPermitidos = new[] { "image/jpeg", "image/png", "image/webp", "image/pjpeg", "image/x-png" };
                if (!mimesPermitidos.Contains(model.ImagenProducto.ContentType.ToLowerInvariant()))
                {
                    ModelState.AddModelError("ImagenProducto", "El formato del archivo cargado no corresponde a una imagen válida.");
                }

                // Validación de tamaño máximo (5 MB)
                if (model.ImagenProducto.Length > 5 * 1024 * 1024)
                {
                    ModelState.AddModelError("ImagenProducto", "La imagen no debe superar los 5 MB de tamaño.");
                }
            }

            // REGLA DE NEGOCIO: Si ya registró previamente en esta demanda, debe usar el mismo punto de acopio
            if (aportePrevio != null && aportePrevio.IdPuntoAcopio.HasValue && aportePrevio.IdPuntoAcopio.Value > 0)
            {
                model.IdPuntoAcopioFijo = aportePrevio.IdPuntoAcopio.Value;
                model.NombrePuntoAcopioFijo = aportePrevio.IdPuntoAcopioNavigation?.NombrePunto ?? $"Punto #{aportePrevio.IdPuntoAcopio.Value}";
                model.DetallePuntoAcopioFijo = $"{aportePrevio.IdPuntoAcopioNavigation?.Direccion} ({aportePrevio.IdPuntoAcopioNavigation?.IdMunicipioNavigation?.Municipio1}, {aportePrevio.IdPuntoAcopioNavigation?.IdMunicipioNavigation?.IdDepartamentoNavigation?.Departamento1})";

                if (model.IdPuntoAcopio.HasValue && model.IdPuntoAcopio.Value != aportePrevio.IdPuntoAcopio.Value)
                {
                    ModelState.AddModelError("IdPuntoAcopio", $"Al añadir más producto a esta solicitud, debes usar el mismo punto de acopio de tu registro anterior: '{model.NombrePuntoAcopioFijo}'.");
                }
                // Fijamos el punto previo para asegurar coherencia
                model.IdPuntoAcopio = aportePrevio.IdPuntoAcopio.Value;
            }
            else
            {
                // Si es su primer aporte, debe seleccionar un punto de acopio obligatorio
                if (!model.IdPuntoAcopio.HasValue || model.IdPuntoAcopio.Value <= 0)
                {
                    ModelState.AddModelError("IdPuntoAcopio", "Debe seleccionar el punto de acopio donde entregará su producto.");
                }
            }

            // Si hay errores de validación, retornar la vista con los mensajes
            if (!ModelState.IsValid)
            {
                await CargarPuntosAcopioParaOfertaAsync(model, idUsuarioCampesino);
                return View(model);
            }

            // 5. GUARDADO SEGURO DE LA IMAGEN EN EL SERVIDOR (CARPETA images DEL APLICATIVO)
            string rutaConsultaWeb = string.Empty;
            try
            {
                var extension = Path.GetExtension(model.ImagenProducto!.FileName).ToLowerInvariant();
                // Generamos un identificador único seguro (32 caracteres hex + extensión)
                string nombreArchivo = $"{Guid.NewGuid():N}{extension}";

                // Guardado físico en la carpeta wwwroot/images/productos del aplicativo
                string carpetaImagenes = Path.Combine(_webHostEnvironment.WebRootPath, "images", "productos");
                if (!Directory.Exists(carpetaImagenes))
                {
                    Directory.CreateDirectory(carpetaImagenes);
                }

                string rutaFisicaCompleta = Path.Combine(carpetaImagenes, nombreArchivo);
                using (var stream = new FileStream(rutaFisicaCompleta, FileMode.Create))
                {
                    await model.ImagenProducto.CopyToAsync(stream);
                }

                // Dirección web de consulta que se guarda en la base de datos (máximo 60 caracteres)
                // Ejemplo: "/images/productos/a1b2c3d4e5f6...jpg" (~54 caracteres)
                rutaConsultaWeb = $"/images/productos/{nombreArchivo}";
            }
            catch (Exception)
            {
                ModelState.AddModelError("ImagenProducto", "Ocurrió un error al guardar la imagen en el servidor. Intente nuevamente.");
                await CargarPuntosAcopioParaOfertaAsync(model, idUsuarioCampesino);
                return View(model);
            }

            // 6. OPERACIÓN EN BASE DE DATOS (Paso 6 CU-013)
            try
            {
                // A) Insertar el aporte del campesino en la tabla aporte_Camp
                var nuevoAporte = new AporteCamp
                {
                    IdUsuario = idUsuarioCampesino,
                    IdDemanda = demanda.IdDemanda,
                    CantAporte = model.CantidadAporte!.Value,
                    Imagen = rutaConsultaWeb, // Se almacena la dirección de consulta web en la BD
                    IdPuntoAcopio = model.IdPuntoAcopio!.Value // Punto de acopio seleccionado por el campesino para entrega
                };

                _context.AporteCamps.Add(nuevoAporte);

                // B) RN-02 y RN-03: Evaluar si el aporte completa el 100% de la solicitud
                // NOTA: El cambio de estado de la demanda (a Estado 2 'En recolección' o Estado 3 'En tránsito')
                // lo gestionan automáticamente los triggers de BD: [trg_ActualizarEstadoPorAporte] y [trg_CierreAutomaticoPorAportes].
                bool pedidoCompleto = (totalAportadoActual + model.CantidadAporte.Value) >= demanda.CantidadDeman;

                if (pedidoCompleto)
                {
                    // Notificar al comprador que su pedido fue completado al 100%
                    try
                    {
                        _notificacionService.Agregar(
                            new[] { demanda.IdUsuario },
                            "Llegada",
                            $"¡Excelente! Tu solicitud de compra #{demanda.IdDemanda} ha sido completada al 100% con aportes de campesinos.",
                            demanda.IdDemanda
                        );
                    }
                    catch
                    {
                        // Si falla la notificación auxiliar, no interrumpir la transacción principal
                    }
                }

                // Guardar los cambios en la base de datos (dispara los triggers de BD)
                await _context.SaveChangesAsync();

                // Sincronizamos la entidad en memoria con los estados actualizados por los triggers de BD
                await _context.Entry(demanda).ReloadAsync();

                // AUDITORÍA EN LA TABLA log_Sistema (CU-030 / RNF-01):
                try
                {
                    var usuarioCampesino = await _context.Usuarios.FindAsync(idUsuarioCampesino);
                    string nombreCampesino = usuarioCampesino != null
                        ? $"{usuarioCampesino.NombreCom} {usuarioCampesino.ApellidoCom}".Trim()
                        : $"Campesino ID #{idUsuarioCampesino}";

                    await _logService.RegistrarAsync(
                        NivelesLog.Info,
                        "CU-013 Registro de Oferta",
                        $"El campesino '{nombreCampesino}' (ID #{idUsuarioCampesino}) registró un aporte de {model.CantidadAporte!.Value} unidades a la Demanda #{demanda.IdDemanda} ({demanda.IdTipoProductoNavigation?.Producto}). Aporte total acumulado del campesino en esta demanda: {aportePrevioCampesino + model.CantidadAporte.Value} unidades. Imagen: {rutaConsultaWeb}",
                        idUsuarioCampesino
                    );

                    if (pedidoCompleto)
                    {
                        await _logService.RegistrarAsync(
                            NivelesLog.Info,
                            "CU-013 Demanda Completa",
                            $"La Demanda #{demanda.IdDemanda} ({demanda.IdTipoProductoNavigation?.Producto}) ha alcanzado el 100% requerido ({demanda.CantidadDeman} unidades) tras el aporte del campesino ID #{idUsuarioCampesino}. Estado de la demanda actualizado en base de datos a 'En tránsito' (Estado 3).",
                            idUsuarioCampesino
                        );
                    }
                }
                catch
                {
                    // La auditoría complementaria en logs nunca debe interrumpir el éxito de la transacción
                }

                // 7. MENSAJE DE ÉXITO Y REDIRECCIÓN (Paso 6 CU-013)
                // Se muestra el mensaje durante 3 segundos antes de redirigir al catálogo de demanda CU-011
                ViewBag.RegistroExitoso = true;
                if (pedidoCompleto)
                {
                    ViewBag.MensajeExito = "¡Excelente! Tu producto completó la solicitud con éxito";
                    TempData["MensajeExito"] = "¡Excelente! Tu producto completó la solicitud con éxito";
                }
                else
                {
                    ViewBag.MensajeExito = $"¡Oferta registrada con éxito! Tu producto ha sido aportado a la demanda ({model.CantidadAporte.Value} unidades).";
                    TempData["MensajeExito"] = $"¡Oferta registrada con éxito! Tu producto ha sido aportado a la demanda ({model.CantidadAporte.Value} unidades).";
                }

                // Actualizar las cantidades en el modelo para la vista de éxito
                model.CantidadAportadaPrevia = totalAportadoActual + model.CantidadAporte.Value;
                model.CantidadFaltante = Math.Max(0, (int)demanda.CantidadDeman - (model.CantidadAportadaPrevia ?? 0));

                await CargarPuntosAcopioParaOfertaAsync(model, idUsuarioCampesino);
                return View(model);
            }
            catch (Exception ex)
            {
                // AUDITORÍA DE ERROR EN log_Sistema
                try
                {
                    await _logService.RegistrarAsync(
                        NivelesLog.Error,
                        "CU-013 Registro de Oferta",
                        $"Error al registrar oferta del campesino ID #{idUsuarioCampesino} en Demanda #{model.IdDemanda}: {ex.Message}",
                        idUsuarioCampesino
                    );
                }
                catch { }

                // FE04-CU013: Error al registrar en base de datos
                ViewBag.MensajeErrorConexion = "No fue posible registrar la oferta en la base de datos. Por favor verifique su conexión e intente nuevamente.";
                await CargarPuntosAcopioParaOfertaAsync(model, idUsuarioCampesino);
                return View(model);
            }
        }

        // =========================================================================
        // MÉTODOS PRIVADOS AUXILIARES (SEGURIDAD Y PERMISOS)
        // =========================================================================
        /// <summary>
        /// Directriz de Seguridad RNF-01:
        /// Valida que el usuario tenga sesión activa y que su rol sea estrictamente 'Campesino'.
        /// Si no cumple, genera la redirección o la vista de acceso denegado correspondiente.
        /// </summary>
        private bool ValidarCampesinoAutenticado(out int idUsuario, out IActionResult? resultadoAcceso)
        {
            resultadoAcceso = null;
            idUsuario = HttpContext.Session.GetInt32("IdUsuario") ?? 0;
            string? rolUsuario = HttpContext.Session.GetString("RolUsuario");

            // 1. Validar que la sesión esté activa
            if (idUsuario <= 0 || string.IsNullOrEmpty(rolUsuario))
            {
                resultadoAcceso = RedirectToAction("Login", "InicioSesion");
                return false;
            }

            // 2. Validar que el rol sea estrictamente Campesino
            if (!string.Equals(rolUsuario, "Campesino", StringComparison.OrdinalIgnoreCase))
            {
                Response.StatusCode = StatusCodes.Status403Forbidden;
                resultadoAcceso = View("~/Views/Shared/AccesoDenegado.cshtml", "Acceso Denegado: Esta función está disponible exclusivamente para usuarios con rol Campesino.");
                return false;
            }

            return true;
        }
    }
}
