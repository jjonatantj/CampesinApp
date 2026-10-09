using CampesinApp.Dtos;
using CampesinApp.Repositories.Models;
using Microsoft.EntityFrameworkCore;

namespace CampesinApp.Services
{
    public interface IUsuarioActualService
    {
        /// <summary>Devuelve el usuario con sesión iniciada o null si es un invitado.</summary>
        Task<UsuarioActual?> ObtenerAsync();
    }

    /// <summary>
    /// Obtiene el usuario de la sesión que crea el inicio de sesión (CU-004, InicioSesionController guarda "IdUsuario" en la
    /// sesión HTTP) y lo consulta en la base de datos, de modo que el rol y el estado siempre son los guardados allí.
    /// </summary>
    public class UsuarioSesionService : IUsuarioActualService
    {
        private const string ClaveCache = "CampesinApp.UsuarioActual";

        private readonly IHttpContextAccessor _http;
        private readonly Campesinapp3Context _db;
        private readonly ILogger<UsuarioSesionService> _logger;

        public UsuarioSesionService(
            IHttpContextAccessor http,
            Campesinapp3Context db,
            ILogger<UsuarioSesionService> logger)
        {
            _http = http;
            _db = db;
            _logger = logger;
        }

        public async Task<UsuarioActual?> ObtenerAsync()
        {
            var contexto = _http.HttpContext;
            if (contexto == null)
            {
                return null;
            }

            if (contexto.Items.TryGetValue(ClaveCache, out var enCache))
            {
                return enCache as UsuarioActual;
            }

            UsuarioActual? usuario = null;
            var idUsuario = contexto.Session.GetInt32("IdUsuario");
            if (idUsuario.HasValue)
            {
                try
                {
                    usuario = await ConsultarAsync(idUsuario.Value);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "No se pudo consultar el usuario de la sesión {IdUsuario}", idUsuario);
                }
            }

            contexto.Items[ClaveCache] = usuario;
            return usuario;
        }

        /// <summary>Usuario activo con rol reconocido, o null.</summary>
        internal async Task<UsuarioActual?> ConsultarAsync(int idUsuario)
        {
            var datos = await _db.Usuarios
                .AsNoTracking()
                .Where(u => u.IdUsuario == idUsuario && u.Estado)
                .Select(u => new
                {
                    u.IdUsuario,
                    u.NombreCom,
                    u.ApellidoCom,
                    u.TelelfonoUsu,
                    u.CorreoUsu,
                    u.CedulaUsu,
                    u.Imagen,
                    TipoRol = u.IdRolNavigation.TipoRol,
                    Municipio = u.IdMunicipioNavigation.Municipio1
                })
                .FirstOrDefaultAsync();

            if (datos == null) return null;

            var rol = RolUsuarioExtensions.Desde(datos.TipoRol);
            if (rol == RolUsuario.Ninguno) return null;

            return new UsuarioActual(
                datos.IdUsuario,
                $"{datos.NombreCom} {datos.ApellidoCom}".Trim(),
                datos.TelelfonoUsu.ToString(),
                rol,
                datos.CorreoUsu,
                datos.Municipio)
            {
                Cedula = datos.CedulaUsu.ToString(),
                Imagen = datos.Imagen
            };
        }
    }
}
