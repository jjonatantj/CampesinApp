using CampesinApp.Dtos;
using CampesinApp.Repositories.Models;
using CampesinApp.Services;
using CampesinApp.Tests.Falsos;

namespace CampesinApp.Tests;

public class PedidoServiceTests
{
    [Fact]
    public async Task ParticipaAsync_CompradorPropietario_RetornaTrue()
    {
        // Arrange
        using var db = DbContextEnMemoriaHelper.CrearContexto();
        db.Demanda.Add(new Demandum { IdDemanda = 10, IdUsuario = 5, DescripcionDeman = "Yuca" });
        db.Demanda.Add(new Demandum { IdDemanda = 11, IdUsuario = 99, DescripcionDeman = "Plátano" });
        await db.SaveChangesAsync();

        var servicio = new PedidoService(db);
        var comprador = new UsuarioActual(5, "Ana", "3001234567", RolUsuario.Comprador);

        // Act
        var participaEnPropio = await servicio.ParticipaAsync(comprador, 10);
        var participaEnAjeno = await servicio.ParticipaAsync(comprador, 11);

        // Assert
        Assert.True(participaEnPropio);
        Assert.False(participaEnAjeno);
    }

    [Fact]
    public async Task ParticipaAsync_CampesinoQueAporto_RetornaTrue()
    {
        // Arrange
        using var db = DbContextEnMemoriaHelper.CrearContexto();
        var demanda = new Demandum { IdDemanda = 20, IdUsuario = 1, DescripcionDeman = "Café" };
        demanda.AporteCamps.Add(new AporteCamp { IdAporte = 1, IdDemanda = 20, IdUsuario = 7, CantAporte = 50, Imagen = "foto.jpg" });
        db.Demanda.Add(demanda);
        await db.SaveChangesAsync();

        var servicio = new PedidoService(db);
        var campesino = new UsuarioActual(7, "Carlos", "3100000000", RolUsuario.Campesino);
        var otroCampesino = new UsuarioActual(8, "Luis", "3110000000", RolUsuario.Campesino);

        // Act
        var participa = await servicio.ParticipaAsync(campesino, 20);
        var noParticipa = await servicio.ParticipaAsync(otroCampesino, 20);

        // Assert
        Assert.True(participa);
        Assert.False(noParticipa);
    }

    [Fact]
    public async Task ParticipaAsync_Administrador_VeTodosLosPedidos()
    {
        // Arrange
        using var db = DbContextEnMemoriaHelper.CrearContexto();
        db.Demanda.Add(new Demandum { IdDemanda = 30, IdUsuario = 99, DescripcionDeman = "Arroz" });
        await db.SaveChangesAsync();

        var servicio = new PedidoService(db);
        var admin = new UsuarioActual(1, "Admin", "3000000000", RolUsuario.Administrador);

        // Act
        var participa = await servicio.ParticipaAsync(admin, 30);

        // Assert
        Assert.True(participa);
    }
}

