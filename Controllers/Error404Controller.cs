using Microsoft.AspNetCore.Mvc;
using CampesinApp.Dtos;
using CampesinApp.Services;
using CampesinApp.Repositories.Models;

namespace CampesinApp.Controllers
{
    public class Error404Controller : Controller
    {
        private readonly ILogService? _log;
        private readonly ILogger<Error404Controller>? _logger;

        public Error404Controller(ILogService? log = null, ILogger<Error404Controller>? logger = null)
        {
            _log = log;
            _logger = logger;
        }

        [Route("Error404/Index")]
        public async Task<IActionResult> Index(int statusCode)
        {
            if (statusCode == 504)
            {
                Response.StatusCode = StatusCodes.Status504GatewayTimeout;

                var idUsuario = HttpContext.Session?.GetInt32("IdUsuario");
                _logger?.LogWarning("Error 504 Gateway Timeout detectado. IdSeguimiento: {TraceId}", HttpContext.TraceIdentifier);

                if (_log != null)
                {
                    try
                    {
                        var ruta = HttpContext.Request.Path.Value ?? string.Empty;
                        await _log.RegistrarAsync(NivelesLog.Advertencia, "504 Gateway Timeout",
                            $"Tiempo de espera agotado al procesar la solicitud en {ruta}", idUsuario);
                    }
                    catch
                    {
                        // Evita que un problema de auditoría impida presentar el error
                    }
                }

                if (string.Equals(Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase))
                {
                    return new EmptyResult();
                }

                var modelo504 = PaginaErrorDto.TiempoAgotado();
                modelo504.IdSeguimiento = HttpContext.TraceIdentifier;
                return View("~/Views/Error/Pagina.cshtml", modelo504);
            }

            var modelo = new PaginaErrorDto();

            if (statusCode == 404)
            {
                modelo.Codigo = 404;
                modelo.Titulo = "Página no encontrada";
                modelo.Mensaje = "La ruta o perfil que intentas consultar no existe en CampesinApp.";
                modelo.Consejo = "Verifica que la dirección escrita en el navegador sea correcta o regresa al inicio.";
                modelo.EsDelServidor = false;
                modelo.Detalle = "El recurso solicitado no está disponible.";
            }
            else
            {
                modelo.Codigo = statusCode > 0 ? statusCode : 500;
                modelo.Titulo = "Ocurrió un error inesperado";
                modelo.Mensaje = "No pudimos procesar tu solicitud correctamente.";
                modelo.Consejo = "Intenta recargar la página más tarde.";
                modelo.EsDelServidor = true;
            }

            return View("Error404", modelo);
        }
    }
}