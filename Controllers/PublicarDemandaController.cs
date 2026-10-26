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

        public PublicarDemandaController(Campesinapp3Context context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            int? idUsuario = HttpContext.Session.GetInt32("IdUsuario");
            if (idUsuario == null)
            {
                return RedirectToAction("Login", "InicioSesion");
            }

            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.IdUsuario == idUsuario.Value && u.Estado);
            if (usuario == null)
            {
                return RedirectToAction("Login", "InicioSesion");
            }

            var model = new PublicarDemandaViewModel
            {
                ListaTiposProducto = await _context.TipoProductos
                    .Select(tp => new SelectListItem { Value = tp.IdTipoProducto.ToString(), Text = tp.Producto })
                    .ToListAsync(),
                ListaPuntosAcopio = await _context.PuntoAcopios
                    .Select(pa => new SelectListItem { Value = pa.IdPuntoAcopio.ToString(), Text = pa.NombrePunto })
                    .ToListAsync()
            };

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

            decimal precio = precioEst.PrecioEsti;
            decimal min = precio * 0.9m; // -10%
            decimal max = precio * 1.1m; // +10%

            return Json(new { exito = true, precio = precio, min = min, max = max });
        }

        [HttpPost]
        public IActionResult GuardarAjax(PublicarDemandaViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var errorMensaje = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .FirstOrDefault()?.ErrorMessage ?? "Datos inválidos.";

                return Json(new { exito = false, error = errorMensaje });
            }

            int idUsuarioSesion = HttpContext.Session.GetInt32("IdUsuario") ?? 0;

            if (idUsuarioSesion == 0)
            {
                return Json(new { exito = false, error = "La sesión ha expirado. Por favor, inicie sesión nuevamente." });
            }

            // Combinar Fecha y Hora de Entrega
            DateTime fechaHoraEntrega = model.FechaEntrega.Date;
            if (TimeSpan.TryParse(model.HoraEntrega, out TimeSpan timeEntrega))
            {
                fechaHoraEntrega = fechaHoraEntrega.Add(timeEntrega);
            }

            // Combinar Fecha y Hora Límite
            DateTime fechaHoraLimite = model.LimTiemDeman.Date;
            if (TimeSpan.TryParse(model.HoraLimite, out TimeSpan timeLimite))
            {
                fechaHoraLimite = fechaHoraLimite.Add(timeLimite);
            }

            // Guardamos el pedido en memoria de sesión como borrador validado.
            // NO se inserta en la base de datos hasta que el comprador pague el 100% en ProcesoPago (CU-017).
            // Esto evita que pedidos no pagados o con pago rechazado queden registrados en el sistema.
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
                DireccionInicial = model.DireccionEstructurada ?? string.Empty
            };

            HttpContext.Session.SetString("DemandaBorrador", JsonSerializer.Serialize(borrador));

            return Json(new
            {
                exito = true,
                mensaje = "Información del pedido validada. Procediendo a la confirmación del pago...",
                redirectUrl = Url.Action("Index", "ProcesoPago")
            });
        }
    }
}
}