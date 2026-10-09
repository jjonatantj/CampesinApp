using System;
using System.Collections.Generic;

namespace CampesinApp.Repositories.Models;

public partial class TipoCuentum
{
    public int IdTipoCuenta { get; set; }

    public string TipoCuenta { get; set; } = null!;

    public virtual ICollection<PerfilPago> PerfilPagos { get; set; } = new List<PerfilPago>();
}
