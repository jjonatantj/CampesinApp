using System;
using System.Collections.Generic;

namespace CampesinApp.Repositories.Models;

public partial class LogSistema
{
    public int IdLog { get; set; }

    public DateTime Fecha { get; set; }

    public string Nivel { get; set; } = null!;

    public int? IdUsuario { get; set; }

    public string Origen { get; set; } = null!;

    public string Mensaje { get; set; } = null!;

    public virtual Usuario? IdUsuarioNavigation { get; set; }
}
