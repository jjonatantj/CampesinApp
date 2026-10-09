using System;
using System.Collections.Generic;

namespace CampesinApp.Repositories.Models;

/// <summary>
/// Servicio de transporte asignado a un pedido (demanda). Guarda los datos logísticos
/// capturados por el administrador (CU-024) y el estado del envío (CU-021, CU-023, CU-025).
/// </summary>
public partial class Viaje
{
    public int IdViaje { get; set; }

    public int IdDemanda { get; set; }

    public int IdUsuarioTransportador { get; set; }

    public int IdUsuarioAdmin { get; set; }

    public string NombreTransportador { get; set; } = null!;

    public string TelefonoTransportador { get; set; } = null!;

    public string Placa { get; set; } = null!;

    public string Modelo { get; set; } = null!;

    public int Capacidad { get; set; }

    public string EstadoEnvio { get; set; } = null!;

    public DateTime FechaAsignacion { get; set; }

    public bool LlegadaNotificada { get; set; }

    public DateTime? FechaLlegada { get; set; }

    public bool TransportadorConfirma { get; set; }

    public bool CompradorConfirma { get; set; }

    public bool CompradorConObservaciones { get; set; }

    public string? ObservacionComprador { get; set; }

    public string? ImagenEvidencia { get; set; }

    public bool VisibleTransportador { get; set; }

    public DateTime? FechaEntrega { get; set; }

    public virtual Demandum IdDemandaNavigation { get; set; } = null!;

    public virtual Usuario IdUsuarioTransportadorNavigation { get; set; } = null!;

    public virtual ICollection<ReporteUbicacion> ReporteUbicacions { get; set; } = new List<ReporteUbicacion>();
}
