using System;
using System.Collections.Generic;

namespace CampesinApp.Repositories.Models;

public partial class PerfilPago
{
    public int IdPerfilPago { get; set; }

    public int IdUsuario { get; set; }

    public int IdTipoCuenta { get; set; }

    public int IdTipoPago { get; set; }

    public int IdEntiFinanc { get; set; }

    public string NumCuenta { get; set; } = null!;

    public string Cvc { get; set; } = null!;

    public virtual ICollection<Consignacion> Consignacions { get; set; } = new List<Consignacion>();

    public virtual EntidadFinanciera IdEntiFinancNavigation { get; set; } = null!;

    public virtual TipoCuentum IdTipoCuentaNavigation { get; set; } = null!;

    public virtual TipoPago IdTipoPagoNavigation { get; set; } = null!;

    public virtual Usuario IdUsuarioNavigation { get; set; } = null!;

    public virtual ICollection<PagoComprador> PagoCompradors { get; set; } = new List<PagoComprador>();

    public virtual ICollection<PagoTransportador> PagoTransportadors { get; set; } = new List<PagoTransportador>();
}
