using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Http;
using CampesinApp.Repositories.Models;
using CampesinApp.ViewModels;
using CampesinApp.Services;
using System.Linq;
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;

namespace CampesinApp.Controllers
{
    public class RegistroUsuarioController : Controller
    {
        private readonly Campesinapp3Context _context;
        private readonly IWebHostEnvironment _environment;

        public RegistroUsuarioController(Campesinapp3Context context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        [HttpGet]
        public IActionResult Index()
        {
            var model = new RegistroUsuarioViewModel();
            CargarCombosBD(model);
            return View("RegistroUsuario", model);
        }

        [HttpGet]
        public JsonResult ObtenerMunicipiosPorDepartamento(int idDepartamento)
        {
            var municipios = _context.Municipios
                .Where(m => m.IdDepartamento == idDepartamento)
                .OrderBy(m => m.Municipio1)
                .Select(m => new { id = m.IdMunicipio, nombre = m.Municipio1 })
                .ToList();

            return Json(municipios);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(RegistroUsuarioViewModel model)
        {
            if (int.TryParse(model.Cedula, out int cedulaInt))
            {
                if (_context.Usuarios.Any(u => u.CedulaUsu == cedulaInt))
                {
                    ModelState.AddModelError("Cedula", "La cédula ingresada ya se encuentra registrada.");
                }
            }

            if (_context.Usuarios.Any(u => u.CorreoUsu == model.Correo))
            {
                ModelState.AddModelError("Correo", "El correo electrónico ingresado ya se encuentra registrado.");
            }

            if (ModelState.IsValid)
            {
                string nombreImagen = "default_user.png";
                if (model.FotoPerfil != null && model.FotoPerfil.Length > 0)
                {
                    string folder = Path.Combine(_environment.WebRootPath, "images/perfiles");
                    if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

                    nombreImagen = Guid.NewGuid().ToString() + Path.GetExtension(model.FotoPerfil.FileName);
                    string filePath = Path.Combine(folder, nombreImagen);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        model.FotoPerfil.CopyTo(stream);
                    }
                }

                // Generar código de Correo y código de WhatsApp (Teléfono)
                Random rnd = new Random();
                string codigoCorreo = rnd.Next(100000, 999999).ToString();
                string codigoSms = rnd.Next(100000, 999999).ToString();

                // Límite de caducidad estricto a los 5 minutos
                DateTime fechaExpiracion = DateTime.Now.AddMinutes(5);

                // 1. ENVIAR CORREO REAL CON MAILKIT
                EmailService.EnviarCodigoVerificacion(model.Correo, model.Nombres, codigoCorreo);

                // 2. ENVIAR CÓDIGO POR WHATSAPP API DE META
                await WhatsAppService.EnviarCodigoWhatsAppAsync(model.Telefono, codigoSms);

                // Guardar información temporal en Session
                HttpContext.Session.SetString("Reg_Nombres", model.Nombres);
                HttpContext.Session.SetString("Reg_Apellidos", model.Apellidos);
                HttpContext.Session.SetString("Reg_Cedula", model.Cedula);
                HttpContext.Session.SetString("Reg_Correo", model.Correo);
                HttpContext.Session.SetString("Reg_Telefono", model.Telefono);
                HttpContext.Session.SetString("Reg_Contrasena", model.Contrasena);
                HttpContext.Session.SetInt32("Reg_IdRol", model.IdRol);
                HttpContext.Session.SetInt32("Reg_IdMunicipio", model.IdMunicipio);
                HttpContext.Session.SetString("Reg_Imagen", nombreImagen);

                HttpContext.Session.SetString("Reg_CodigoCorreo", codigoCorreo);
                HttpContext.Session.SetString("Reg_CodigoTelefono", codigoSms);
                HttpContext.Session.SetString("Reg_Expiracion", fechaExpiracion.ToString("o"));

                return RedirectToAction("Verificacion");
            }

            CargarCombosBD(model);
            return View("RegistroUsuario", model);
        }

        [HttpGet]
        public IActionResult Verificacion()
        {
            string? correo = HttpContext.Session.GetString("Reg_Correo");
            string? telefono = HttpContext.Session.GetString("Reg_Telefono");

            if (string.IsNullOrEmpty(correo))
            {
                return RedirectToAction("Index");
            }

            var model = new VerificacionCodigoViewModel
            {
                Correo = correo,
                Telefono = telefono ?? ""
            };

            // La vista 'VerificacionCodigo.cshtml' solo captura los dos inputs del usuario.
            // Se eliminó ViewBag.CodigoSmsPrueba para evitar quemar datos en pantalla.

            return View("VerificacionCodigo", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Verificacion(VerificacionCodigoViewModel model)
        {
            string? codigoCorreoReal = HttpContext.Session.GetString("Reg_CodigoCorreo");
            string? codigoTelefonoReal = HttpContext.Session.GetString("Reg_CodigoTelefono");
            string? expiracionStr = HttpContext.Session.GetString("Reg_Expiracion");
            string? cedula = HttpContext.Session.GetString("Reg_Cedula");

            if (string.IsNullOrEmpty(codigoCorreoReal) || string.IsNullOrEmpty(expiracionStr))
            {
                return RedirectToAction("Index");
            }

            // 1. VALIDACIÓN DE CADUCIDAD A LOS 5 MINUTOS
            DateTime fechaExpiracion = DateTime.Parse(expiracionStr);
            if (DateTime.Now > fechaExpiracion)
            {
                ModelState.AddModelError(string.Empty, "Los códigos han caducado (tiempo límite de 5 minutos excedido). Inicie el registro nuevamente.");
                return View("VerificacionCodigo", model);
            }

            // 2. VALIDACIÓN DE CÓDIGOS RECIBIDOS EN CORREO Y WHATSAPP
            bool correoValido = (model.Codigo == codigoCorreoReal);
            bool telefonoValido = (model.CodigoTelefono == codigoTelefonoReal);

            if (!correoValido && !telefonoValido)
            {
                ModelState.AddModelError(string.Empty, "Ambos códigos (correo y WhatsApp) son incorrectos.");
            }
            else if (!correoValido)
            {
                ModelState.AddModelError("Codigo", "El código enviado al correo electrónico es incorrecto.");
            }
            else if (!telefonoValido)
            {
                ModelState.AddModelError("CodigoTelefono", "El código enviado a tu WhatsApp es incorrecto.");
            }
            else
            {
                // AMBOS CÓDIGOS SON CORRECTOS Y DENTRO DEL TIEMPO -> GUARDAR EN BD
                string nombres = HttpContext.Session.GetString("Reg_Nombres")!;
                string apellidos = HttpContext.Session.GetString("Reg_Apellidos")!;
                string correo = HttpContext.Session.GetString("Reg_Correo")!;
                string telefono = HttpContext.Session.GetString("Reg_Telefono")!;
                string contrasena = HttpContext.Session.GetString("Reg_Contrasena")!;
                int idRol = HttpContext.Session.GetInt32("Reg_IdRol") ?? 1;
                int idMunicipio = HttpContext.Session.GetInt32("Reg_IdMunicipio") ?? 1;
                string imagen = HttpContext.Session.GetString("Reg_Imagen")!;

                int.TryParse(telefono.Length > 9 ? telefono.Substring(telefono.Length - 9) : telefono, out int telefonoInt);

                var nuevoUsuario = new Usuario
                {
                    NombreCom = nombres,
                    ApellidoCom = apellidos,
                    CedulaUsu = int.Parse(cedula!),
                    CorreoUsu = correo,
                    TelelfonoUsu = telefonoInt,
                    ContrasenaUsu = EncriptacionService.Encriptar(contrasena),
                    IdRol = idRol,
                    IdMunicipio = idMunicipio,
                    Estado = true,
                    Imagen = imagen
                };

                _context.Usuarios.Add(nuevoUsuario);
                _context.SaveChanges();

                HttpContext.Session.Clear();

                TempData["MensajeExito"] = "¡Cuenta verificada y registrada con éxito en la base de datos!";
                return RedirectToAction("Login", "InicioSesion");
            }

            return View("VerificacionCodigo", model);
        }

        private void CargarCombosBD(RegistroUsuarioViewModel model)
        {
            model.ListaRoles = _context.Rols
                .Where(r => r.TipoRol != "Administrador" && r.IdRol != 4)
                .Select(r => new SelectListItem { Value = r.IdRol.ToString(), Text = r.TipoRol })
                .ToList();

            model.ListaDepartamentos = _context.Departamentos
                .Select(d => new SelectListItem { Value = d.IdDepartamento.ToString(), Text = d.Departamento1 })
                .ToList();

            if (model.IdDepartamento > 0)
            {
                model.ListaMunicipios = _context.Municipios
                    .Where(m => m.IdDepartamento == model.IdDepartamento)
                    .Select(m => new SelectListItem { Value = m.IdMunicipio.ToString(), Text = m.Municipio1 })
                    .ToList();
            }
        }
    }
}