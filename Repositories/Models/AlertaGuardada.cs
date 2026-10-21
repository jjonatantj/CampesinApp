using System;
using System.Collections.Generic;

namespace CampesinApp.Repositories.Models;

/// <summary>
/// Alerta de ubicación que el campesino o comprador decidió guardar con "Guardar reporte" (CU-022).
/// Se guarda en la tabla alerta_Guardada y apunta a un reporte de reporte_Transportador.
/// </summary>
public partial class AlertaGuardada
{
    public int IdAlertaGuardada { get; set; }

    public int IdReporte { get; set; }

    public int IdUsuario { get; set; }

    public DateTime FechaGuardado { get; set; }

    public virtual ReporteTransportador IdReporteNavigation { get; set; } = null!;

    public virtual Usuario IdUsuarioNavigation { get; set; } = null!;
}
