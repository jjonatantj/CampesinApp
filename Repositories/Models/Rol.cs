using System;
using System.Collections.Generic;

namespace CampesinApp.Repositories.Models;

public partial class Rol
{
    public int IdRol { get; set; }

    public string TipoRol { get; set; } = null!;

    public virtual ICollection<Usuario> Usuarios { get; set; } = new List<Usuario>();
}
