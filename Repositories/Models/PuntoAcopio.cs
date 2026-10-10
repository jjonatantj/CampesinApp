using System;
using System.Collections.Generic;

namespace CampesinApp.Repositories.Models;

public partial class PuntoAcopio
{
    public int IdPuntoAcopio { get; set; }

    public int IdMunicipio { get; set; }

    public string NombrePunto { get; set; } = null!;

    public string Direccion { get; set; } = null!;

    public string Telefono { get; set; } = null!;

    public string HorarioAtencion { get; set; } = null!;

    public decimal Latitud { get; set; }

    public decimal Longitud { get; set; }

    public decimal Capacidad { get; set; }

    public bool AccesoVehicular { get; set; }

    public DateTime FechaRegistro { get; set; }

    public virtual ICollection<AporteCamp> AporteCamps { get; set; } = new List<AporteCamp>();

    public virtual ICollection<Demandum> Demanda { get; set; } = new List<Demandum>();

    public virtual Municipio IdMunicipioNavigation { get; set; } = null!;

    public virtual ICollection<Usuario> IdUsuarios { get; set; } = new List<Usuario>();
}
