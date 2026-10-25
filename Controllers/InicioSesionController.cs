using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using CampesinApp.Repositories;
using CampesinApp.Repositories.Models;
using CampesinApp.ViewModels;
using CampesinApp.Services;
using System;
using System.Linq;

namespace CampesinApp.Controllers
{
    /// <summary>
    /// Controlador de inicio de sesión y recuperación de contraseña (CU-004 / CU-005).
    /// </summary>
    public class InicioSesionController : Controller
    {
        private readonly UsuariosRepository _usuariosRepo;
        private readonly Campesinapp3Context _context;

        public InicioSesionController(UsuariosRepository usuariosRepo, Campesinapp3Context context)
        {
            _usuariosRepo = usuariosRepo;
            _context = context;
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

            string sessionKeyIntentos = $"IntentosFallidos_{model.Cedula}";
            string sessionKeyBloqueo = "TiempoBloqueoUsuario";

            // Verificar si el usuario se encuentra bloqueado temporalmente
            long? bloqueoTicks = HttpContext.Session.GetLong(sessionKeyBloqueo);
            if (bloqueoTicks != null)
            {
                DateTime tiempoBloqueoFin = new DateTime(bloqueoTicks.Value);
                if (DateTime.Now < tiempoBloqueoFin)
                {
                    int minutosRestantes = (int)Math.Ceiling((tiempoBloqueoFin - DateTime.Now).TotalMinutes);
                    ModelState.AddModelError(string.Empty, $"Demasiados intentos fallidos. Su cuenta está bloqueada temporalmente. Intente de nuevo en {minutosRestantes} minuto(s).");
                    return View("InicioSesion", model ?? new LoginViewModel());
                }
                else
                {
                    // Expiró el bloqueo, limpiar
                    HttpContext.Session.Remove(sessionKeyBloqueo);
                    HttpContext.Session.Remove(sessionKeyIntentos);
                }
            }

            // Validación de credenciales contra la base de datos real
            var usuario = _usuariosRepo.ValidarCredenciales(model.Cedula, model.Contrasena);

            if (usuario != null)
            {
                // INTERCEPTAR: Verificar si tiene cambio de contraseña pendiente en log_Sistema (Primer inicio de admin)
                var pendienteCambio = _context.LogSistemas
                    .FirstOrDefault(l => l.IdUsuario == usuario.IdUsuario && l.Mensaje == "CAMBIO_CONTRASENA_PENDIENTE");

                if (pendienteCambio != null)
                {
                    HttpContext.Session.SetInt32("UsuarioCambioPasswordId", usuario.IdUsuario);
                    return RedirectToAction("ForzarCambioContrasena", "InicioSesion");
                }

                // Limpiar intentos fallidos al ingresar exitosamente
                HttpContext.Session.Remove(sessionKeyIntentos);
                HttpContext.Session.Remove(sessionKeyBloqueo);

                string rolUsuario = usuario.IdRolNavigation?.TipoRol ?? "Campesino";

                // Guardar información real de la BD en la Sesión HTTP
                HttpContext.Session.SetString("RolUsuario", rolUsuario);
                HttpContext.Session.SetString("CedulaUsuario", model.Cedula);
                HttpContext.Session.SetInt32("IdUsuario", usuario.IdUsuario);

                TempData["MensajeExito"] = $"¡Bienvenido de nuevo a CampesinApp ({rolUsuario})!";

                // Redirección directa hacia la Pantalla Principal
                return RedirectToAction("Index", "PantallaPrincipal");
            }

            // Lógica de control de intentos fallidos (máximo 3)
            int intentosActuales = HttpContext.Session.GetInt32(sessionKeyIntentos) ?? 0;
            intentosActuales++;

            if (intentosActuales >= 3)
            {
                // Bloquear por 5 minutos
                DateTime bloqueoFin = DateTime.Now.AddMinutes(5);
                HttpContext.Session.SetLong(sessionKeyBloqueo, bloqueoFin.Ticks);
                HttpContext.Session.Remove(sessionKeyIntentos);

                ModelState.AddModelError(string.Empty, "Ha superado el límite de 3 intentos fallidos. Su cuenta ha sido bloqueada temporalmente por 5 minutos.");
            }
            else
            {
                HttpContext.Session.SetInt32(sessionKeyIntentos, intentosActuales);
                int intentosRestantes = 3 - intentosActuales;
                ModelState.AddModelError(string.Empty, $"Cédula o contraseña incorrectas. Le quedan {intentosRestantes} intento(s) antes del bloqueo temporal.");
            }

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

        // ─── FORZAR CAMBIO DE CONTRASEÑA (PRIMER INGRESO ADMINISTRADOR) ───

        // ─── FORZAR CAMBIO DE CONTRASEÑA (PRIMER INGRESO ADMINISTRADOR) ───

        [HttpGet]
        public IActionResult ForzarCambioContrasena()
        {
            if (TempData["PasswordActualizado"] != null)
            {
                ViewBag.PasswordActualizado = true;
            }
            else
            {
                int? idUsuario = HttpContext.Session.GetInt32("UsuarioCambioPasswordId");
                if (idUsuario == null) return RedirectToAction("Login");
            }

            return View("ForzarCambioContrasena", new ForzarCambioViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ForzarCambioContrasena(ForzarCambioViewModel model)
        {
            int? idUsuario = HttpContext.Session.GetInt32("UsuarioCambioPasswordId");
            if (idUsuario == null) return RedirectToAction("Login");

            if (!ModelState.IsValid) return View("ForzarCambioContrasena", model);

            var usuario = _context.Usuarios.FirstOrDefault(u => u.IdUsuario == idUsuario.Value);
            if (usuario == null) return RedirectToAction("Login");

            // Actualizar con la nueva contraseña encriptada
            usuario.ContrasenaUsu = EncriptacionService.Encriptar(model.NuevaContrasena);
            _context.Usuarios.Update(usuario);

            // Eliminar el registro pendiente de la tabla log_Sistema
            var pendienteCambio = _context.LogSistemas
                .FirstOrDefault(l => l.IdUsuario == idUsuario.Value && l.Mensaje == "CAMBIO_CONTRASENA_PENDIENTE");

            if (pendienteCambio != null)
            {
                _context.LogSistemas.Remove(pendienteCambio);
            }

            _context.SaveChanges();

            HttpContext.Session.Remove("UsuarioCambioPasswordId");

            // Activar indicador para mostrar el modal de éxito en la vista
            TempData["PasswordActualizado"] = true;
            return RedirectToAction("ForzarCambioContrasena");
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
            // PASO 1: Validar cédula ingresada
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

                // Guardar datos en sesión para el flujo
                HttpContext.Session.SetString("Rec_Cedula", model.Cedula);
                HttpContext.Session.SetString("Rec_Correo", usuario.CorreoUsu);
                HttpContext.Session.SetString("Rec_Telefono", usuario.TelelfonoUsu.ToString());
                HttpContext.Session.SetString("Rec_Nombres", usuario.NombreCom);

                model.Paso = 2;
                model.CorreoEnmascarado = EnmascararCorreo(usuario.CorreoUsu);
                model.TelefonoEnmascarado = EnmascararTelefono(usuario.TelelfonoUsu.ToString());
                ModelState.Clear();
                return View("RecuperarContraseña", model);
            }

            // PASO 2: El usuario seleccionó el método de envío (correo o teléfono)
            if (model.Paso == 2)
            {
                string? cedula = HttpContext.Session.GetString("Rec_Cedula");
                string? correo = HttpContext.Session.GetString("Rec_Correo");
                string? telefono = HttpContext.Session.GetString("Rec_Telefono");

                if (string.IsNullOrEmpty(cedula)) return RedirectToAction("RecuperarPassword");

                if (string.IsNullOrEmpty(model.MetodoSeleccionado))
                {
                    model.Paso = 2;
                    model.Cedula = cedula;
                    model.CorreoEnmascarado = EnmascararCorreo(correo ?? "");
                    model.TelefonoEnmascarado = EnmascararTelefono(telefono ?? "");
                    ModelState.AddModelError("MetodoSeleccionado", "Debe seleccionar un método de verificación.");
                    return View("RecuperarContraseña", model);
                }

                // Generar código de 6 dígitos y expiración a 5 minutos
                Random rnd = new Random();
                string codigoGenerado = rnd.Next(100000, 999999).ToString();
                DateTime fechaExpiracion = DateTime.Now.AddMinutes(5);

                HttpContext.Session.SetString("Rec_MetodoElegido", model.MetodoSeleccionado);
                HttpContext.Session.SetString("Rec_Expiracion", fechaExpiracion.ToString("o"));
                HttpContext.Session.SetInt32("Rec_IntentosCodigo", 0);
                HttpContext.Session.SetInt32("Rec_ReenviosCodigo", 0);

                if (model.MetodoSeleccionado == "telefono")
                {
                    HttpContext.Session.SetString("Rec_CodigoWhatsAppSimulado", codigoGenerado);
                    ViewBag.CodigoWhatsappSimulado = codigoGenerado;
                }
                else
                {
                    string nombres = HttpContext.Session.GetString("Rec_Nombres") ?? "Usuario";
                    EmailService.EnviarCodigoVerificacion(correo!, nombres, codigoGenerado, HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>());
                    HttpContext.Session.SetString("Rec_CodigoCorreo", codigoGenerado);
                }

                // Calcular reenvíos restantes iniciales (3)
                ViewBag.ReenviosRestantes = 3;

                model.Paso = 3;
                model.Cedula = cedula;
                ModelState.Clear();
                return View("RecuperarContraseña", model);
            }

            // PASO 3: Validar código de verificación (Máx 3 intentos y caducidad de 5 min)
            if (model.Paso == 3)
            {
                string? metodo = HttpContext.Session.GetString("Rec_MetodoElegido");
                string? expiracionStr = HttpContext.Session.GetString("Rec_Expiracion");
                string? cedula = HttpContext.Session.GetString("Rec_Cedula");

                if (string.IsNullOrEmpty(metodo) || string.IsNullOrEmpty(expiracionStr) || string.IsNullOrEmpty(cedula))
                {
                    return RedirectToAction("RecuperarPassword");
                }

                DateTime fechaExpiracion = DateTime.Parse(expiracionStr);
                if (DateTime.Now > fechaExpiracion)
                {
                    ModelState.AddModelError(string.Empty, "El código ha caducado. Inicie la recuperación nuevamente.");
                    model.Paso = 3;
                    model.Cedula = cedula;
                    ViewBag.ReenviosRestantes = 3 - (HttpContext.Session.GetInt32("Rec_ReenviosCodigo") ?? 0);
                    if (metodo == "telefono") ViewBag.CodigoWhatsappSimulado = HttpContext.Session.GetString("Rec_CodigoWhatsAppSimulado");
                    return View("RecuperarContraseña", model);
                }

                string codigoReal = (metodo == "telefono")
                    ? HttpContext.Session.GetString("Rec_CodigoWhatsAppSimulado")!
                    : HttpContext.Session.GetString("Rec_CodigoCorreo")!;

                if (model.CodigoVerificacion != codigoReal)
                {
                    int intentos = HttpContext.Session.GetInt32("Rec_IntentosCodigo") ?? 0;
                    intentos++;
                    HttpContext.Session.SetInt32("Rec_IntentosCodigo", intentos);

                    if (intentos >= 3)
                    {
                        TempData["ErrorRecuperacion"] = "Ha superado el límite de 3 intentos incorrectos. El proceso ha sido cancelado por seguridad.";
                        HttpContext.Session.Clear();
                        return RedirectToAction("Login");
                    }

                    int restantes = 3 - intentos;
                    ModelState.AddModelError("CodigoVerificacion", $"Código incorrecto. Le quedan {restantes} intento(s).");
                    model.Paso = 3;
                    model.Cedula = cedula;
                    ViewBag.ReenviosRestantes = 3 - (HttpContext.Session.GetInt32("Rec_ReenviosCodigo") ?? 0);
                    if (metodo == "telefono") ViewBag.CodigoWhatsappSimulado = codigoReal;
                    return View("RecuperarContraseña", model);
                }

                // Código correcto -> Avanzar al Paso 4 (Nueva contraseña)
                model.Paso = 4;
                model.Cedula = cedula;
                ModelState.Clear();
                return View("RecuperarContraseña", model);
            }

            // PASO 4: Guardar la nueva contraseña encriptada
            if (model.Paso == 4)
            {
                if (ModelState.IsValid)
                {
                    _usuariosRepo.ActualizarContrasena(model.Cedula, model.NuevaContrasena);
                    HttpContext.Session.Clear();
                    TempData["MensajeExito"] = "¡Contraseña actualizada con éxito! Inicie sesión con su nueva clave.";
                    return RedirectToAction("Login");
                }
            }

            return View("RecuperarContraseña", model);
        }

        // ACCIÓN PARA REENVIAR CÓDIGO DE RECUPERACIÓN (Máximo 3 reenvíos)
        [HttpGet]
        public IActionResult ReenviarCodigoRecuperacion()
        {
            string? cedula = HttpContext.Session.GetString("Rec_Cedula");
            string? correo = HttpContext.Session.GetString("Rec_Correo");
            string? metodo = HttpContext.Session.GetString("Rec_MetodoElegido");

            if (string.IsNullOrEmpty(cedula) || string.IsNullOrEmpty(metodo))
                return RedirectToAction("RecuperarPassword");

            int reenvios = HttpContext.Session.GetInt32("Rec_ReenviosCodigo") ?? 0;
            if (reenvios >= 3)
            {
                TempData["ErrorRecuperacion"] = "Ha alcanzado el límite máximo de 3 reenvíos de código.";
                HttpContext.Session.Clear();
                return RedirectToAction("Login");
            }

            reenvios++;
            HttpContext.Session.SetInt32("Rec_ReenviosCodigo", reenvios);

            Random rnd = new Random();
            string codigoGenerado = rnd.Next(100000, 999999).ToString();
            DateTime fechaExpiracion = DateTime.Now.AddMinutes(5);
            HttpContext.Session.SetString("Rec_Expiracion", fechaExpiracion.ToString("o"));

            if (metodo == "telefono")
            {
                HttpContext.Session.SetString("Rec_CodigoWhatsAppSimulado", codigoGenerado);
                TempData["MensajeReenvio"] = "Se ha generado un nuevo código simulado por WhatsApp.";
            }
            else
            {
                string nombres = HttpContext.Session.GetString("Rec_Nombres") ?? "Usuario";
                EmailService.EnviarCodigoVerificacion(correo!, nombres, codigoGenerado, HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>());
                HttpContext.Session.SetString("Rec_CodigoCorreo", codigoGenerado);
                TempData["MensajeReenvio"] = "Se ha reenviado el código a su correo electrónico.";
            }

            // Calcular reenvíos restantes dinámicamente y pasarlos a la vista
            ViewBag.ReenviosRestantes = 3 - reenvios;

            var model = new RecuperaciónContraseñaViewModel
            {
                Paso = 3,
                Cedula = cedula,
                MetodoSeleccionado = metodo
            };
            if (metodo == "telefono") ViewBag.CodigoWhatsappSimulado = codigoGenerado;

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

    // Extensión auxiliar sencilla para almacenar long/Ticks en Session
    public static class SessionExtensions
    {
        public static void SetLong(this ISession session, string key, long value)
        {
            session.Set(key, BitConverter.GetBytes(value));
        }

        public static long? GetLong(this ISession session, string key)
        {
            var data = session.Get(key);
            if (data == null) return null;
            return BitConverter.ToInt64(data, 0);
        }
    }
}