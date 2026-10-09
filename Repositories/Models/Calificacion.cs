using System;
using System.Collections.Generic;

namespace CampesinApp.Repositories.Models;

/// <summary>
/// Calificación (1 a 5 estrellas) y comentario que un usuario deja sobre otro participante de un pedido (CU-026).
/// </summary>
public partial class Calificacion
{
    public int IdCalificacion { get; set; }

    public int IdDemanda { get; set; }

    public int IdUsuarioEvalua { get; set; }

    public int IdUsuarioEvaluado { get; set; }

    public int Estrellas { get; set; }

    public string Comentario { get; set; } = null!;

    public DateTime Fecha { get; set; }

    public virtual Demandum IdDemandaNavigation { get; set; } = null!;

    public virtual Usuario IdUsuarioEvaluaNavigation { get; set; } = null!;

    public virtual Usuario IdUsuarioEvaluadoNavigation { get; set; } = null!;
}
