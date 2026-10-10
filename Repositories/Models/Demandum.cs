using System;
using System.Collections.Generic;

namespace CampesinApp.Repositories.Models;

public partial class Demandum
{
    public int IdDemanda { get; set; }

    public int IdUsuario { get; set; }

    public int IdTipoProducto { get; set; }

    public int IdVariedad { get; set; }

    public int? IdCamion { get; set; }

    public int IdEstadoDeman { get; set; }

    public int CantidadDeman { get; set; }

    public double PrecioDeman { get; set; }

    public DateTime FechaEntrega { get; set; }

    public DateTime LimTiemDeman { get; set; }

    public string DescripcionDeman { get; set; } = null!;

    public string? DireccionInicial { get; set; }

    public int? IdPuntoAcopio { get; set; }

    public virtual ICollection<AporteCamp> AporteCamps { get; set; } = new List<AporteCamp>();

    public virtual EstadoDemandum IdEstadoDemanNavigation { get; set; } = null!;

    public virtual PuntoAcopio? IdPuntoAcopioNavigation { get; set; }

    public virtual TipoProducto IdTipoProductoNavigation { get; set; } = null!;

    public virtual Usuario IdUsuarioNavigation { get; set; } = null!;

    public virtual Variedad IdVariedadNavigation { get; set; } = null!;

    public virtual ICollection<Observacion> Observacions { get; set; } = new List<Observacion>();

    public virtual ICollection<PagoComprador> PagoCompradors { get; set; } = new List<PagoComprador>();

    public virtual ICollection<PagoTransportador> PagoTransportadors { get; set; } = new List<PagoTransportador>();

    public virtual ICollection<ReporteTransportador> ReporteTransportadors { get; set; } = new List<ReporteTransportador>();
}
