using CampesinApp.Services;
using Microsoft.AspNetCore.Mvc;

namespace CampesinApp.Controllers
{
    /// <summary>
    /// Configuración del perfil de usuario (CU-031): tarjeta con los datos del usuario y los botones de su rol,
    /// que abren las ventanas emergentes de los demás casos de uso. También el perfil que se consulta desde las
    /// tarjetas de la pantalla principal.
    /// </summary>
    public class PerfilController : AppController
    {
        private readonly PerfilService _perfil;
        private readonly ILogService _log;

        public PerfilController(IUsuarioActualService usuarioActual, PerfilService perfil, ILogService log) : base(usuarioActual)
        {
            _perfil = perfil;
            _log = log;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var idUsuario = HttpContext.Session.GetInt32("IdUsuario");
            if (idUsuario == null)
            {
                return RedirectToAction("Login", "InicioSesion");
            }

            try
            {
                var perfil = await _perfil.ObtenerAsync(idUsuario.Value);
                if (perfil == null)
                {
                    // La cuenta ya no está activa o no tiene un rol válido: se cierra la sesión.
                    HttpContext.Session.Clear();
                    return RedirectToAction("Login", "InicioSesion");
                }

                return View(perfil);
            }
            catch (Exception ex)
            {
                // FE-01-CU031: aviso durante 3 segundos y regreso a la pantalla principal.
                await _log.RegistrarAsync(Repositories.Models.NivelesLog.Error, "CU-031 Perfil", $"{ex.GetType().Name}: {ex.Message}", idUsuario);
                return View("ErrorPerfil");
            }
        }

        /// <summary>Botón de un módulo que pertenece a otro caso de uso y todavía no está disponible.</summary>
        [HttpGet]
        public IActionResult Proximamente(string? opcion)
        {
            var nombre = string.IsNullOrWhiteSpace(opcion) ? "Esta opción" : $"«{opcion}»";
            return Aviso($"{nombre} pertenece a otro módulo de CampesinApp que aún no está disponible en esta versión.");
        }

        [HttpGet("Perfil/ConsultaPerfilCampesino/{id:int}")]
        public Task<IActionResult> ConsultaPerfilCampesino(int id) => ConsultarPerfilAsync(id, "Campesino");

        [HttpGet("Perfil/ConsultaPerfilComprador/{id:int}")]
        public Task<IActionResult> ConsultaPerfilComprador(int id) => ConsultarPerfilAsync(id, "Comprador");

        private async Task<IActionResult> ConsultarPerfilAsync(int id, string rol)
        {
            if (HttpContext.Session.GetInt32("IdUsuario") == null)
            {
                return RedirectToAction("Login", "InicioSesion");
            }

            try
            {
                var perfil = await _perfil.ObtenerPublicoAsync(id, rol);
                return perfil == null ? View("PerfilNoEncontrado") : View("Consulta", perfil);
            }
            catch (Exception ex)
            {
                await _log.RegistrarAsync(Repositories.Models.NivelesLog.Error, "CU-031 Consulta de perfil", $"{ex.GetType().Name}: {ex.Message}", HttpContext.Session.GetInt32("IdUsuario"));
                return View("ErrorPerfil");
            }
        }
    }
}
