using CampesinApp.Dtos;
using CampesinApp.Services;
using CampesinApp.Repositories.Models;
using Microsoft.AspNetCore.Mvc;

namespace CampesinApp.Controllers
{
    /// <summary>Precios sugeridos (CU-028): consolidado de precios estimados por producto, según el rol.</summary>
    public class PreciosController : AppController
    {
        private readonly PreciosService _precios;
        private readonly ILogService _log;

        public PreciosController(IUsuarioActualService usuarioActual, PreciosService precios, ILogService log)
            : base(usuarioActual)
        {
            _precios = precios;
            _log = log;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var usuario = await ObtenerUsuarioAsync();
            if (usuario == null)
            {
                return RedirectToAction("Login", "InicioSesion");
            }

            if (usuario.Rol == RolUsuario.Transportador)
            {
                return View("SinAcceso");
            }

            try
            {
                return View(await _precios.ListarAsync(usuario));
            }
            catch (Exception ex)
            {
                // FE-01-CU028
                await _log.RegistrarAsync(NivelesLog.Error, "CU-028 Precios sugeridos", $"{ex.GetType().Name}: {ex.Message}", usuario.Id);
                return View("ErrorCarga");
            }
        }
    }
}
