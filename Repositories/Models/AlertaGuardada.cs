using System;
using System.Collections.Generic;

namespace CampesinApp.Repositories.Models;

/// <summary>
/// Alerta de ubicación que el campesino o comprador decidió guardar con "Guardar reporte" (CU-022).
/// </summary>
public partial class AlertaGuardada
{
    public int IdAlertaGuardada { get; set; }

    public int IdReporteUbicacion { get; set; }

    public int IdUsuario { get; set; }

    public DateTime FechaGuardado { get; set; }

    public virtual ReporteUbicacion IdReporteUbicacionNavigation { get; set; } = null!;

    public virtual Usuario IdUsuarioNavigation { get; set; } = null!;
}
