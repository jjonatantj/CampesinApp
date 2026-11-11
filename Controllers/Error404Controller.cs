using Microsoft.AspNetCore.Mvc;
using CampesinApp.Dtos;

namespace CampesinApp.Controllers
{
    public class Error404Controller : Controller
    {
        [Route("Error404/Index")]
        public IActionResult Index(int statusCode)
        {
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