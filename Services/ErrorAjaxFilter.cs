using CampesinApp.Controllers;
using CampesinApp.Dtos;
using CampesinApp.Repositories.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace CampesinApp.Services
{
    /// <summary>
    /// Convierte los errores no controlados de las peticiones AJAX (las de las ventanas emergentes) en un aviso,
    /// los registra en log_Sistema y evita que el navegador reciba una página de error dentro del modal.
    /// </summary>
    public class ErrorAjaxFilter : IAsyncExceptionFilter
    {
        private readonly ILogService _log;
        private readonly IUsuarioActualService _usuarioActual;

        public ErrorAjaxFilter(ILogService log, IUsuarioActualService usuarioActual)
        {
            _log = log;
            _usuarioActual = usuarioActual;
        }

        public async Task OnExceptionAsync(ExceptionContext context)
        {
            var peticion = context.HttpContext.Request;
            if (peticion.Headers["X-Requested-With"] != "XMLHttpRequest")
            {
                return;
            }

            UsuarioActual? usuario = null;
            try { usuario = await _usuarioActual.ObtenerAsync(); } catch { /* el registro del error no depende del usuario */ }

            await _log.RegistrarAsync(NivelesLog.Error, $"{peticion.Method} {peticion.Path}",
                $"{context.Exception.GetType().Name}: {context.Exception.Message}", usuario?.Id);

            context.HttpContext.Response.Headers[ModalHeaders.Destino] = "superpuesta";
            context.Result = new PartialViewResult
            {
                ViewName = "_Aviso",
                StatusCode = StatusCodes.Status500InternalServerError,
                ViewData = new ViewDataDictionary<AvisoDto>(new EmptyModelMetadataProvider(), new ModelStateDictionary())
                {
                    Model = new AvisoDto
                    {
                        Tipo = TipoAviso.Error,
                        Mensaje = "Ocurrió un error inesperado. Intenta de nuevo más tarde."
                    }
                }
            };
            context.ExceptionHandled = true;
        }
    }
}
