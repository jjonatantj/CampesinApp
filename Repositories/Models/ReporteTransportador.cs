using System;
using System.Collections.Generic;

namespace CampesinApp.Repositories.Models;

public partial class ReporteTransportador
{
    public int IdReporte { get; set; }

    public int IdUsuario { get; set; }

    public int IdDemanada { get; set; }

    public DateTime FechaInicio { get; set; }

    public DateTime FechaFinal { get; set; }

    public DateTime FechaActual { get; set; }

    public DateTime UbucacionActual { get; set; }

    public virtual Demandum IdDemanadaNavigation { get; set; } = null!;

    public virtual Usuario IdUsuarioNavigation { get; set; } = null!;
}
