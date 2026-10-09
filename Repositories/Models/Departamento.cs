using System;
using System.Collections.Generic;

namespace CampesinApp.Repositories.Models;

public partial class Departamento
{
    public int IdDepartamento { get; set; }

    public string Departamento1 { get; set; } = null!;

    public virtual ICollection<Municipio> Municipios { get; set; } = new List<Municipio>();
}
