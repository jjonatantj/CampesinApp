using CampesinApp.Repositories;
using CampesinApp.Repositories.Models;
using CampesinApp.Tests.Falsos;
using Xunit;

namespace CampesinApp.Tests;

public class DemandaRepositoryTests
{
    [Fact]
    public async Task ObtenerDemandasFiltradasAsync_ExcluyeDemandaConFechaLimiteAyer()
    {
        // Arrange
        using var db = DbContextEnMemoriaHelper.CrearContexto();
        var demandaVencida = new Demandum
        {
            IdDemanda = 1,
            IdUsuario = 10,
            IdTipoProducto = 1,
            IdVariedad = 1,
            IdEstadoDeman = 1,
            CantidadDeman = 500,
            LimTiemDeman = DateTime.Today.AddDays(-1), // Venció ayer
            DescripcionDeman = "Papa pastusa vencida"
        };
        db.Demanda.Add(demandaVencida);
        await db.SaveChangesAsync();

        var repo = new DemandaRepository(db);

        // Act
        var resultado = await repo.ObtenerDemandasFiltradasAsync(null, null, null, null);

        // Assert
        Assert.DoesNotContain(resultado, d => d.IdDemanda == 1);
    }

    [Fact]
    public async Task ObtenerDemandasFiltradasAsync_IncluyeDemandaConFechaLimiteHoy()
    {
        // Arrange: Hoy es el último día para ofertar, por lo que debe ser visible
        using var db = DbContextEnMemoriaHelper.CrearContexto();
        var demandaHoy = new Demandum
        {
            IdDemanda = 2,
            IdUsuario = 10,
            IdTipoProducto = 1,
            IdVariedad = 1,
            IdEstadoDeman = 1,
            CantidadDeman = 500,
            LimTiemDeman = DateTime.Today, // Vence hoy (último día)
            DescripcionDeman = "Tomate chonto activo hoy"
        };
        db.Demanda.Add(demandaHoy);
        await db.SaveChangesAsync();

        var repo = new DemandaRepository(db);

        // Act
        var resultado = await repo.ObtenerDemandasFiltradasAsync(null, null, null, null);

        // Assert
        Assert.Contains(resultado, d => d.IdDemanda == 2);
    }

    [Fact]
    public async Task ObtenerDemandasFiltradasAsync_IncluyeDemandaConFechaLimiteFutura()
    {
        // Arrange
        using var db = DbContextEnMemoriaHelper.CrearContexto();
        var demandaFutura = new Demandum
        {
            IdDemanda = 3,
            IdUsuario = 10,
            IdTipoProducto = 1,
            IdVariedad = 1,
            IdEstadoDeman = 1,
            CantidadDeman = 500,
            LimTiemDeman = DateTime.Today.AddDays(5),
            DescripcionDeman = "Cebolla junca activa"
        };
        db.Demanda.Add(demandaFutura);
        await db.SaveChangesAsync();

        var repo = new DemandaRepository(db);

        // Act
        var resultado = await repo.ObtenerDemandasFiltradasAsync(null, null, null, null);

        // Assert
        Assert.Contains(resultado, d => d.IdDemanda == 3);
    }

    [Fact]
    public async Task ObtenerDemandasFiltradasAsync_ExcluyeDemandaConCupoLleno()
    {
        // Arrange: Se pidieron 100 kg y ya se aportaron 100 kg
        using var db = DbContextEnMemoriaHelper.CrearContexto();
        var demandaLlena = new Demandum
        {
            IdDemanda = 4,
            IdUsuario = 10,
            IdTipoProducto = 1,
            IdVariedad = 1,
            IdEstadoDeman = 1,
            CantidadDeman = 100,
            LimTiemDeman = DateTime.Today.AddDays(2),
            DescripcionDeman = "Zanahoria con cupo lleno"
        };
        demandaLlena.AporteCamps.Add(new AporteCamp
        {
            IdAporte = 1,
            IdDemanda = 4,
            IdUsuario = 20,
            CantAporte = 100,
            Imagen = "foto.jpg"
        });
        db.Demanda.Add(demandaLlena);
        await db.SaveChangesAsync();

        var repo = new DemandaRepository(db);

        // Act
        var resultado = await repo.ObtenerDemandasFiltradasAsync(null, null, null, null);

        // Assert
        Assert.DoesNotContain(resultado, d => d.IdDemanda == 4);
    }

    [Fact]
    public async Task ObtenerDemandasFiltradasAsync_IncluyeDemandaConCupoParcial()
    {
        // Arrange: Se pidieron 100 kg y se han aportado 40 kg (faltan 60 kg)
        using var db = DbContextEnMemoriaHelper.CrearContexto();
        var demandaParcial = new Demandum
        {
            IdDemanda = 5,
            IdUsuario = 10,
            IdTipoProducto = 1,
            IdVariedad = 1,
            IdEstadoDeman = 1,
            CantidadDeman = 100,
            LimTiemDeman = DateTime.Today.AddDays(2),
            DescripcionDeman = "Aguacate hass con cupo restante"
        };
        demandaParcial.AporteCamps.Add(new AporteCamp
        {
            IdAporte = 2,
            IdDemanda = 5,
            IdUsuario = 20,
            CantAporte = 40,
            Imagen = "foto.jpg"
        });
        db.Demanda.Add(demandaParcial);
        await db.SaveChangesAsync();

        var repo = new DemandaRepository(db);

        // Act
        var resultado = await repo.ObtenerDemandasFiltradasAsync(null, null, null, null);

        // Assert
        Assert.Contains(resultado, d => d.IdDemanda == 5);
    }

    [Fact]
    public async Task ObtenerDemandasFiltradasAsync_ExcluyeDemandaFinalizadaOEnTransito()
    {
        // Arrange: Estado 3 (en tránsito / cupo completo) y Estado 4 (finalizada)
        using var db = DbContextEnMemoriaHelper.CrearContexto();
        var demandaTransito = new Demandum
        {
            IdDemanda = 6,
            IdUsuario = 10,
            IdTipoProducto = 1,
            IdVariedad = 1,
            IdEstadoDeman = 3, // En tránsito
            CantidadDeman = 500,
            LimTiemDeman = DateTime.Today.AddDays(5),
            DescripcionDeman = "Fresa en tránsito"
        };
        var demandaFinalizada = new Demandum
        {
            IdDemanda = 7,
            IdUsuario = 10,
            IdTipoProducto = 1,
            IdVariedad = 1,
            IdEstadoDeman = 4, // Finalizada
            CantidadDeman = 500,
            LimTiemDeman = DateTime.Today.AddDays(5),
            DescripcionDeman = "Mora finalizada"
        };
        db.Demanda.AddRange(demandaTransito, demandaFinalizada);
        await db.SaveChangesAsync();

        var repo = new DemandaRepository(db);

        // Act
        var resultado = await repo.ObtenerDemandasFiltradasAsync(null, null, null, null);

        // Assert
        Assert.DoesNotContain(resultado, d => d.IdDemanda == 6);
        Assert.DoesNotContain(resultado, d => d.IdDemanda == 7);
    }
}

