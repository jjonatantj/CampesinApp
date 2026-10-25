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

        public EliminarUsuarioController(Campesinapp3Context context)
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

            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.IdUsuario == idUsuario.Value && u.Estado);
            if (usuario == null)
            {
                return RedirectToAction("Login", "InicioSesion");
            }

            var model = new EliminarUsuarioViewModel
            {
                Correo = usuario.CorreoUsu
            };

            return View("EliminarUsuario", model);
        }

        [HttpPost("SolicitarCodigo")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SolicitarCodigo(EliminarUsuarioViewModel model)
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

            model.Correo = usuario.CorreoUsu;

            string passEncriptada = EncriptacionService.Encriptar(model.Contrasena);
            if (usuario.ContrasenaUsu != passEncriptada)
            {
                ModelState.AddModelError("Contrasena", "La contraseña ingresada es incorrecta.");
                return View("EliminarUsuario", model);
            }

            string codigoGenerado = new Random().Next(100000, 999999).ToString();
            HttpContext.Session.SetString("CodigoEliminarCuenta", codigoGenerado);

            // LLAMADO AL NUEVO MÉTODO CON EL MENSAJE DE ELIMINACIÓN
            EmailService.EnviarCodigoEliminacionCuenta(usuario.CorreoUsu, usuario.NombreCom, codigoGenerado);

            model.CodigoEnviado = true;
            ViewBag.MensajeCodigoEnviado = "Se ha enviado un código de verificación a su correo electrónico.";
            return View("EliminarUsuario", model);
        }

        [HttpPost("ConfirmarEliminacion")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmarEliminacion(EliminarUsuarioViewModel model)
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

            model.Correo = usuario.CorreoUsu;

            string? codigoSesion = HttpContext.Session.GetString("CodigoEliminarCuenta");
            if (string.IsNullOrEmpty(model.CodigoVerificacion) || model.CodigoVerificacion != codigoSesion)
            {
                ModelState.AddModelError("CodigoVerificacion", "Código de verificación incorrecto, intente de nuevo.");
                model.CodigoEnviado = true;
                return View("EliminarUsuario", model);
            }

            // Regla de negocio RN01-CU007: Validar demandas o transacciones activas
            bool tieneDemandasActivas = await _context.Demanda
                .AnyAsync(d => d.IdUsuario == idUsuario.Value && (d.IdEstadoDeman == 1 || d.IdEstadoDeman == 2));

            if (tieneDemandasActivas)
            {
                ViewBag.BloqueoEliminacion = "No puedes eliminar tu cuenta porque tienes demandas activas o entregas pendientes de finalizar.";
                model.CodigoEnviado = true;
                return View("EliminarUsuario", model);
            }

            // Borrado lógico: Cambiar estado a 0 (false)
            usuario.Estado = false;
            await _context.SaveChangesAsync();

            // Limpiar la sesión HTTP
            HttpContext.Session.Clear();
            TempData["MensajeCuentaEliminada"] = "Tu cuenta ha sido desactivada correctamente.";

            // REDIRECCIÓN A LA ACCIÓN BIENVENIDA DEL CONTROLADOR PAGINABIENVENIDA
            return RedirectToAction("Bienvenida", "PaginaBienvenida");
        }
    }
}