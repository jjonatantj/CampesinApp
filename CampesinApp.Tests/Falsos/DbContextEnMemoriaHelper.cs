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

        var context = new Campesinapp3ContextEnMemoria(opciones);

        // Semilla base para que los .Include() y .ThenInclude() de EF Core InMemory resuelvan las claves foráneas
        var depto = new Departamento { IdDepartamento = 1, Departamento1 = "Cundinamarca" };
        var muni = new Municipio { IdMunicipio = 1, Municipio1 = "Bogotá", IdDepartamento = 1, IdDepartamentoNavigation = depto };
        var rol = new Rol { IdRol = 1, TipoRol = "Comprador" };
        var user = new Usuario
        {
            IdUsuario = 10,
            IdRol = 1,
            IdMunicipio = 1,
            CorreoUsu = "test@user.com",
            ContrasenaUsu = "123",
            NombreCom = "Test",
            ApellidoCom = "User",
            Imagen = "default.png",
            Estado = true,
            IdMunicipioNavigation = muni,
            IdRolNavigation = rol
        };
        var tp = new TipoProducto { IdTipoProducto = 1, Producto = "Tomate" };
        var variedad = new Variedad { IdVariedad = 1, VariedadProd = "Chonto", IdTipoProducto = 1, IdTipoProductoNavigation = tp };

        context.Departamentos.Add(depto);
        context.Municipios.Add(muni);
        context.Rols.Add(rol);
        context.Usuarios.Add(user);
        context.TipoProductos.Add(tp);
        context.Variedads.Add(variedad);
        context.SaveChanges();

        return context;
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
