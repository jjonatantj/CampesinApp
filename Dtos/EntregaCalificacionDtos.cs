using System.ComponentModel.DataAnnotations;

namespace CampesinApp.Dtos
{
    /// <summary>Ventana de validación de entrega del comprador (CU-025).</summary>
    public class ValidacionEntregaDto
    {
        public int IdDemanda { get; set; }

        public string CodigoPedido { get; set; } = string.Empty;

        public string Producto { get; set; } = string.Empty;

        public int Cantidad { get; set; }

        public string EstadoEnvio { get; set; } = string.Empty;

        public bool LlegadaNotificada { get; set; }

        public bool YaRespondio { get; set; }
    }

    public enum RespuestaRecepcion
    {
        SinObservaciones,
        ConObservaciones,
        Rechazada
    }

    public class ConfirmarRecepcionDto
    {
        public int IdDemanda { get; set; }

        public RespuestaRecepcion Respuesta { get; set; }

        [StringLength(500)]
        public string? Observacion { get; set; }

        public IFormFile? Evidencia { get; set; }
    }

    /// <summary>Resultado de cruzar la confirmación del transportador con la del comprador.</summary>
    public class ResultadoEntregaDto
    {
        public int IdDemanda { get; set; }

        public string EstadoEnvio { get; set; } = string.Empty;

        public bool EntregaCompletada { get; set; }

        public bool ConNovedad { get; set; }
    }

    public class FormularioCalificacionDto
    {
        public int IdDemanda { get; set; }

        public string CodigoPedido { get; set; } = string.Empty;

        [Required]
        public int? IdUsuarioEvaluado { get; set; }

        [Range(1, 5)]
        public int Estrellas { get; set; }

        [Required, StringLength(500)]
        public string? Comentario { get; set; }

        public List<ParticipantePedido> Pendientes { get; set; } = new();

        public string[] TelefonosSoporte { get; set; } = Array.Empty<string>();

        /// <summary>Se activa cuando se llega desde un desacuerdo de entrega (FE-01-CU025).</summary>
        public bool PorDesacuerdo { get; set; }
    }
}
