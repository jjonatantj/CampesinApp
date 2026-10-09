using System;
using System.Collections.Generic;

namespace CampesinApp.Repositories.Models;

public partial class EntidadFinanciera
{
    public int IdEntiFinanc { get; set; }

    public string EntiFinanc { get; set; } = null!;

    public virtual ICollection<PerfilPago> PerfilPagos { get; set; } = new List<PerfilPago>();
}
