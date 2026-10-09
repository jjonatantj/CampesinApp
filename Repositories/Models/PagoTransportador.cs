using System;
using System.Collections.Generic;

namespace CampesinApp.Repositories.Models;

public partial class PagoTransportador
{
    public int IdConsignacionTrans { get; set; }

    public int IdUsuario { get; set; }

    public int IdDemanda { get; set; }

    public int IdPerfilPago { get; set; }

    public double AnticipoTrans { get; set; }

    public double PagoCompleTrans { get; set; }

    public DateTime? FechaAntiTrans { get; set; }

    public DateTime? FechaPagoTrans { get; set; }

    public virtual Demandum IdDemandaNavigation { get; set; } = null!;

    public virtual PerfilPago IdPerfilPagoNavigation { get; set; } = null!;

    public virtual Usuario IdUsuarioNavigation { get; set; } = null!;
}
