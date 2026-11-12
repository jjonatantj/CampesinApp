using CampesinApp.Repositories.Models;
using CampesinApp.Services;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace CampesinApp.Repositories
{
    public class UsuariosRepository
    {
        private readonly Campesinapp3Context _context;

        public UsuariosRepository(Campesinapp3Context context)
        {
            _context = context;
        }

        public Usuario? ValidarCredenciales(string cedula, string contrasenaIngresada)
        {
            if (!int.TryParse(cedula, out int cedulaInt))
                return null;

            // Consultar el usuario activo por cédula
            var usuario = _context.Usuarios
                .Include(u => u.IdRolNavigation)
                .Include(u => u.IdMunicipioNavigation)
                .FirstOrDefault(u => u.CedulaUsu == cedulaInt && u.Estado);

            if (usuario == null) return null;

            if (string.IsNullOrEmpty(contrasenaIngresada) || string.IsNullOrEmpty(usuario.ContrasenaUsu))
            {
                return null;
            }

            // Verificación segura con PBKDF2 (PasswordHasher)
            bool coincide = EncriptacionService.Verificar(usuario.ContrasenaUsu, contrasenaIngresada);

            return coincide ? usuario : null;
        }

        public Usuario? BuscarPorCedula(string cedula)
        {
            if (!int.TryParse(cedula, out int cedulaInt))
                return null;

            return _context.Usuarios
                .Include(u => u.IdRolNavigation)
                .FirstOrDefault(u => u.CedulaUsu == cedulaInt && u.Estado);
        }

        public bool ActualizarContrasena(string cedula, string nuevaContrasena)
        {
            if (!int.TryParse(cedula, out int cedulaInt))
                return false;

            var usuario = _context.Usuarios
                .FirstOrDefault(u => u.CedulaUsu == cedulaInt && u.Estado);

            if (usuario == null) return false;

            // ENCRIPTACIÓN SEGURA DE LA NUEVA CONTRASEÑA ANTES DE GUARDAR
            usuario.ContrasenaUsu = EncriptacionService.Encriptar(nuevaContrasena);
            _context.SaveChanges();
            return true;
        }

        public Usuario? ObtenerPorId(int idUsuario)
        {
            return _context.Usuarios
                .Include(u => u.IdRolNavigation)
                .Include(u => u.IdMunicipioNavigation)
                    .ThenInclude(m => m.IdDepartamentoNavigation)
                .FirstOrDefault(u => u.IdUsuario == idUsuario && u.Estado);
        }
    }
}