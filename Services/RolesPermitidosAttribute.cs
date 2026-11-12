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
    /// Restringe una acción a ciertos roles. Si no se cumple, responde con un aviso emergente (no una página
    /// completa) porque las acciones protegidas se abren como ventanas emergentes.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public class RolesPermitidosAttribute : Attribute, IAsyncActionFilter
    {
        private readonly RolUsuario[] _roles;

        public RolesPermitidosAttribute(params RolUsuario[] roles)
        {
            _roles = roles;
        }

        /// <summary>Texto del aviso cuando el usuario autenticado no tiene el rol requerido.</summary>
        public string? Mensaje { get; set; }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var usuario = await context.HttpContext.RequestServices.GetRequiredService<IUsuarioActualService>().ObtenerAsync();
            if (usuario != null && _roles.Contains(usuario.Rol))
            {
                await next();
                return;
            }

            // Navegación normal (pantallas completas): sin sesión va al inicio de sesión; sin permiso, a una pantalla de aviso.
            if (!string.Equals(context.HttpContext.Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase)
                && context.HttpContext.Request.Method == HttpMethods.Get)
            {
                if (usuario == null)
                {
                    context.Result = new RedirectResult("/InicioSesion/Login");
                    return;
                }

                context.HttpContext.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Result = new ViewResult
                {
                    ViewName = "~/Views/Shared/AccesoDenegado.cshtml",
                    ViewData = new ViewDataDictionary<string>(new EmptyModelMetadataProvider(), new ModelStateDictionary())
                    {
                        Model = Mensaje ?? "No tiene permisos para realizar esta acción."
                    }
                };
                return;
            }

            var aviso = usuario == null
                ? AppController.AvisoSinSesion()
                : new AvisoDto
                {
                    Tipo = TipoAviso.Advertencia,
                    TextoBoton = "Cerrar",
                    Mensaje = Mensaje ?? "No tiene permisos para realizar esta acción."
                };
            context.HttpContext.Response.Headers[ModalHeaders.Destino] = "superpuesta";
            context.Result = new PartialViewResult
            {
                ViewName = "_Aviso",
                StatusCode = usuario == null ? StatusCodes.Status401Unauthorized : StatusCodes.Status403Forbidden,
                ViewData = new ViewDataDictionary<AvisoDto>(new EmptyModelMetadataProvider(), new ModelStateDictionary()) { Model = aviso }
            };
        }
    }
}
