using CampesinApp.Dtos;
using CampesinApp.Repositories.Models;
using CampesinApp.Services;
using Microsoft.AspNetCore.Mvc;

namespace CampesinApp.Controllers
{
    /// <summary>
    /// Cierre de sesión (CU-032): confirmación en una ventana emergente, salida y cierre automático por inactividad.
    /// El inicio de sesión es del CU-004 (InicioSesionController); aquí solo se finaliza la sesión que ese caso de uso crea.
    /// </summary>
    public class SesionController : AppController
    {
        /// <summary>Claves de TempData que lee el inicio de sesión para mostrar el cuadro flotante.</summary>
        public const string ClaveAviso = "AvisoSesion";
        public const string ClaveAvisoError = "AvisoSesionError";

        private readonly ILogService _log;

        public SesionController(IUsuarioActualService usuarioActual, ILogService log) : base(usuarioActual)
        {
            _log = log;
        }

        /// <summary>Paso 4 del flujo básico: "¿Estás seguro de que deseas cerrar sesión?" con "Cancelar" y "Sí, salir".</summary>
        [HttpGet]
        public IActionResult Confirmar()
        {
            if (SinSesion())
            {
                return AvisoResultado(AvisoSinSesion(), StatusCodes.Status401Unauthorized);
            }

            return ModalSuperpuesto("_ConfirmarSalida");
        }

        /// <summary>Paso 6: finaliza la sesión y redirige al inicio de sesión con el cuadro "Sesión cerrada con éxito".</summary>
        [HttpPost]
        public async Task<IActionResult> Salir()
        {
            var fallo = false;
            try
            {
                var usuario = await ObtenerUsuarioAsync();
                if (usuario != null)
                {
                    await _log.RegistrarAsync(NivelesLog.Info, "CU-032 Cierre de sesión", $"{usuario.Rol} {usuario.Nombre} cerró sesión.", usuario.Id);
                }
            }
            catch (Exception)
            {
                fallo = true;
            }
            finally
            {
                // FE01-CU032: aunque el servidor falle, la salida del usuario tiene prioridad.
                HttpContext.Session.Clear();
            }

            if (fallo)
            {
                TempData[ClaveAvisoError] = "No se pudo procesar la salida correctamente en el servidor, pero la sesión ha sido finalizada";
            }
            else
            {
                TempData[ClaveAviso] = "Sesión cerrada con éxito";
            }

            return RedirectToAction("Login", "InicioSesion");
        }

        /// <summary>FA02-CU032: la página la invoca inactividad.js cuando se supera el tiempo sin actividad.</summary>
        [HttpGet]
        public IActionResult Expirada()
        {
            HttpContext.Session.Clear();
            TempData[ClaveAvisoError] = "La sesión ha expirado por inactividad";
            return RedirectToAction("Login", "InicioSesion");
        }

        private bool SinSesion() => HttpContext.Session.GetInt32("IdUsuario") == null;
    }
}
