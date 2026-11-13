using CampesinApp.Repositories.Models;
using Microsoft.EntityFrameworkCore;

namespace CampesinApp.Tests.Falsos;

/// <summary>
/// Crea un DbContext aislado en memoria RAM para cada prueba, sin tocar SQL Server.
/// </summary>
public static class DbContextEnMemoriaHelper
{
    public static Campesinapp3Context CrearContexto()
    {
        var opciones = new DbContextOptionsBuilder<Campesinapp3Context>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new Campesinapp3ContextEnMemoria(opciones);
    }

    private sealed class Campesinapp3ContextEnMemoria : Campesinapp3Context
    {
        public Campesinapp3ContextEnMemoria(DbContextOptions<Campesinapp3Context> options)
            : base(options)
        {
        }

        // Evita que OnConfiguring vuelva a registrar UseSqlServer sobre InMemory.
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
        }
    }
}
