using System.ComponentModel.DataAnnotations;

namespace CampesinApp.Dtos
{
    /// <summary>Detalle del flete de un camión registrado (tarjeta "Fletes y Camiones" de la pantalla principal, CU-023).</summary>
    public class CamionFleteDto
    {
        public int IdCamion { get; set; }

        public string NombreTransportador { get; set; } = string.Empty;

        public string TelefonoTransportador { get; set; } = string.Empty;

        public string Placa { get; set; } = string.Empty;

        public string Modelo { get; set; } = string.Empty;

        public int Capacidad { get; set; }

        public bool PaseAlDia { get; set; }

        public bool SeguroAlDia { get; set; }

        public bool TecnomecanicaAlDia { get; set; }
    }

    /// <summary>"Detalle Logístico del Transporte" de CU-023.</summary>
    public class DetalleTransporteDto
    {
        public int IdDemanda { get; set; }

        public string CodigoPedido { get; set; } = string.Empty;

        public int IdTransportador { get; set; }

        public string NombreTransportador { get; set; } = string.Empty;

        public string TelefonoTransportador { get; set; } = string.Empty;

        public string Placa { get; set; } = string.Empty;

        public string Modelo { get; set; } = string.Empty;

        public int Capacidad { get; set; }

        public string EstadoEnvio { get; set; } = string.Empty;

        /// <summary>Promedio de estrellas del transportador (CU-026) o null si aún no tiene calificaciones.</summary>
        public double? Reputacion { get; set; }

        /// <summary>Si el detalle se abrió desde la búsqueda del administrador, "Regresar" vuelve al perfil.</summary>
        public bool DesdeBusqueda { get; set; }
    }

    public class OpcionDto
    {
        public int Id { get; set; }

        public string Texto { get; set; } = string.Empty;

        public string? Extra { get; set; }
    }

    /// <summary>Formulario "Asignación de Datos de Transporte" de CU-024.</summary>
    public class AsignacionTransporteDto
    {
        [Required]
        public int? IdDemanda { get; set; }

        /// <summary>Camión registrado (tabla camion): de él salen el transportador, la placa, el modelo y la capacidad.</summary>
        [Required]
        public int? IdCamion { get; set; }

        public List<OpcionDto> Pedidos { get; set; } = new();

        public List<OpcionDto> Camiones { get; set; } = new();
    }

    /// <summary>Fila de la pantalla "Mis Viajes" del transportador.</summary>
    public class ViajeTransportadorDto
    {
        public int IdViaje { get; set; }

        public int IdDemanda { get; set; }

        public string CodigoPedido { get; set; } = string.Empty;

        public string Producto { get; set; } = string.Empty;

        public int Cantidad { get; set; }

        public string DireccionFinal { get; set; } = string.Empty;

        public string EstadoEnvio { get; set; } = string.Empty;

        public bool Activo { get; set; }

        public bool LlegadaNotificada { get; set; }
    }
}
