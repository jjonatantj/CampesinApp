using System;
using System.Collections.Generic;

namespace CampesinApp.Repositories.Models;

public partial class TipoProducto
{
    public int IdTipoProducto { get; set; }

    public string Producto { get; set; } = null!;

    public virtual ICollection<DatoDane> DatoDanes { get; set; } = new List<DatoDane>();

    public virtual ICollection<Demandum> Demanda { get; set; } = new List<Demandum>();

    public virtual ICollection<PrecioEstimado> PrecioEstimados { get; set; } = new List<PrecioEstimado>();

    public virtual ICollection<Producto> Productos { get; set; } = new List<Producto>();

    public virtual ICollection<Variedad> Variedads { get; set; } = new List<Variedad>();
}
