using System;
using System.Collections.Generic;

namespace CampesinApp.Repositories.Models;

public partial class Consignacion
{
    public int IdConsignacionCam { get; set; }

    public int IdUsuario { get; set; }

    public int IdAporte { get; set; }

    public int IdPerfilPago { get; set; }

    public double ConsigCampesino { get; set; }

    public DateTime FechaConsigCam { get; set; }

    public virtual AporteCamp IdAporteNavigation { get; set; } = null!;

    public virtual PerfilPago IdPerfilPagoNavigation { get; set; } = null!;

    public virtual Usuario IdUsuarioNavigation { get; set; } = null!;
}
