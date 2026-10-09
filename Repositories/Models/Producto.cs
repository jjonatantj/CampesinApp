using System;
using System.Collections.Generic;

namespace CampesinApp.Repositories.Models;

public partial class Producto
{
    public int IdProducto { get; set; }

    public int IdUsuario { get; set; }

    public int IdTipoProducto { get; set; }

    public int IdVariedad { get; set; }

    public string ImagenProduc { get; set; } = null!;

    public string DescripcionProduc { get; set; } = null!;

    public string UbicacionCultivo { get; set; } = null!;

    public virtual TipoProducto IdTipoProductoNavigation { get; set; } = null!;

    public virtual Usuario IdUsuarioNavigation { get; set; } = null!;

    public virtual Variedad IdVariedadNavigation { get; set; } = null!;
}
