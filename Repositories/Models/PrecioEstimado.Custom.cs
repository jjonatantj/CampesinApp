using System.ComponentModel.DataAnnotations.Schema;

namespace CampesinApp.Repositories.Models;

/// <summary>
/// Extensión parcial de PrecioEstimado.
/// Al estar en un archivo separado, EF Core no lo sobreescribe cuando se ejecuta 'dotnet ef dbcontext scaffold'.
/// </summary>
public partial class PrecioEstimado
{
    [NotMapped]
    public int PrecioEsti
    {
        get => (int)Math.Round(PrecioEstiCampesino);
        set => PrecioEstiCampesino = value;
    }
}

