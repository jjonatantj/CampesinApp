using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc;
using CampesinApp.Repositories.Models;
using CampesinApp.ViewModels;
using CampesinApp.Services;
using Microsoft.EntityFrameworkCore;

namespace CampesinApp.Controllers
{
    [Route("EliminarUsuario")]
    public class EliminarUsuarioController : Controller
    {
        private readonly Campesinapp3Context _context;
        private readonly IWebHostEnvironment _environment;

        public EliminarUsuarioController(Campesinapp3Context context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            int? idUsuario = HttpContext.Session.GetInt32("IdUsuario");
            if (idUsuario == null)
            {
                return RedirectToAction("Login", "InicioSesion");
            }

            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.IdUsuario == idUsuario.Value && u.Estado);
            if (usuario == null)
            {
                return RedirectToAction("Login", "InicioSesion");
            }

            var model = new EliminarUsuarioViewModel
            {
                Correo = usuario.CorreoUsu,
                CodigoEnviado = HttpContext.Session.GetString("Eliminar_CodigoEnviado") == "true"
            };

            if (model.CodigoEnviado)
            {
                int reenviosActuales = HttpContext.Session.GetInt32("Eliminar_ReenviosCodigo") ?? 0;
                ViewBag.ReenviosRestantes = 3 - reenviosActuales;
                ViewBag.MensajeCodigoEnviado = "Se ha enviado un código de verificación a su correo electrónico.";
                ViewBag.SegundosRestantes = ObtenerSegundosRestantesExpiracion();
            }

            return View("EliminarUsuario", model);
        }

        [HttpGet("Cancelar")]
        public IActionResult CancelarProceso()
        {
            HttpContext.Session.Remove("Eliminar_Codigo");
            HttpContext.Session.Remove("Eliminar_Expiracion");
            HttpContext.Session.Remove("Eliminar_CodigoEnviado");
            HttpContext.Session.Remove("Eliminar_IntentosCodigo");
            HttpContext.Session.Remove("Eliminar_ReenviosCodigo");

            return RedirectToAction("Index", "Perfil");
        }

        [HttpPost("SolicitarCodigo")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SolicitarCodigo(EliminarUsuarioViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.CodigoEnviado = false;
                return View("EliminarUsuario", model);
            }

            int? idUsuario = HttpContext.Session.GetInt32("IdUsuario");
            if (idUsuario == null) return RedirectToAction("Login", "InicioSesion");

            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.IdUsuario == idUsuario.Value && u.Estado);
            if (usuario == null) return RedirectToAction("Login", "InicioSesion");

            model.Correo = usuario.CorreoUsu;

            bool contraseñaValida = EncriptacionService.Verificar(usuario.ContrasenaUsu, model.Contrasena);
            if (!contraseñaValida)
            {
                ModelState.AddModelError("Contrasena", "La contraseña ingresada es incorrecta.");
                model.CodigoEnviado = false;
                return View("EliminarUsuario", model);
            }

            // Generador criptográficamente seguro (CSPRNG) para códigos de verificación (Regla SonarQube S2245)
            string codigoGenerado = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
            DateTime fechaExpiracion = DateTime.Now.AddMinutes(5);

            HttpContext.Session.SetString("Eliminar_Codigo", codigoGenerado);
            HttpContext.Session.SetString("Eliminar_Expiracion", fechaExpiracion.ToString("o"));
            HttpContext.Session.SetString("Eliminar_CodigoEnviado", "true");
            HttpContext.Session.SetInt32("Eliminar_IntentosCodigo", 0);
            HttpContext.Session.SetInt32("Eliminar_ReenviosCodigo", 0);

            EmailService.EnviarCodigoEliminacionCuenta(usuario.CorreoUsu, usuario.NombreCom, codigoGenerado, _environment);

