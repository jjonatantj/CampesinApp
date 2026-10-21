using System;
using System.Collections.Generic;

namespace CampesinApp.Repositories.Models;

/// <summary>
/// Evento técnico o de negocio registrado por el sistema; se consulta en el historial global (CU-030).
/// </summary>
public partial class LogSistema
{
    public int IdLog { get; set; }

    public DateTime Fecha { get; set; }

    public string Nivel { get; set; } = null!;

    public int? IdUsuario { get; set; }

    public string Origen { get; set; } = null!;

    public string Mensaje { get; set; } = null!;
}
