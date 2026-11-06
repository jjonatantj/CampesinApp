namespace CampesinApp.Dtos
{
    /// <summary>Datos de la pantalla "Servicio no disponible" (503) de CampesinApp (Views/Error/Pagina).</summary>
    public class PaginaErrorDto
    {
        public int Codigo { get; set; }

        public string Titulo { get; set; } = string.Empty;

        public string Mensaje { get; set; } = string.Empty;

        /// <summary>Texto corto en mayúsculas, como el que muestra el navegador ("HTTP ERROR 503").</summary>
        public string Detalle { get; set; } = string.Empty;

        /// <summary>Recomendación corta para el usuario (aparece en el recuadro con icono).</summary>
        public string Consejo { get; set; } = string.Empty;

        /// <summary>true = icono de servidor; false = icono de globo.</summary>
        public bool EsDelServidor { get; set; } = true;

        /// <summary>Identificador de la petición; sirve para que soporte encuentre el error en la bitácora.</summary>
        public string? IdSeguimiento { get; set; }

        public static PaginaErrorDto ServicioNoDisponible() => new()
        {
            Codigo = 503,
            Titulo = "Servicio no disponible",
            Mensaje = "CampesinApp no puede atender su solicitud en este momento: el servidor está muy ocupado, en mantenimiento o sin conexión con la base de datos. Inténtelo de nuevo en unos minutos.",
            Detalle = "HTTP ERROR 503",
            Consejo = "Por favor, inténtelo de nuevo más tarde.",
            EsDelServidor = true
        };
    }
}
