namespace CampesinApp.Repositories.Models
{
    public enum RolUsuario
    {
        Ninguno = 0,
        Administrador,
        Campesino,
        Comprador,
        Transportador
    }

    public static class RolUsuarioExtensions
    {
        /// <summary>Convierte el texto de la tabla rol (tipo_Rol) al enum, sin depender de mayúsculas o tildes.</summary>
        public static RolUsuario Desde(string? tipoRol)
        {
            var texto = (tipoRol ?? string.Empty).Trim().ToLowerInvariant();
            if (texto.StartsWith("admin")) return RolUsuario.Administrador;
            if (texto.StartsWith("campes")) return RolUsuario.Campesino;
            if (texto.StartsWith("compra")) return RolUsuario.Comprador;
            if (texto.StartsWith("transp")) return RolUsuario.Transportador;
            return RolUsuario.Ninguno;
        }
    }
}
