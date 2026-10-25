using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using CampesinApp.Repositories.Models;
using CampesinApp.ViewModels;
using CampesinApp.Services;

namespace CampesinApp.Controllers
{
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
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
            if (TempData["RegistroExitoso"] != null)
            {
                ViewBag.RegistroExitoso = true;
            }

            var model = new RegistroUsuarioViewModel { Paso = 1 };
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
            if (model.Paso == 1)
            {
                ModelState.Remove("MetodoSeleccionado");
                ModelState.Remove("CodigoVerificacion");
                ModelState.Remove("CorreoEnmascarado");
                ModelState.Remove("TelefonoEnmascarado");

                // SEGURIDAD: Impedir registro como Administrador (IdRol 4) por consola
                var rolSeleccionado = _context.Rols.FirstOrDefault(r => r.IdRol == model.IdRol);
                if (rolSeleccionado == null || rolSeleccionado.TipoRol.Equals("Administrador", StringComparison.OrdinalIgnoreCase) || model.IdRol == 4)
                {
                    ModelState.AddModelError("IdRol", "No está autorizado para registrarse con este rol.");
                }

                if (long.TryParse(model.Cedula, out long cedulaLong))
                {
                    if (_context.Usuarios.Any(u => u.CedulaUsu == cedulaLong))
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
                            await model.FotoPerfil.CopyToAsync(stream);
                        }
                    }

                    // Guardar datos temporalmente en Session (sin dirección)
                    HttpContext.Session.SetString("Reg_Nombres", model.Nombres);
                    HttpContext.Session.SetString("Reg_Apellidos", model.Apellidos);
                    HttpContext.Session.SetString("Reg_Cedula", model.Cedula);
                    HttpContext.Session.SetString("Reg_Correo", model.Correo);
                    HttpContext.Session.SetString("Reg_Telefono", model.Telefono);
                    HttpContext.Session.SetString("Reg_Contrasena", model.Contrasena);
                    HttpContext.Session.SetInt32("Reg_IdRol", model.IdRol);
                    HttpContext.Session.SetInt32("Reg_IdMunicipio", model.IdMunicipio);
                    HttpContext.Session.SetString("Reg_Imagen", nombreImagen);

