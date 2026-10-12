using Microsoft.AspNetCore.Mvc;
using CampesinApp.Repositories.Models;
using CampesinApp.ViewModels;
using CampesinApp.Services;
using System.Linq;

namespace CampesinApp.Controllers
{
    [Route("RegistroAdmin")]
    public class RegistroAdminController : Controller
    {
        private readonly Campesinapp3Context _context;

        public RegistroAdminController(Campesinapp3Context context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Index()
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

            // RN-1-CU003: Valida que cédula o correo no existan previamente
            if (int.TryParse(model.Cedula, out int cedulaInt))
            {
                if (_context.Usuarios.Any(u => u.CedulaUsu == cedulaInt))
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
                int.TryParse(model.Telefono.Length > 9 ? model.Telefono.Substring(model.Telefono.Length - 9) : model.Telefono, out int telefonoInt);

                int idMunicipioDefecto = _context.Municipios.Select(m => m.IdMunicipio).FirstOrDefault();

                var nuevoAdmin = new Usuario
                {
                    NombreCom = model.Nombres,
                    ApellidoCom = model.Apellidos,
                    CedulaUsu = int.Parse(model.Cedula),
                    CorreoUsu = model.Correo,
                    TelelfonoUsu = telefonoInt,
                    ContrasenaUsu = EncriptacionService.Encriptar(model.Contrasena), // Encriptación de contraseña
                    IdRol = 4, // Rol Administrador
                    IdMunicipio = idMunicipioDefecto,
                    Estado = true,
                    Imagen = "admin.jpg"
                };

                _context.Usuarios.Add(nuevoAdmin);
                _context.SaveChanges();

                // Notificación por correo institucional en formato HTML limpio
                EmailService.EnviarNotificacionAdmin(model.Correo, model.Nombres);

                // Mensaje flotante según la especificación CU-003
                TempData["MensajeExito"] = "Usuario administrador creado con éxito";
                return RedirectToAction("Index", "RegistroAdmin");
            }

            return View("RegistroAdmin", model);
        }
    }
}