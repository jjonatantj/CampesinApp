using System;
using System.Collections.Generic;

namespace CampesinApp.Repositories.Models;

public partial class EstadoDemandum
{
    public int IdEstadoDeman { get; set; }

    public string Estado { get; set; } = null!;

    public virtual ICollection<Demandum> Demanda { get; set; } = new List<Demandum>();
}