                    model.Paso = 2;
                    model.CorreoEnmascarado = EnmascararCorreo(model.Correo);
                    model.TelefonoEnmascarado = EnmascararTelefono(model.Telefono);
                    ModelState.Clear();
                    return View("RegistroUsuario", model);
                }

                model.Contrasena = string.Empty;
                model.ConfirmarContrasena = string.Empty;
                CargarCombosBD(model);
                return View("RegistroUsuario", model);
            }

            if (model.Paso == 2)
            {
                string? correo = HttpContext.Session.GetString("Reg_Correo");
                string? telefono = HttpContext.Session.GetString("Reg_Telefono");

                if (string.IsNullOrEmpty(correo)) return RedirectToAction("Index");

                if (string.IsNullOrEmpty(model.MetodoSeleccionado))
                {
                    model.Paso = 2;
                    model.CorreoEnmascarado = EnmascararCorreo(correo);
                    model.TelefonoEnmascarado = EnmascararTelefono(telefono ?? "");
                    ModelState.Clear();
                    return View("RegistroUsuario", model);
                }

                Random rnd = new Random();
                string codigoGenerado = rnd.Next(100000, 999999).ToString();
                DateTime fechaExpiracion = DateTime.Now.AddMinutes(5);

                HttpContext.Session.SetString("Reg_MetodoElegido", model.MetodoSeleccionado);
                HttpContext.Session.SetString("Reg_Expiracion", fechaExpiracion.ToString("o"));

                // Inicializar contadores de seguridad para intentos y reenvíos
                HttpContext.Session.SetInt32("Reg_IntentosCodigo", 0);
                HttpContext.Session.SetInt32("Reg_ReenviosCodigo", 0);

                if (model.MetodoSeleccionado == "telefono")
                {
                    HttpContext.Session.SetString("Reg_CodigoWhatsAppSimulado", codigoGenerado);
                    ViewBag.CodigoWhatsappSimulado = codigoGenerado;
                }
                else
                {
                    string nombres = HttpContext.Session.GetString("Reg_Nombres") ?? "Usuario";
                    EmailService.EnviarCodigoVerificacion(correo, nombres, codigoGenerado, _environment);
                    HttpContext.Session.SetString("Reg_CodigoCorreo", codigoGenerado);
                }

                model.Paso = 3;
                model.Correo = correo;
                model.Telefono = telefono ?? "";
                ModelState.Clear();
                return View("RegistroUsuario", model);
            }

            if (model.Paso == 3)
            {
                string? metodo = HttpContext.Session.GetString("Reg_MetodoElegido");
                string? expiracionStr = HttpContext.Session.GetString("Reg_Expiracion");
                string? cedula = HttpContext.Session.GetString("Reg_Cedula");

                if (string.IsNullOrEmpty(metodo) || string.IsNullOrEmpty(expiracionStr) || string.IsNullOrEmpty(cedula))
                {
                    return RedirectToAction("Index");
                }

                DateTime fechaExpiracion = DateTime.Parse(expiracionStr);
                if (DateTime.Now > fechaExpiracion)
                {
                    ModelState.AddModelError(string.Empty, "El código ha caducado. Inicie el registro nuevamente.");
                    model.Paso = 3;
                    if (metodo == "telefono") ViewBag.CodigoWhatsappSimulado = HttpContext.Session.GetString("Reg_CodigoWhatsAppSimulado");
                    return View("RegistroUsuario", model);
                }

                string codigoReal = (metodo == "telefono")
                    ? HttpContext.Session.GetString("Reg_CodigoWhatsAppSimulado")!
                    : HttpContext.Session.GetString("Reg_CodigoCorreo")!;

                if (model.CodigoVerificacion != codigoReal)
                {
                    // Límite máximo de 3 intentos fallidos de código
                    int intentos = HttpContext.Session.GetInt32("Reg_IntentosCodigo") ?? 0;
                    intentos++;
                    HttpContext.Session.SetInt32("Reg_IntentosCodigo", intentos);

                    if (intentos >= 3)
                    {
                        TempData["ErrorRegistro"] = "Ha superado el límite de 3 intentos incorrectos. Su registro ha sido cancelado por seguridad.";
                        HttpContext.Session.Clear();
                        return RedirectToAction("Index");
                    }

                    int restantes = 3 - intentos;
                    ModelState.AddModelError("CodigoVerificacion", $"Código incorrecto. Le quedan {restantes} intento(s).");
                    model.Paso = 3;
                    if (metodo == "telefono") ViewBag.CodigoWhatsappSimulado = codigoReal;
                    return View("RegistroUsuario", model);
                }

                string nombres = HttpContext.Session.GetString("Reg_Nombres")!;
                string apellidos = HttpContext.Session.GetString("Reg_Apellidos")!;
                string correo = HttpContext.Session.GetString("Reg_Correo")!;
                string telefonoStr = HttpContext.Session.GetString("Reg_Telefono") ?? "3000000000";
                string contrasena = HttpContext.Session.GetString("Reg_Contrasena")!;
                int idRol = HttpContext.Session.GetInt32("Reg_IdRol") ?? 1;
                int idMunicipio = HttpContext.Session.GetInt32("Reg_IdMunicipio") ?? 1;
                string imagen = HttpContext.Session.GetString("Reg_Imagen") ?? "default_user.png";

                var rolVerificacion = _context.Rols.FirstOrDefault(r => r.IdRol == idRol);
                if (rolVerificacion == null || rolVerificacion.TipoRol.Equals("Administrador", StringComparison.OrdinalIgnoreCase) || idRol == 4)
                {
                    idRol = 1;
                }

                long.TryParse(telefonoStr, out long telefonoLong);
                var nuevoUsuario = new Usuario
                {
                    NombreCom = nombres,
                    ApellidoCom = apellidos,
                    CedulaUsu = int.Parse(cedula!),
                    CorreoUsu = correo,
                    TelelfonoUsu = telefonoLong,
                    ContrasenaUsu = EncriptacionService.Encriptar(contrasena),
                    IdRol = idRol,
                    IdMunicipio = idMunicipio,
                    Estado = true,
                    Imagen = imagen
                };

                _context.Usuarios.Add(nuevoUsuario);
                _context.SaveChanges();

                HttpContext.Session.Clear();

                TempData["RegistroExitoso"] = true;
                return RedirectToAction("Index");
            }

            return View("RegistroUsuario", model);
        }

        [HttpGet]
        public IActionResult ReenviarCodigoRegistro()
        {
            string? correo = HttpContext.Session.GetString("Reg_Correo");
            string? telefono = HttpContext.Session.GetString("Reg_Telefono");
            string? metodo = HttpContext.Session.GetString("Reg_MetodoElegido");

            if (string.IsNullOrEmpty(correo) || string.IsNullOrEmpty(metodo))
                return RedirectToAction("Index");

            // Validar límite máximo de 3 reenvíos
            int reenvios = HttpContext.Session.GetInt32("Reg_ReenviosCodigo") ?? 0;
            if (reenvios >= 3)
            {
                TempData["ErrorRegistro"] = "Ha alcanzado el límite máximo de 3 reenvíos de código. Inicie el registro nuevamente.";
                HttpContext.Session.Clear();
                return RedirectToAction("Index");
            }

            reenvios++;
            HttpContext.Session.SetInt32("Reg_ReenviosCodigo", reenvios);

            Random rnd = new Random();
            string codigoGenerado = rnd.Next(100000, 999999).ToString();
            DateTime fechaExpiracion = DateTime.Now.AddMinutes(5);
            HttpContext.Session.SetString("Reg_Expiracion", fechaExpiracion.ToString("o"));

            if (metodo == "telefono")
            {
                HttpContext.Session.SetString("Reg_CodigoWhatsAppSimulado", codigoGenerado);
                TempData["MensajeReenvio"] = "Se ha generado un nuevo código simulado por WhatsApp.";
            }
            else
            {
                string nombres = HttpContext.Session.GetString("Reg_Nombres") ?? "Usuario";
                EmailService.EnviarCodigoVerificacion(correo, nombres, codigoGenerado, _environment);
                HttpContext.Session.SetString("Reg_CodigoCorreo", codigoGenerado);
                TempData["MensajeReenvio"] = "Se ha reenviado el código a su correo electrónico.";
            }

            // Forzar retorno al Paso 3 manteniendo el estado
            var model = new RegistroUsuarioViewModel
            {
                Paso = 3,
                Correo = correo,
                Telefono = telefono ?? "",
                MetodoSeleccionado = metodo
            };
            if (metodo == "telefono") ViewBag.CodigoWhatsappSimulado = codigoGenerado;

            return View("RegistroUsuario", model);
        }

        [HttpGet]
        public IActionResult CambiarMetodo()
        {
            string? correo = HttpContext.Session.GetString("Reg_Correo");
            string? telefono = HttpContext.Session.GetString("Reg_Telefono");

            if (string.IsNullOrEmpty(correo)) return RedirectToAction("Index");

            var model = new RegistroUsuarioViewModel
            {
                Paso = 2,
                CorreoEnmascarado = EnmascararCorreo(correo),
                TelefonoEnmascarado = EnmascararTelefono(telefono ?? "")
            };

            return View("RegistroUsuario", model);
        }

        [HttpGet]
        public IActionResult VolverARegistro()
        {
            string? nombres = HttpContext.Session.GetString("Reg_Nombres");

            if (string.IsNullOrEmpty(nombres))
            {
                return RedirectToAction("Index");
            }

            var model = new RegistroUsuarioViewModel
            {
                Paso = 1,
                Nombres = nombres,
                Apellidos = HttpContext.Session.GetString("Reg_Apellidos") ?? "",
                Cedula = HttpContext.Session.GetString("Reg_Cedula") ?? "",
                Correo = HttpContext.Session.GetString("Reg_Correo") ?? "",
                Telefono = HttpContext.Session.GetString("Reg_Telefono") ?? "",
                IdRol = HttpContext.Session.GetInt32("Reg_IdRol") ?? 0,
                IdDepartamento = HttpContext.Session.GetInt32("Reg_IdDepartamento") ?? 0,
                IdMunicipio = HttpContext.Session.GetInt32("Reg_IdMunicipio") ?? 0
            };

            CargarCombosBD(model);
            return View("RegistroUsuario", model);
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

        private string EnmascararCorreo(string correo)
        {
            if (string.IsNullOrEmpty(correo) || !correo.Contains("@")) return correo;
            var partes = correo.Split('@');
            var nombre = partes[0];
            var dominio = partes[1];
            if (nombre.Length <= 2) return "***@" + dominio;
            return nombre.Substring(0, 2 + 1) + "***@" + dominio;
        }

        private string EnmascararTelefono(string telefono)
        {
            if (string.IsNullOrEmpty(telefono) || telefono.Length < 4) return telefono;
            return new string('*', telefono.Length - 4) + telefono.Substring(telefono.Length - 4);
        }
    }
}