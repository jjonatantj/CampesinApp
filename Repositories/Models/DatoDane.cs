using System;
using System.Collections.Generic;

namespace CampesinApp.Repositories.Models;

public partial class DatoDane
{
    public int IdDatoDane { get; set; }

    public int IdTipoProducto { get; set; }

    public int IdVariedad { get; set; }

    public int PrecioDato { get; set; }

    public DateOnly FechaDato { get; set; }

    public virtual TipoProducto IdTipoProductoNavigation { get; set; } = null!;

    public virtual Variedad IdVariedadNavigation { get; set; } = null!;
}
