using System;
using System.Collections.Generic;

namespace CampesinApp.Repositories.Models;

/// <summary>
/// Reporte manual de ubicación registrado por el transportador durante el viaje (CU-021).
/// </summary>
public partial class ReporteUbicacion
{
    public int IdReporteUbicacion { get; set; }

    public int IdViaje { get; set; }

    public int IdDemanda { get; set; }

    public int IdUsuario { get; set; }

    public DateTime FechaInicial { get; set; }

    public DateTime FechaFinal { get; set; }

    public string Region { get; set; } = null!;

    public string PuntoOrigen { get; set; } = null!;

    public string PuntoDestino { get; set; } = null!;

    public string Lugar { get; set; } = null!;

    public string Hito { get; set; } = null!;

    public DateTime FechaHora { get; set; }

    public virtual Viaje IdViajeNavigation { get; set; } = null!;

    public virtual Usuario IdUsuarioNavigation { get; set; } = null!;

    public virtual ICollection<AlertaGuardada> AlertaGuardadas { get; set; } = new List<AlertaGuardada>();
}
