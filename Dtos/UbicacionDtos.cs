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

        /// <summary>Puntos de acopio registrados (tabla punto_Acopio) entre los que se elige el lugar y que se dibujan en el mapa.</summary>
        public List<PuntoAcopioOpcionDto> Puntos { get; set; } = new();

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

        /// <summary>Posición del reporte (null si es un reporte anterior a las coordenadas).</summary>
        public double? Latitud { get; set; }

        public double? Longitud { get; set; }

        /// <summary>Recorrido del pedido con coordenadas, en orden cronológico (el último es la posición actual).</summary>
        public List<PuntoMapaDto> Recorrido { get; set; } = new();

        /// <summary>Punto de acopio de entrega del pedido, si lo tiene.</summary>
        public PuntoMapaDto? Destino { get; set; }
    }

    /// <summary>Punto de acopio de la tabla punto_Acopio con las coordenadas que usa Google Maps.</summary>
    public class PuntoAcopioOpcionDto
    {
        public int Id { get; set; }

        public string Nombre { get; set; } = string.Empty;

        public string Direccion { get; set; } = string.Empty;

        public double Lat { get; set; }

        public double Lng { get; set; }
    }

    /// <summary>Punto de un mapa: posición, título y hito, serializado a JSON para mapas.js.</summary>
    public class PuntoMapaDto
    {
        public double Lat { get; set; }

        public double Lng { get; set; }

        public string Titulo { get; set; } = string.Empty;

        public string Hito { get; set; } = string.Empty;

        public string Fecha { get; set; } = string.Empty;

        /// <summary>"reporte" (posición del transportador) o "destino" (punto de acopio de entrega del pedido).</summary>
        public string Tipo { get; set; } = "reporte";
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
