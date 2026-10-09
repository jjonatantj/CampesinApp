using System;
using System.Collections.Generic;

namespace CampesinApp.Repositories.Models;

public partial class PrecioEstimado
{
    public int IdPrecioEstimado { get; set; }

    public int IdTipoProducto { get; set; }

    public int IdVariedad { get; set; }

    public int PrecioEsti { get; set; }

    public DateOnly FechaPreci { get; set; }

    public virtual TipoProducto IdTipoProductoNavigation { get; set; } = null!;

    public virtual Variedad IdVariedadNavigation { get; set; } = null!;
}
