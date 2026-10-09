using System;
using System.Collections.Generic;

namespace CampesinApp.Repositories.Models;

public partial class PagoComprador
{
    public int IdPago { get; set; }

    public int IdTipoPago { get; set; }

    public int IdDemanda { get; set; }

    public int IdPerfilPago { get; set; }

    public int IdUsuario { get; set; }

    public double PagoComprador1 { get; set; }

    public DateTime FechaPagoC { get; set; }

    public virtual Demandum IdDemandaNavigation { get; set; } = null!;

    public virtual PerfilPago IdPerfilPagoNavigation { get; set; } = null!;

    public virtual TipoPago IdTipoPagoNavigation { get; set; } = null!;
}
