using System;
using System.Collections.Generic;

namespace CampesinApp.Repositories.Models;

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

    public virtual ICollection<AlertaGuardadum> AlertaGuardada { get; set; } = new List<AlertaGuardadum>();

    public virtual Demandum IdDemandaNavigation { get; set; } = null!;

    public virtual Usuario IdUsuarioNavigation { get; set; } = null!;

    public virtual ICollection<Notificacion> Notificacions { get; set; } = new List<Notificacion>();
}
