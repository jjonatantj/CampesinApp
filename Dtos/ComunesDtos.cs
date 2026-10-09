using CampesinApp.Repositories.Models;

namespace CampesinApp.Dtos
{
    /// <summary>Usuario con sesión iniciada que ejecuta la petición.</summary>
    public sealed record UsuarioActual(int Id, string Nombre, string Telefono, RolUsuario Rol, string Correo = "", string Municipio = "")
    {
        public bool EsAdmin => Rol == RolUsuario.Administrador;

        public string Cedula { get; init; } = string.Empty;

        /// <summary>Ruta de la foto de perfil guardada en usuario.imagen (puede estar vacía).</summary>
        public string Imagen { get; init; } = string.Empty;
    }

    public enum TipoError
    {
        Ninguno = 0,
        NoEncontrado,
        NoAutorizado,
        Validacion,
        Conflicto,
        Interno
    }

    public class ResultadoOperacion
    {
        public bool Exitoso => Error == TipoError.Ninguno;

        public TipoError Error { get; init; }

        public string? Mensaje { get; init; }

        public static ResultadoOperacion Ok(string? mensaje = null) => new() { Mensaje = mensaje };

        public static ResultadoOperacion Falla(TipoError error, string mensaje) => new() { Error = error, Mensaje = mensaje };
    }

    public class ResultadoOperacion<T> : ResultadoOperacion
    {
        public T? Datos { get; init; }

        public static ResultadoOperacion<T> Ok(T datos, string? mensaje = null) => new() { Datos = datos, Mensaje = mensaje };

        public new static ResultadoOperacion<T> Falla(TipoError error, string mensaje) => new() { Error = error, Mensaje = mensaje };
    }

    public enum TipoAviso
    {
        Informacion,
        Exito,
        Advertencia,
        Error
    }

    /// <summary>Cuadro emergente de aviso (mensaje + botón) que se muestra en la ventana superpuesta.</summary>
    public class AvisoDto
    {
        public string Mensaje { get; set; } = string.Empty;

        public string? Titulo { get; set; }

        public TipoAviso Tipo { get; set; } = TipoAviso.Informacion;

        public string TextoBoton { get; set; } = "Aceptar";

        /// <summary>Si se indica, el aviso ofrece además un botón "Reintentar" que vuelve a ejecutar esta petición.</summary>
        public string? ReintentarUrl { get; set; }

        /// <summary>Ventana que se cierra con el botón: "superpuesta" (por defecto) o "todas".</summary>
        public string Cerrar { get; set; } = "superpuesta";

        /// <summary>Si se indica, el botón además navega a esta URL (página completa).</summary>
        public string? RedirigirA { get; set; }

        /// <summary>Si se indica, al cerrar el aviso se vuelve a cargar esta URL en la ventana principal (p. ej. para refrescar una lista).</summary>
        public string? RecargarPrincipalUrl { get; set; }
    }

    public class PaginaRolDto
    {
        public UsuarioActual? Usuario { get; set; }
    }
}
