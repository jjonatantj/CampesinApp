using System;
using System.Collections.Generic;

namespace CampesinApp.Repositories.Models;

/// <summary>
/// Notificación dirigida a un usuario (nueva ubicación, llegada del pedido, entrega completada, novedades).
/// </summary>
public partial class Notificacion
{
    public int IdNotificacion { get; set; }

    public int IdUsuario { get; set; }

    public int? IdDemanda { get; set; }

    public int? IdReporte { get; set; }

    public string Tipo { get; set; } = null!;

    public string Mensaje { get; set; } = null!;

    public bool Leida { get; set; }

    public bool PopupMostrado { get; set; }

    public DateTime Fecha { get; set; }

    public virtual Usuario IdUsuarioNavigation { get; set; } = null!;
}
