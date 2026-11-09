using Microsoft.AspNetCore.Mvc;

namespace CampesinApp.Controllers
{
    public class Error404Controller : Controller
    {
        // Esta acción ataja exactamente la ruta configurada en el Program.cs
        [Route("Error404/Index")]
        public IActionResult Index(int statusCode)
        {
            if (statusCode == 404)
            {
                ViewData["ErrorMessage"] = "La página o ruta que buscas en CampesinApp no existe.";
            }

            // Retorna tu archivo Views/Shared/Error404.cshtml
            return View("Error404");
        }
    }
}