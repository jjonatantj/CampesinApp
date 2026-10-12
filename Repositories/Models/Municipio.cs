using System;
using System.Collections.Generic;

namespace CampesinApp.Repositories.Models;

public partial class Municipio
{
    public int IdMunicipio { get; set; }

    public int IdDepartamento { get; set; }

    public string Municipio1 { get; set; } = null!;

    public virtual Departamento IdDepartamentoNavigation { get; set; } = null!;

    public virtual ICollection<PuntoAcopio> PuntoAcopios { get; set; } = new List<PuntoAcopio>();

    public virtual ICollection<Usuario> Usuarios { get; set; } = new List<Usuario>();
}
