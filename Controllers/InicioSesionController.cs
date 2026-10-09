using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using CampesinApp.Repositories;
using CampesinApp.ViewModels;

namespace CampesinApp.Controllers
{
    /// <summary>
    /// Controlador de inicio de sesión y recuperación de contraseña (CU-004 / CU-005).
    /// </summary>
    public class InicioSesionController : Controller
    {
        private readonly UsuariosRepository _usuariosRepo;

        public InicioSesionController(UsuariosRepository usuariosRepo)
        {
            _usuariosRepo = usuariosRepo;
        }

        // ─── CU-004 INICIO DE SESIÓN ─────────────────────────────────────

        [HttpGet]
        public IActionResult Login()
        {
            return View("InicioSesion", new LoginViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View("InicioSesion", model ?? new LoginViewModel());

            // Validación de credenciales contra la base de datos real
            var usuario = _usuariosRepo.ValidarCredenciales(model.Cedula, model.Contrasena);

            if (usuario != null)
            {
                string rolUsuario = usuario.IdRolNavigation?.TipoRol ?? "Campesino";

                // Guardar información real de la BD en la Sesión HTTP
                HttpContext.Session.SetString("RolUsuario", rolUsuario);
                HttpContext.Session.SetString("CedulaUsuario", model.Cedula);
                HttpContext.Session.SetInt32("IdUsuario", usuario.IdUsuario);

                TempData["MensajeExito"] = $"¡Bienvenido de nuevo a CampesinApp ({rolUsuario})!";

                // Redirección directa hacia la Pantalla Principal
                return RedirectToAction("Index", "PantallaPrincipal");
            }

            ModelState.AddModelError(string.Empty, "Cédula o contraseña incorrectas, o la cuenta está inactiva.");
            return View("InicioSesion", model ?? new LoginViewModel());
        }

        // ─── CIERRE DE SESIÓN (CU-030) ───────────────────────────────────

        [HttpGet]
        public IActionResult CerrarSesion()
        {
            HttpContext.Session.Clear();
            TempData["MensajeExito"] = "Sesión cerrada correctamente.";
            return RedirectToAction("Login");
        }

        // ─── CU-005 RECUPERACIÓN DE CONTRASEÑA ──────────────────────────

        [HttpGet]
        public IActionResult RecuperarPassword()
        {
            return View("RecuperarContraseña", new RecuperaciónContraseñaViewModel { Paso = 1 });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RecuperarPassword(RecuperaciónContraseñaViewModel model)
        {
            if (model.Paso == 1)
            {
                if (string.IsNullOrEmpty(model.Cedula))
                {
                    ModelState.AddModelError("Cedula", "Ingrese su número de cédula.");
                    return View("RecuperarContraseña", model);
                }

                var usuario = _usuariosRepo.BuscarPorCedula(model.Cedula);
                if (usuario == null)
                {
                    ModelState.AddModelError("Cedula", "No encontramos una cuenta activa con esa cédula.");
                    return View("RecuperarContraseña", model);
                }

                model.Paso = 2;
                model.CorreoEnmascarado = EnmascararCorreo(usuario.CorreoUsu);
                model.TelefonoEnmascarado = EnmascararTelefono(usuario.TelelfonoUsu.ToString());
                ModelState.Clear();
                return View("RecuperarContraseña", model);
            }

            if (model.Paso == 2)
            {
                model.Paso = 3;
                ModelState.Clear();
                return View("RecuperarContraseña", model);
            }

            if (model.Paso == 3)
            {
                if (model.CodigoVerificacion?.Length != 6)
                {
                    ModelState.AddModelError("CodigoVerificacion", "El código debe tener exactamente 6 dígitos.");
                    model.Paso = 3;
                    return View("RecuperarContraseña", model);
                }

                model.Paso = 4;
                ModelState.Clear();
                return View("RecuperarContraseña", model);
            }

            if (model.Paso == 4)
            {
                if (ModelState.IsValid)
                {
                    _usuariosRepo.ActualizarContrasena(model.Cedula, model.NuevaContrasena);
                    TempData["MensajeExito"] = "¡Contraseña actualizada con éxito! Inicie sesión con su nueva clave.";
                    return RedirectToAction("Login");
                }
            }

            return View("RecuperarContraseña", model);
        }

        // ─── MÉTODOS PRIVADOS DE APOYO ───────────────────────────────────

        private static string EnmascararCorreo(string correo)
        {
            if (string.IsNullOrEmpty(correo)) return "****@****.com";
            int arroba = correo.IndexOf('@');
            if (arroba <= 2) return correo;

            int longitudOculta = arroba - 2;
            return correo.Substring(0, 2) + new string('*', longitudOculta) + correo.Substring(arroba);
        }

        private static string EnmascararTelefono(string telefono)
        {
            if (string.IsNullOrEmpty(telefono) || telefono.Length < 4) return "****";
            return new string('*', telefono.Length - 4) + telefono.Substring(telefono.Length - 4);
        }
    }
}
