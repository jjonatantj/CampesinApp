using CampesinApp.Dtos;
using CampesinApp.Repositories.Models;
using CampesinApp.Services;
using CampesinApp.Tests.Falsos;
using Microsoft.Extensions.Options;

namespace CampesinApp.Tests;

public class CalificacionServiceTests
{
    private static CalificacionService CrearServicio(Campesinapp3Context db)
    {
        var pedidos = new PedidoService(db);
        var log = new LogServiceFalso();
        var notificaciones = new NotificacionService(db);
        var soporte = Options.Create(new SoporteOptions { Correo = "soporte@campesinapp.test", Telefonos = new[] { "3001234567" } });
        return new CalificacionService(db, pedidos, log, soporte, notificaciones);
    }

    [Theory]
    [InlineData(RolUsuario.Campesino)]
    [InlineData(RolUsuario.Administrador)]
    public async Task ObtenerFormularioAsync_RolNoPermitido_RetornaNoAutorizado(RolUsuario rol)
    {
        // Arrange
        using var db = DbContextEnMemoriaHelper.CrearContexto();
        var servicio = CrearServicio(db);
        var usuario = new UsuarioActual(1, "Usuario", "3000000000", rol);

        // Act
        var resultado = await servicio.ObtenerFormularioAsync(usuario, idDemanda: 1);

        // Assert
        Assert.False(resultado.Exitoso);
        Assert.Equal(TipoError.NoAutorizado, resultado.Error);
    }

    [Fact]
    public async Task ObtenerFormularioAsync_CompradorSinParticipacion_RetornaNoAutorizado()
    {
        // Arrange
        using var db = DbContextEnMemoriaHelper.CrearContexto();
        db.Demanda.Add(new Demandum { IdDemanda = 1, IdUsuario = 99, DescripcionDeman = "Papa" });
        await db.SaveChangesAsync();

        var servicio = CrearServicio(db);
        var otroComprador = new UsuarioActual(2, "Pedro", "3001112233", RolUsuario.Comprador);

        // Act
        var resultado = await servicio.ObtenerFormularioAsync(otroComprador, idDemanda: 1);

        // Assert
        Assert.False(resultado.Exitoso);
        Assert.Equal(TipoError.NoAutorizado, resultado.Error);
    }
}
