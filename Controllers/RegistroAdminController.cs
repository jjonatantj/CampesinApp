using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using CampesinApp.Repositories.Models;
using CampesinApp.ViewModels;
using CampesinApp.Services;
using System.Linq;
using System;
using System.Security.Cryptography; // <-- Espacio de nombres necesario para la seguridad

namespace CampesinApp.Controllers
{
    [Route("RegistroAdmin")]
    public class RegistroAdminController : Controller
    {
        private readonly Campesinapp3Context _context;
        private readonly IWebHostEnvironment _environment;

        public RegistroAdminController(Campesinapp3Context context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        [HttpGet]
        public IActionResult Index(bool? exito)
        {
            int? idUsuario = HttpContext.Session.GetInt32("IdUsuario");
            if (idUsuario == null)
            {
                return RedirectToAction("Login", "InicioSesion");
            }

            var usuario = _context.Usuarios.FirstOrDefault(u => u.IdUsuario == idUsuario.Value && u.Estado);
            if (usuario == null || usuario.IdRol != 4)
            {
                return RedirectToAction("Index", "Perfil");
            }

            // El modal solo se activa si la URL trae el parámetro de éxito
            if (exito == true)
            {
                ViewBag.MostrarModalExito = true;
            }

            var model = new RegistroAdminViewModel();
            return View("RegistroAdmin", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Index(RegistroAdminViewModel model)
        {
            int? idUsuario = HttpContext.Session.GetInt32("IdUsuario");
            if (idUsuario == null)
            {
                return RedirectToAction("Login", "InicioSesion");
            }

            var usuarioSesion = _context.Usuarios.FirstOrDefault(u => u.IdUsuario == idUsuario.Value && u.Estado);
            if (usuarioSesion == null || usuarioSesion.IdRol != 4)
            {
                return RedirectToAction("Index", "Perfil");
            }

            if (long.TryParse(model.Cedula, out long cedulaLong))
            {
                if (_context.Usuarios.Any(u => u.CedulaUsu == cedulaLong))
                {
                    ModelState.AddModelError("Cedula", "El número de documento ya se encuentra registrado.");
                }
            }

            if (_context.Usuarios.Any(u => u.CorreoUsu == model.Correo))
            {
                ModelState.AddModelError("Correo", "El correo electrónico ya se encuentra registrado.");
            }

            if (ModelState.IsValid)
            {
                long.TryParse(model.Telefono, out long telefonoLong);
                int idMunicipioDefecto = _context.Municipios.Select(m => m.IdMunicipio).FirstOrDefault();

                // Solución al aviso S2245: Uso de generador criptográfico seguro
                int randomPin = RandomNumberGenerator.GetInt32(1000, 10000);
                string passwordTemporal = "Admin" + randomPin;

                var nuevoAdmin = new Usuario
                {
                    NombreCom = model.Nombres,
                    ApellidoCom = model.Apellidos,
                    CedulaUsu = int.Parse(model.Cedula),
                    CorreoUsu = model.Correo,
                    TelelfonoUsu = telefonoLong,
                    ContrasenaUsu = EncriptacionService.Encriptar(passwordTemporal),
                    IdRol = 4,
                    IdMunicipio = idMunicipioDefecto,
                    Estado = true,
                    Imagen = "admin.jpg"
                };

                _context.Usuarios.Add(nuevoAdmin);
                _context.SaveChanges();

                var logPendiente = new LogSistema
                {
                    Fecha = DateTime.Now,
                    Nivel = "INFO",
                    IdUsuario = nuevoAdmin.IdUsuario,
                    Origen = "RegistroAdmin",
                    Mensaje = "CAMBIO_CONTRASENA_PENDIENTE"
                };
                _context.LogSistemas.Add(logPendiente);
                _context.SaveChanges();
                // Enviar la notificación por correo con la cédula y la contraseña temporal generada
                EmailService.EnviarNotificacionAdmin(model.Correo, model.Nombres, model.Cedula, passwordTemporal, _environment);

                // Redirigir enviando el indicador de éxito en la URL de forma segura
                return RedirectToAction("Index", new { exito = true });
            }

            return View("RegistroAdmin", model);
        }
    }
}