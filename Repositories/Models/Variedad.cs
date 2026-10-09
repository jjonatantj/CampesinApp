using System;
using System.Collections.Generic;

namespace CampesinApp.Repositories.Models;

public partial class Variedad
{
    public int IdVariedad { get; set; }

    public string VariedadProd { get; set; } = null!;

    public int IdTipoProducto { get; set; }

    public virtual ICollection<DatoDane> DatoDanes { get; set; } = new List<DatoDane>();

    public virtual ICollection<Demandum> Demanda { get; set; } = new List<Demandum>();

    public virtual TipoProducto IdTipoProductoNavigation { get; set; } = null!;

    public virtual ICollection<PrecioEstimado> PrecioEstimados { get; set; } = new List<PrecioEstimado>();

    public virtual ICollection<Producto> Productos { get; set; } = new List<Producto>();
}
