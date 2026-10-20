using System;
using System.Collections.Generic;

namespace CampesinApp.Repositories.Models;

/// <summary>
/// Reporte manual de ubicación que el transportador registra durante el viaje (CU-021). Tabla reporte_Transportador:
/// ubicacion_Actual guarda el punto de acopio donde se encuentra y fecha_Actual la fecha y hora del reporte.
/// </summary>
public partial class ReporteTransportador
{
    public int IdReporte { get; set; }

    public int IdUsuario { get; set; }

    public int IdDemanda { get; set; }

    public DateTime FechaInicio { get; set; }

    public DateTime FechaFinal { get; set; }

    public DateTime FechaActual { get; set; }

    public string UbicacionActual { get; set; } = null!;

    public string? Region { get; set; }

    public string? PuntoOrigen { get; set; }

    public string? PuntoDestino { get; set; }

    public string? Hito { get; set; }

    public virtual Demandum IdDemandaNavigation { get; set; } = null!;

    public virtual Usuario IdUsuarioNavigation { get; set; } = null!;

    public virtual ICollection<AlertaGuardada> AlertaGuardadas { get; set; } = new List<AlertaGuardada>();
}