            model.CodigoEnviado = true;
            ViewBag.ReenviosRestantes = 3;
            ViewBag.MensajeCodigoEnviado = "Se ha enviado un código de verificación a su correo electrónico.";
            ViewBag.SegundosRestantes = 300;
            return View("EliminarUsuario", model);
        }

        [HttpPost("ReenviarCodigo")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReenviarCodigo()
        {
            int? idUsuario = HttpContext.Session.GetInt32("IdUsuario");
            if (idUsuario == null) return RedirectToAction("Login", "InicioSesion");

            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.IdUsuario == idUsuario.Value && u.Estado);
            if (usuario == null) return RedirectToAction("Login", "InicioSesion");

            int reenvios = HttpContext.Session.GetInt32("Eliminar_ReenviosCodigo") ?? 0;
            if (reenvios >= 3)
            {
                TempData["ErrorEliminar"] = "Ha alcanzado el límite máximo de 3 reenvíos de código.";
                return RedirectToAction("Index");
            }

            reenvios++;
            HttpContext.Session.SetInt32("Eliminar_ReenviosCodigo", reenvios);

            // Generador criptográficamente seguro (CSPRNG) para códigos de verificación (Regla SonarQube S2245)
            string codigoGenerado = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
            DateTime fechaExpiracion = DateTime.Now.AddMinutes(5);

            HttpContext.Session.SetString("Eliminar_Codigo", codigoGenerado);
            HttpContext.Session.SetString("Eliminar_Expiracion", fechaExpiracion.ToString("o"));

            EmailService.EnviarCodigoEliminacionCuenta(usuario.CorreoUsu, usuario.NombreCom, codigoGenerado, _environment);

            var model = new EliminarUsuarioViewModel
            {
                Correo = usuario.CorreoUsu,
                CodigoEnviado = true
            };

            ViewBag.ReenviosRestantes = 3 - reenvios;
            ViewBag.MensajeCodigoEnviado = "Se ha reenviado un nuevo código a su correo electrónico.";
            ViewBag.SegundosRestantes = 300;
            return View("EliminarUsuario", model);
        }

        [HttpPost("ConfirmarEliminacion")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmarEliminacion(EliminarUsuarioViewModel model)
        {
            int? idUsuario = HttpContext.Session.GetInt32("IdUsuario");
            if (idUsuario == null) return RedirectToAction("Login", "InicioSesion");

            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.IdUsuario == idUsuario.Value);
            if (usuario == null) return RedirectToAction("Login", "InicioSesion");

            model.Correo = usuario.CorreoUsu;
            model.CodigoEnviado = true;

            // En este segundo paso solo se envía y valida el Código de Verificación;
            // la contraseña ya fue verificada en el Paso 1 (SolicitarCodigo), por lo que se remueve de ModelState.
            ModelState.Remove(nameof(model.Contrasena));

            if (string.IsNullOrWhiteSpace(model.CodigoVerificacion))
            {
                ModelState.AddModelError("CodigoVerificacion", "Por favor ingrese el código de verificación.");
            }

            if (!ModelState.IsValid)
            {
                int reenviosActuales = HttpContext.Session.GetInt32("Eliminar_ReenviosCodigo") ?? 0;
                ViewBag.ReenviosRestantes = 3 - reenviosActuales;
                ViewBag.MensajeCodigoEnviado = "Ingrese el código enviado a su correo.";
                ViewBag.SegundosRestantes = ObtenerSegundosRestantesExpiracion();
                return View("EliminarUsuario", model);
            }

            string? codigoSesion = HttpContext.Session.GetString("Eliminar_Codigo");
            string? expiracionStr = HttpContext.Session.GetString("Eliminar_Expiracion");

            if (!string.IsNullOrEmpty(expiracionStr) && DateTime.Now > DateTime.Parse(expiracionStr))
            {
                ModelState.AddModelError("CodigoVerificacion", "El código ha caducado. Solicite uno nuevo.");
                int rRestantes = 3 - (HttpContext.Session.GetInt32("Eliminar_ReenviosCodigo") ?? 0);
                ViewBag.ReenviosRestantes = rRestantes;
                ViewBag.SegundosRestantes = 0;
                ViewBag.MensajeCodigoEnviado = "Ingrese el código enviado a su correo.";
                return View("EliminarUsuario", model);
            }

            if (string.IsNullOrEmpty(model.CodigoVerificacion) || model.CodigoVerificacion != codigoSesion)
            {
                int intentos = HttpContext.Session.GetInt32("Eliminar_IntentosCodigo") ?? 0;
                intentos++;
                HttpContext.Session.SetInt32("Eliminar_IntentosCodigo", intentos);

                if (intentos >= 3)
                {
                    TempData["ErrorEliminar"] = "Ha superado el límite de 3 intentos incorrectos. El proceso ha sido cancelado por seguridad.";
                    HttpContext.Session.Clear();
                    return RedirectToAction("Index");
                }

                int restantes = 3 - intentos;
                ModelState.AddModelError("CodigoVerificacion", $"Código incorrecto. Le quedan {restantes} intento(s).");
                int reenviosActuales = HttpContext.Session.GetInt32("Eliminar_ReenviosCodigo") ?? 0;
                ViewBag.ReenviosRestantes = 3 - reenviosActuales;
                ViewBag.MensajeCodigoEnviado = "Ingrese el código enviado a su correo.";
                ViewBag.SegundosRestantes = ObtenerSegundosRestantesExpiracion();
                return View("EliminarUsuario", model);
            }

            bool tieneDemandasActivas = await _context.Demanda
                .AnyAsync(d => d.IdUsuario == idUsuario.Value && (d.IdEstadoDeman == 1 || d.IdEstadoDeman == 2 || d.IdEstadoDeman == 3));

            if (tieneDemandasActivas)
            {
                ViewBag.BloqueoEliminacion = "No puedes eliminar tu cuenta porque tienes demandas activas o entregas pendientes de finalizar.";
                int reenviosActuales = HttpContext.Session.GetInt32("Eliminar_ReenviosCodigo") ?? 0;
                ViewBag.ReenviosRestantes = 3 - reenviosActuales;
                ViewBag.SegundosRestantes = ObtenerSegundosRestantesExpiracion();
                return View("EliminarUsuario", model);
            }

            // Realizamos la desactivación lógica
            usuario.Estado = false;
            await _context.SaveChangesAsync();

            // Dejamos el mensaje de éxito en TempData y limpiamos la sesión
            TempData["MensajeEliminacion"] = "Tu cuenta ha sido desactivada correctamente.";
            HttpContext.Session.Clear();

            // Retornamos la misma vista pero con el modelo limpio para que el script dispare la alerta de SweetAlert2 antes de salir
            model.CodigoEnviado = false;
            return View("EliminarUsuario", model);
        }

        private int ObtenerSegundosRestantesExpiracion()
        {
            string? expiracionStr = HttpContext.Session.GetString("Eliminar_Expiracion");
            if (string.IsNullOrEmpty(expiracionStr)) return 0;

            if (DateTime.TryParse(expiracionStr, out DateTime fechaExpiracion))
            {
                TimeSpan restante = fechaExpiracion - DateTime.Now;
                return restante.TotalSeconds > 0 ? (int)restante.TotalSeconds : 0;
            }
            return 0;
        }
    }
}