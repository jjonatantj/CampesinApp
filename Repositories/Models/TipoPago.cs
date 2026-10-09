using System;
using System.Collections.Generic;

namespace CampesinApp.Repositories.Models;

public partial class TipoPago
{
    public int IdTipoPago { get; set; }

    public string TipoPago1 { get; set; } = null!;

    public virtual ICollection<PagoComprador> PagoCompradors { get; set; } = new List<PagoComprador>();

    public virtual ICollection<PerfilPago> PerfilPagos { get; set; } = new List<PerfilPago>();
}
