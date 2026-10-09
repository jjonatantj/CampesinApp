namespace CampesinApp.Dtos
{
    /// <summary>Un botón del panel de perfil (CU-031).</summary>
    public class OpcionPerfilDto
    {
        public string Texto { get; init; } = string.Empty;

        public string Icono { get; init; } = "bi-circle";

        /// <summary>Si se indica, el botón abre esta URL en una ventana emergente (casos de uso con ventanas).</summary>
        public string? UrlModal { get; init; }

        /// <summary>Si se indica, el botón navega a esta página completa.</summary>
        public string? UrlPagina { get; init; }

        /// <summary>true cuando el módulo de destino es de otro caso de uso que aún no está disponible.</summary>
        public bool PendienteDeOtroModulo => UrlModal == null && UrlPagina == null;

        /// <summary>Estilo del botón: "principal" (verde), "secundario" (blanco con borde) o "peligro".</summary>
        public string Estilo { get; init; } = "secundario";
    }

    public class PerfilDto
    {
        public int Id { get; set; }
        public string Nombres { get; set; } = string.Empty;
        public string Apellidos { get; set; } = string.Empty;
        public string Correo { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public string Cedula { get; set; } = string.Empty;
        public string Rol { get; set; } = string.Empty;
        public string Municipio { get; set; } = string.Empty;

        /// <summary>Ruta web de la foto, o vacío si el usuario no tiene una (se dibujan las iniciales).</summary>
        public string FotoUrl { get; set; } = string.Empty;

        /// <summary>Botones generales (a la derecha de la tarjeta) y los propios del rol.</summary>
        public List<OpcionPerfilDto> OpcionesGenerales { get; set; } = new();

        public List<OpcionPerfilDto> OpcionesRol { get; set; } = new();

        public string Iniciales =>
            string.Concat(new[] { Nombres, Apellidos }
                .Select(p => p.Trim())
                .Where(p => p.Length > 0)
                .Select(p => char.ToUpperInvariant(p[0])));
    }

    /// <summary>Perfil que otro usuario puede consultar (tarjetas de la pantalla principal): datos básicos y reputación.</summary>
    public class PerfilPublicoDto
    {
        public int Id { get; set; }
        public string NombreCompleto { get; set; } = string.Empty;
        public string Rol { get; set; } = string.Empty;
        public string Ubicacion { get; set; } = string.Empty;
        public string FotoUrl { get; set; } = string.Empty;
        public double? PromedioEstrellas { get; set; }
        public int TotalCalificaciones { get; set; }
        public List<string> Comentarios { get; set; } = new();
    }
}
