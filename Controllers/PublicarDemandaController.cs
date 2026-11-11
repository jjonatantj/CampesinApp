using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using CampesinApp.Repositories.Models;
using CampesinApp.ViewModels;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace CampesinApp.Controllers
{
    [Route("PublicarDemanda")]
    public class PublicarDemandaController : Controller
    {
        private readonly Campesinapp3Context _context;
        private readonly IConfiguration _configuration;

        public PublicarDemandaController(Campesinapp3Context context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            int? idUsuario = HttpContext.Session.GetInt32("IdUsuario");
            if (idUsuario == null)
            {
                return RedirectToAction("Login", "InicioSesion");
            }

            // Consultar el usuario junto con su rol para validar la restricción de acceso
            var usuario = await _context.Usuarios
                .Include(u => u.IdRolNavigation)
                .FirstOrDefaultAsync(u => u.IdUsuario == idUsuario.Value && u.Estado);

            if (usuario == null)
            {
                return RedirectToAction("Login", "InicioSesion");
            }

            // SEGURIDAD ESTRICTA: Solo el rol "Comprador" tiene permitido acceder a publicar demandas
            string rolUsuario = usuario.IdRolNavigation?.TipoRol ?? string.Empty;
            if (!rolUsuario.Equals("Comprador", StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorAcceso"] = "No tienes permisos para acceder a esta sección. Acceso exclusivo para compradores.";
                return RedirectToAction("Index", "Perfil");
            }

            // Limpiar cualquier borrador previo al ingresar para iniciar con campos limpios
            HttpContext.Session.Remove("DemandaBorrador");

            // Validar Puntos de Acopio del usuario
            var puntosAcopio = await _context.PuntoAcopios
                .Where(pa => pa.IdUsuario == idUsuario.Value)
                .Select(pa => new SelectListItem { Value = pa.IdPuntoAcopio.ToString(), Text = pa.NombrePunto })
                .ToListAsync();

            var model = new PublicarDemandaViewModel
            {
                ListaTiposProducto = await _context.TipoProductos
                    .Select(tp => new SelectListItem { Value = tp.IdTipoProducto.ToString(), Text = tp.Producto })
                    .ToListAsync(),
                ListaPuntosAcopio = puntosAcopio,
                TienePuntosAcopio = puntosAcopio.Count > 0
            };

            ViewBag.GoogleMapsApiKey = _configuration["GoogleMaps:ApiKey"] ?? string.Empty;

            return View("PublicarDemanda", model);
        }

        [HttpGet("ObtenerVariedadesPorTipo")]
        public async Task<IActionResult> ObtenerVariedadesPorTipo(int idTipoProducto)
        {
            var variedades = await _context.Variedads
                .Where(v => v.IdTipoProducto == idTipoProducto)
                .Select(v => new { id = v.IdVariedad, nombre = v.VariedadProd })
                .ToListAsync();

            return Json(variedades);
        }

        [HttpGet("ObtenerCoordenadasPuntoAcopio")]
        public async Task<IActionResult> ObtenerCoordenadasPuntoAcopio(int idPuntoAcopio)
        {
            var punto = await _context.PuntoAcopios
                .Where(pa => pa.IdPuntoAcopio == idPuntoAcopio)
                .Select(pa => new { lat = pa.Latitud, lng = pa.Longitud, direccion = pa.Direccion, nombre = pa.NombrePunto })
                .FirstOrDefaultAsync();

            if (punto == null) return Json(new { exito = false });

            return Json(new { exito = true, lat = punto.lat, lng = punto.lng, direccion = punto.direccion, nombre = punto.nombre });
        }

        [HttpGet("ObtenerPrecioEstimado")]
        public async Task<IActionResult> ObtenerPrecioEstimado(int idTipoProducto, int idVariedad)
        {
            var precioEst = await _context.PrecioEstimados
                .Where(p => p.IdTipoProducto == idTipoProducto && p.IdVariedad == idVariedad)
                .OrderByDescending(p => p.FechaPreci)
                .FirstOrDefaultAsync();

            if (precioEst == null)
            {
                return Json(new { exito = false, precio = 0 });
            }

            decimal precio = (decimal)Math.Round(precioEst.PrecioEstiComprador, 0);
            decimal min = precio * 0.9m;
            decimal max = precio * 1.1m;

            return Json(new { exito = true, precio = precio, min = min, max = max });
        }

        [HttpPost("GuardarAjax")]
        public async Task<IActionResult> GuardarAjax(PublicarDemandaViewModel model)
        {
            int idUsuarioSesion = HttpContext.Session.GetInt32("IdUsuario") ?? 0;
            if (idUsuarioSesion == 0)
            {
                return Json(new { exito = false, error = "La sesión ha expirado. Inicie sesión nuevamente." });
            }

            // Validar doble seguridad de rol en el POST
            var usuario = await _context.Usuarios
                .Include(u => u.IdRolNavigation)
                .FirstOrDefaultAsync(u => u.IdUsuario == idUsuarioSesion);

            string rolUsuario = usuario?.IdRolNavigation?.TipoRol ?? string.Empty;
            if (!rolUsuario.Equals("Comprador", StringComparison.OrdinalIgnoreCase))
            {
                return Json(new { exito = false, error = "Acción no autorizada para su rol de usuario." });
            }

            // Fecha límite automática: 2 días después a las 23:59[cite: 9]
            model.LimTiemDeman = DateTime.Now.AddDays(2).Date.AddHours(23).AddMinutes(59);

            ModelState.Remove(nameof(model.LimTiemDeman));

            if (!ModelState.IsValid)
            {
                var errorMensaje = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .FirstOrDefault()?.ErrorMessage ?? "Datos inválidos.";

                return Json(new { exito = false, error = errorMensaje });
            }

            var puntoAcopio = await _context.PuntoAcopios
                .FirstOrDefaultAsync(pa => pa.IdPuntoAcopio == model.IdPuntoAcopio && pa.IdUsuario == idUsuarioSesion);

            if (puntoAcopio == null)
            {
                return Json(new { exito = false, error = "El punto de acopio seleccionado no es válido o no le pertenece." });
            }

            DateTime fechaHoraEntrega = model.FechaEntrega.Date;
            if (TimeSpan.TryParse(model.HoraEntrega, out TimeSpan timeEntrega))
            {
                fechaHoraEntrega = fechaHoraEntrega.Add(timeEntrega);
            }

            DateTime fechaHoraLimite = model.LimTiemDeman.Value;

            var borrador = new BorradorDemandaDto
            {
                IdUsuario = idUsuarioSesion,
                IdTipoProducto = model.IdTipoProducto,
                IdVariedad = model.IdVariedad,
                Cantidad = (int)model.Cantidad,
                PrecioEstimado = (double)model.PrecioEstimado,
                FechaEntrega = fechaHoraEntrega,
                LimTiemDeman = fechaHoraLimite,
                Descripcion = model.Descripcion ?? string.Empty,
                IdPuntoAcopio = model.IdPuntoAcopio,
                DireccionInicial = puntoAcopio.Direccion ?? string.Empty
            };

            HttpContext.Session.SetString("DemandaBorrador", JsonSerializer.Serialize(borrador));

            return Json(new
            {
                exito = true,
                mensaje = "Pedido validado correctamente. Redirigiendo al pago seguro...",
                redirectUrl = Url.Action("Index", "ProcesoPago")
            });
        }

        // Acción dedicada para cancelar y limpiar los datos de la demanda en sesión
        [HttpGet("Cancelar")]
        public IActionResult Cancelar()
        {
            HttpContext.Session.Remove("DemandaBorrador");
            return RedirectToAction("Index", "Perfil");
        }
    }

    public class BorradorDemandaDto
    {
        public int IdUsuario { get; set; }
        public int IdTipoProducto { get; set; }
        public int IdVariedad { get; set; }
        public int Cantidad { get; set; }
        public double PrecioEstimado { get; set; }
        public DateTime FechaEntrega { get; set; }
        public DateTime LimTiemDeman { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        public int IdPuntoAcopio { get; set; }
        public string DireccionInicial { get; set; } = string.Empty;
    }
}