using System.ComponentModel.DataAnnotations;

namespace CampesinApp.Dtos
{
    public class ViajeOpcionDto
    {
        public int IdViaje { get; set; }

        public string Texto { get; set; } = string.Empty;
    }

    /// <summary>Formulario "Filtros del Reporte" de CU-021.</summary>
    public class ReporteUbicacionDto
    {
        [Required]
        public int? IdViaje { get; set; }

        [Required]
        public DateTime? FechaInicial { get; set; }

        [Required]
        public DateTime? FechaFinal { get; set; }

        [Required]
        public string? Region { get; set; }

        [Required, StringLength(100)]
        public string? PuntoOrigen { get; set; }

        [Required, StringLength(100)]
        public string? PuntoDestino { get; set; }

        /// <summary>Nombre del lugar o punto de acopio donde se encuentra el transportador (RN02-CU021).</summary>
        [Required, StringLength(100)]
        public string? Lugar { get; set; }

        [Required]
        public string? Hito { get; set; }

        public List<ViajeOpcionDto> Viajes { get; set; } = new();
    }

    /// <summary>Datos del reporte de ubicación tal como se muestran en las notificaciones y el historial.</summary>
    public class ReporteVistaDto
    {
        public int IdReporteUbicacion { get; set; }

        public int IdDemanda { get; set; }

        public string CodigoPedido { get; set; } = string.Empty;

        public string Lugar { get; set; } = string.Empty;

        public string Hito { get; set; } = string.Empty;

        public DateTime FechaHora { get; set; }

        public bool Guardado { get; set; }
    }

    public class ReporteGuardadoVistaDto
    {
        public int IdDemanda { get; set; }

        public string CodigoPedido { get; set; } = string.Empty;

        public List<ReporteVistaDto> Reportes { get; set; } = new();
    }

    public class ReporteVencidoDto
    {
        public int IdViaje { get; set; }

        public string CodigoPedido { get; set; } = string.Empty;
    }
}
