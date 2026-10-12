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

            // 1. Desencriptar la contraseña de la BD y comparar
            string contrasenaDesencriptada = EncriptacionService.Desencriptar(usuario.ContrasenaUsu);
            if (contrasenaDesencriptada == contrasenaIngresada)
            {
                return usuario;
            }

            // 2. Comparar directamente si en la BD se guardó encriptada
            string contrasenaIngresadaEncriptada = EncriptacionService.Encriptar(contrasenaIngresada);
            if (usuario.ContrasenaUsu == contrasenaIngresadaEncriptada)
            {
                return usuario;
            }

            return null; // Credenciales inválidas
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