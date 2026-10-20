using System;
using System.Collections.Generic;

namespace CampesinApp.Repositories.Models;

public partial class AlertaGuardadum
{
    public int IdAlertaGuardada { get; set; }

    public int IdReporte { get; set; }

    public int IdUsuario { get; set; }

    public DateTime FechaGuardado { get; set; }

    public virtual ReporteTransportador IdReporteNavigation { get; set; } = null!;

    public virtual Usuario IdUsuarioNavigation { get; set; } = null!;
}
