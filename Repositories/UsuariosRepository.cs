using CampesinApp.Repositories.Models;
using CampesinApp.Services;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

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

            // Validación por hash, sin desencriptar nunca lo guardado en la BD: la contraseña ingresada se cifra con el
            // mismo método del registro (EncriptacionService.Encriptar, determinístico) y se compara el resultado con el
            // valor almacenado. La comparación es de tiempo constante para no filtrar información por el tiempo de respuesta.
            if (string.IsNullOrEmpty(contrasenaIngresada) || string.IsNullOrEmpty(usuario.ContrasenaUsu))
            {
                return null;
            }

            string hashIngresado = EncriptacionService.Encriptar(contrasenaIngresada);
            bool coincide = CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(hashIngresado),
                Encoding.UTF8.GetBytes(usuario.ContrasenaUsu));

            return coincide ? usuario : null; // null = credenciales inválidas
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

            // ENCRIPTACIÓN DE LA NUEVA CONTRASEÑA ANTES DE GUARDAR
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