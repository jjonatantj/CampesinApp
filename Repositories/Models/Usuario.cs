using System;
using System.Collections.Generic;

namespace CampesinApp.Repositories.Models;

public partial class Usuario
{
    public int IdUsuario { get; set; }

    public int IdRol { get; set; }

    public int IdMunicipio { get; set; }

    public string CorreoUsu { get; set; } = null!;

    public string ContrasenaUsu { get; set; } = null!;

    public int CedulaUsu { get; set; }

    public string NombreCom { get; set; } = null!;

    public string ApellidoCom { get; set; } = null!;

    public long TelelfonoUsu { get; set; }

    public bool Estado { get; set; }

    public string Imagen { get; set; } = null!;

    public virtual ICollection<AlertaGuardadum> AlertaGuardada { get; set; } = new List<AlertaGuardadum>();

    public virtual ICollection<AporteCamp> AporteCamps { get; set; } = new List<AporteCamp>();

    public virtual ICollection<Camion> Camions { get; set; } = new List<Camion>();

    public virtual ICollection<Consignacion> Consignacions { get; set; } = new List<Consignacion>();

    public virtual ICollection<Demandum> Demanda { get; set; } = new List<Demandum>();

    public virtual Municipio IdMunicipioNavigation { get; set; } = null!;

    public virtual Rol IdRolNavigation { get; set; } = null!;

    public virtual ICollection<LogSistema> LogSistemas { get; set; } = new List<LogSistema>();

    public virtual ICollection<Notificacion> Notificacions { get; set; } = new List<Notificacion>();

    public virtual ICollection<PagoTransportador> PagoTransportadors { get; set; } = new List<PagoTransportador>();

    public virtual ICollection<PerfilPago> PerfilPagos { get; set; } = new List<PerfilPago>();

    public virtual ICollection<Producto> Productos { get; set; } = new List<Producto>();

    public virtual ICollection<ReporteTransportador> ReporteTransportadors { get; set; } = new List<ReporteTransportador>();

    public virtual ICollection<PuntoAcopio> IdPuntoAcopios { get; set; } = new List<PuntoAcopio>();
}
