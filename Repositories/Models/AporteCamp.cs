using System;
using System.Collections.Generic;

namespace CampesinApp.Repositories.Models;

public partial class AporteCamp
{
    public int IdAporte { get; set; }

    public int IdUsuario { get; set; }

    public int IdDemanda { get; set; }

    public int CantAporte { get; set; }

    public string Imagen { get; set; } = null!;

    public int? IdPuntoAcopio { get; set; }

    public virtual ICollection<Consignacion> Consignacions { get; set; } = new List<Consignacion>();

    public virtual Demandum IdDemandaNavigation { get; set; } = null!;

    public virtual PuntoAcopio? IdPuntoAcopioNavigation { get; set; }

    public virtual Usuario IdUsuarioNavigation { get; set; } = null!;
}
