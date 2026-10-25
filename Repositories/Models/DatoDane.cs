using System;
using System.Collections.Generic;

namespace CampesinApp.Repositories.Models;

public partial class DatoDane
{
    public int IdDatoDane { get; set; }

    public int IdVariedad { get; set; }

    public int PrecioDane { get; set; }

    public DateOnly FechaDato { get; set; }

    public decimal? PrecioCorabastos { get; set; }

    public virtual Variedad IdVariedadNavigation { get; set; } = null!;
}
