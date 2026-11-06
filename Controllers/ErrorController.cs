using CampesinApp.Dtos;
using CampesinApp.Repositories.Models;
using CampesinApp.Services;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace CampesinApp.Controllers
{
    /// <summary>
    /// Pantalla "Servicio no disponible" (503) con el diseño de CampesinApp. Program.cs la conecta con UseExceptionHandler:
    /// se muestra cuando una petición falla porque SQL Server no responde (sin conexión, tiempo agotado, base no disponible).
    /// Los demás errores no se atienden aquí: se devuelve 404 para que ExceptionHandlerMiddleware vuelva a lanzar la excepción
    /// original y la atienda el manejo de errores del resto del equipo (/Home/Error o la página del desarrollador).
    /// La pantalla no usa sesión ni base de datos, así sigue funcionando cuando el servidor tiene problemas.
    /// La pantalla "Sin conexión a internet" es distinta: la muestra el service worker (wwwroot/sw.js).
    /// </summary>
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    [IgnoreAntiforgeryToken]
    public class ErrorController : Controller
    {
        private readonly ILogService _log;
        private readonly ILogger<ErrorController> _logger;

        public ErrorController(ILogService log, ILogger<ErrorController> logger)
        {
            _log = log;
            _logger = logger;
        }

        [Route("Error/Excepcion")]
        public async Task<IActionResult> Excepcion()
        {
            var excepcion = HttpContext.Features.Get<IExceptionHandlerFeature>()?.Error;
            if (!EsFalloDeBaseDeDatos(excepcion))
            {
                // No es un fallo de base de datos: el 404 indica "no atendido" y el error original sigue su camino.
                return NotFound();
            }

            var ruta = HttpContext.Features.Get<IExceptionHandlerPathFeature>()?.Path ?? string.Empty;
            _logger.LogError(excepcion, "Base de datos no disponible en {Ruta}", ruta);
            await _log.RegistrarAsync(NivelesLog.Error, "Base de datos no disponible",
                $"{ruta}: {excepcion!.GetType().Name}: {excepcion.Message}", HttpContext.Session.GetInt32("IdUsuario"));

            Response.StatusCode = StatusCodes.Status503ServiceUnavailable;

            // Las ventanas emergentes (AJAX) reciben solo el código; modales.js muestra su aviso.
            if (string.Equals(Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase))
            {
                return new EmptyResult();
            }

            var modelo = PaginaErrorDto.ServicioNoDisponible();
            modelo.IdSeguimiento = HttpContext.TraceIdentifier;
            return View("Pagina", modelo);
        }

        /// <summary>true si la excepción (o alguna interna) es de SQL Server: sin conexión, tiempo agotado, base no disponible...</summary>
        private static bool EsFalloDeBaseDeDatos(Exception? excepcion)
        {
            for (var actual = excepcion; actual != null; actual = actual.InnerException)
            {
                if (actual is SqlException) return true;
            }

            return false;
        }
    }
}
