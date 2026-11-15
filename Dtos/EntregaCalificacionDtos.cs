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
        [Required]
        public int? IdDemanda { get; set; }

        [Required]
        public RespuestaRecepcion? Respuesta { get; set; }

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
        [Required]
        public int? IdDemanda { get; set; }

        public string CodigoPedido { get; set; } = string.Empty;

        /// <summary>true para el comprador (estrellas y comentario obligatorios); el transportador solo escribe su comentario.</summary>
        [Required]
        public bool? EsComprador { get; set; }

        [Range(1, 5)]
        public int? Estrellas { get; set; }

        [Required, StringLength(255)]
        public string? Comentario { get; set; }

        public string[] TelefonosSoporte { get; set; } = Array.Empty<string>();

        public string CorreoSoporte { get; set; } = string.Empty;

        /// <summary>Se activa cuando se llega desde un desacuerdo de entrega (FE-01-CU025).</summary>
        [Required]
        public bool? PorDesacuerdo { get; set; }
    }

    /// <summary>"Ver Observaciones" de un pedido finalizado (CU-026): calificación y comentarios guardados en observacion.</summary>
    public class ObservacionesPedidoDto
    {
        public int IdDemanda { get; set; }

        public string CodigoPedido { get; set; } = string.Empty;

        public int Estrellas { get; set; }

        public string? ComentarioComprador { get; set; }

        public bool HayNovedad { get; set; }

        public string? DetalleNovedad { get; set; }

        public string? ComentarioTransportador { get; set; }

        public string? Evidencia { get; set; }

        public DateTime Fecha { get; set; }
    }
}
