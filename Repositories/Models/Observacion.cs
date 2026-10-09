using System;
using System.Collections.Generic;

namespace CampesinApp.Repositories.Models;

public partial class Observacion
{
    public int IdObservacion { get; set; }

    public int IdDemanda { get; set; }

    public string? ObserComprador { get; set; }

    public string? ObserTransportador { get; set; }

    public int CalificacionEstre { get; set; }

    public DateTime HoraObser { get; set; }

    public string? ImagenTranspor { get; set; }

    public string? ImagenComprador { get; set; }

    public virtual Demandum IdDemandaNavigation { get; set; } = null!;
}
