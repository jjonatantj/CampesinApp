using CampesinApp.Dtos;
using CampesinApp.Services;
using CampesinApp.Repositories.Models;
using Microsoft.AspNetCore.Mvc;

namespace CampesinApp.Controllers
{
    /// <summary>
    /// Base de los controladores que responden con ventanas emergentes (partial views que el navegador inserta en
    /// el modal). Las cabeceras X-Modal-Destino, X-Cerrar-Modal y X-Redirigir le indican a modales.js qué hacer.
    /// </summary>
    [AutoValidateAntiforgeryToken]
    [ServiceFilter(typeof(ErrorAjaxFilter))]
    public abstract class AppController : Controller
    {
        private readonly IUsuarioActualService _usuarioActual;

        protected AppController(IUsuarioActualService usuarioActual)
        {
            _usuarioActual = usuarioActual;
        }

        protected Task<UsuarioActual?> ObtenerUsuarioAsync() => _usuarioActual.ObtenerAsync();

        /// <summary>Vista parcial que se muestra en la ventana emergente principal.</summary>
        protected IActionResult ModalPrincipal(string vista, object? modelo = null)
        {
            Response.Headers[ModalHeaders.Destino] = "principal";
            return PartialView(vista, modelo);
        }

        /// <summary>Vista parcial que se muestra dentro del panel de una pantalla completa (#panelPagina), no en una ventana.</summary>
        protected IActionResult PanelPagina(string vista, object? modelo = null)
        {
            Response.Headers[ModalHeaders.Destino] = "pagina";
            return PartialView(vista, modelo);
        }

        /// <summary>true cuando la petición la hizo modales.js con fetch (actualiza el panel); false cuando es una navegación normal.</summary>
        protected bool EsPeticionAjax() => Request.EsPeticionAjax();

        /// <summary>Vista parcial que se muestra en la ventana superpuesta (por encima de la principal).</summary>
        protected IActionResult ModalSuperpuesto(string vista, object? modelo = null)
        {
            Response.Headers[ModalHeaders.Destino] = "superpuesta";
            return PartialView(vista, modelo);
        }

        /// <summary>Cuadro de aviso con un botón, siempre en la ventana superpuesta.</summary>
        protected IActionResult Aviso(string mensaje, TipoAviso tipo = TipoAviso.Informacion, string textoBoton = "Aceptar",
            string? titulo = null, string? reintentarUrl = null, string cerrar = "superpuesta", string? redirigirA = null,
            string? recargarPrincipalUrl = null, int estado = 200)
        {
            return AvisoResultado(new AvisoDto
            {
                Mensaje = mensaje,
                Tipo = tipo,
                TextoBoton = textoBoton,
                Titulo = titulo,
                ReintentarUrl = reintentarUrl,
                Cerrar = cerrar,
                RedirigirA = redirigirA,
                RecargarPrincipalUrl = recargarPrincipalUrl
            }, estado);
        }

        /// <summary>Convierte un resultado fallido de un servicio en el aviso que corresponde.</summary>
        protected IActionResult AvisoDeError(ResultadoOperacion resultado, string? recargarPrincipalUrl = null)
        {
            var tipo = resultado.Error == TipoError.Interno ? TipoAviso.Error : TipoAviso.Advertencia;
            var estado = resultado.Error == TipoError.NoAutorizado ? StatusCodes.Status403Forbidden : StatusCodes.Status200OK;
            return Aviso(resultado.Mensaje ?? "No se pudo completar la operación.", tipo, recargarPrincipalUrl: recargarPrincipalUrl, estado: estado);
        }

        /// <summary>Cierra la ventana superpuesta ("superpuesta") o todas ("todas") sin mostrar nada.</summary>
        protected IActionResult CerrarModales(string cuales = "todas")
        {
            Response.Headers[ModalHeaders.Cerrar] = cuales;
            return new EmptyResult();
        }

        protected IActionResult AvisoResultado(AvisoDto aviso, int estado = 200)
        {
            Response.Headers[ModalHeaders.Destino] = "superpuesta";
            Response.StatusCode = estado;
            return PartialView("_Aviso", aviso);
        }

        /// <summary>Resultado para quien no tiene sesión o rol suficiente.</summary>
        protected IActionResult SinPermiso(UsuarioActual? usuario, string? mensaje = null)
        {
            if (usuario == null)
            {
                return AvisoResultado(AvisoSinSesion(), StatusCodes.Status401Unauthorized);
            }

            return Aviso(mensaje ?? "No tiene permisos para realizar esta acción.", TipoAviso.Advertencia, estado: StatusCodes.Status403Forbidden);
        }

        /// <summary>Aviso para una sesión no iniciada o vencida: el botón lleva a la pantalla de inicio de sesión.</summary>
        public static AvisoDto AvisoSinSesion() => new()
        {
            Tipo = TipoAviso.Advertencia,
            Mensaje = "Su sesión no está activa. Inicie sesión para continuar.",
            TextoBoton = "Iniciar sesión",
            Cerrar = "todas",
            RedirigirA = "/InicioSesion/Login"
        };
    }

    public static class ModalHeaders
    {
        public const string Destino = "X-Modal-Destino";
        public const string Cerrar = "X-Cerrar-Modal";
        public const string Redirigir = "X-Redirigir";
    }
}
