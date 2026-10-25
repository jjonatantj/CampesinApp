using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using CampesinApp.Repositories.Models;
using CampesinApp.ViewModels;
using CampesinApp.Services;
using Microsoft.EntityFrameworkCore;

namespace CampesinApp.Controllers
{
    [Route("ActualizarPerfil")]
    public class ActualizarPerfilController : Controller
    {
        private readonly Campesinapp3Context _context;

        public ActualizarPerfilController(Campesinapp3Context context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            int? idUsuario = HttpContext.Session.GetInt32("IdUsuario");
            if (idUsuario == null)
            {
                return RedirectToAction("Login", "InicioSesion");
            }

            var usuario = await _context.Usuarios
                .Include(u => u.IdMunicipioNavigation)
                .ThenInclude(m => m.IdDepartamentoNavigation)
                .FirstOrDefaultAsync(u => u.IdUsuario == idUsuario.Value && u.Estado);

            if (usuario == null)
            {
                return RedirectToAction("Login", "InicioSesion");
            }

            var model = new ActualizarPerfilViewModel
            {
                Nombres = usuario.NombreCom,
                Apellidos = usuario.ApellidoCom,
                Cedula = usuario.CedulaUsu.ToString(),
                Correo = usuario.CorreoUsu,
                Telefono = usuario.TelelfonoUsu.ToString(),
                IdMunicipio = usuario.IdMunicipio,
                IdDepartamento = usuario.IdMunicipioNavigation?.IdDepartamento ?? 1
            };

            CargarCombos(model);
            return View("ActualizarPerfil", model);
        }

        [HttpPost("Guardar")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Guardar(ActualizarPerfilViewModel model)
        {
            int? idUsuario = HttpContext.Session.GetInt32("IdUsuario");
            if (idUsuario == null)
            {
                return RedirectToAction("Login", "InicioSesion");
            }

            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.IdUsuario == idUsuario.Value);
            if (usuario == null)
            {
                return RedirectToAction("Login", "InicioSesion");
            }

            // Reasignar campos bloqueados (solo lectura) para que no se pierdan si se recarga la vista
            model.Nombres = usuario.NombreCom;
            model.Apellidos = usuario.ApellidoCom;
            model.Cedula = usuario.CedulaUsu.ToString();

            // Validar que el correo no esté registrado por otro usuario (RN01-CU006)
            if (await _context.Usuarios.AnyAsync(u => u.CorreoUsu == model.Correo && u.IdUsuario != idUsuario.Value))
            {
                ModelState.AddModelError("Correo", "El correo electrónico ya se encuentra registrado por otro usuario.");
            }

            if (ModelState.IsValid)
            {
                usuario.CorreoUsu = model.Correo;
                if (long.TryParse(model.Telefono, out long telLong))
                {
                    usuario.TelelfonoUsu = telLong;
                }
                usuario.IdMunicipio = model.IdMunicipio;

                // Si ingresa una nueva contraseña, la encriptamos con AES-256
                if (!string.IsNullOrEmpty(model.NuevaContrasena))
                {
                    usuario.ContrasenaUsu = EncriptacionService.Encriptar(model.NuevaContrasena);
                }

                _context.Usuarios.Update(usuario);
                await _context.SaveChangesAsync();

                TempData["MensajeActualizacion"] = "Tus datos han sido actualizados correctamente.";
                return RedirectToAction("Index");
            }

            CargarCombos(model);
            return View("ActualizarPerfil", model);
        }

        private void CargarCombos(ActualizarPerfilViewModel model)
        {
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

        [HttpGet("ObtenerMunicipiosPorDepartamento")]
        public JsonResult ObtenerMunicipiosPorDepartamento(int idDepartamento)
        {
            var municipios = _context.Municipios
                .Where(m => m.IdDepartamento == idDepartamento)
                .OrderBy(m => m.Municipio1)
                .Select(m => new { id = m.IdMunicipio, nombre = m.Municipio1 })
                .ToList();

            return Json(municipios);
        }
    }
}
