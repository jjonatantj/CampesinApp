namespace CampesinApp.Repositories.Models
{
    /// <summary>
    /// Roles del sistema sincronizados con los identificadores de la base de datos (tabla rol).
    /// </summary>
    public enum RolUsuario
    {
        Ninguno = 0,
        Campesino = 1,
        Comprador = 2,
        Transportador = 3,
        Administrador = 4
    }

    public static class RolUsuarioExtensions
    {
        /// <summary>Convierte el texto de la tabla rol (tipo_Rol) o su Id al enum, sin depender de mayúsculas o tildes.</summary>
        public static RolUsuario Desde(string? tipoRol, int idRol = 0)
        {
            if (idRol is >= 1 and <= 4)
            {
                return (RolUsuario)idRol;
            }

            var texto = (tipoRol ?? string.Empty).Trim().ToLowerInvariant();
            if (texto.StartsWith("campes")) return RolUsuario.Campesino;
            if (texto.StartsWith("compra")) return RolUsuario.Comprador;
            if (texto.StartsWith("transp")) return RolUsuario.Transportador;
            if (texto.StartsWith("admin")) return RolUsuario.Administrador;
            return RolUsuario.Ninguno;
        }
    }
}
