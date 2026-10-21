using System;
using System.Collections.Generic;

namespace CampesinApp.Repositories.Models;

public partial class Camion
{
    public int IdCamion { get; set; }

    public int IdUsuario { get; set; }

    public string PlacaCami { get; set; } = null!;

    public int CapacidadCami { get; set; }

    public string ModeloCami { get; set; } = null!;

    public bool EstadoPase { get; set; }

    public bool EstadoSeguro { get; set; }

    public bool EstadoTecno { get; set; }

    public virtual Usuario IdUsuarioNavigation { get; set; } = null!;
}
