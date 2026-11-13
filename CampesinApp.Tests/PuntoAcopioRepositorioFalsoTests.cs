using CampesinApp.Dtos;
using CampesinApp.Repositories.Models;
using CampesinApp.Tests.Falsos;

namespace CampesinApp.Tests;

public class PuntoAcopioRepositorioFalsoTests
{
    [Fact]
    public async Task ExistePuntoDuplicadoAsync_ConMismaDireccion_RetornaTrue()
    {
        // Arrange
        var repo = new RepositorioPuntoAcopioFalso();
        repo.Puntos.Add(new PuntoAcopio
        {
            IdPuntoAcopio = 1,
            NombrePunto = "Acopio Norte",
            Direccion = "Calle 10 # 5-20",
            Latitud = 4.61m,
            Longitud = -74.08m
        });

        // Act
        var duplicado = await repo.ExistePuntoDuplicadoAsync("calle 10 # 5-20", 4.99m, -74.99m);

        // Assert
        Assert.True(duplicado);
    }

    [Fact]
    public async Task ExistePuntoDuplicadoAsync_ConMismasCoordenadas_RetornaTrue()
    {
        // Arrange
        var repo = new RepositorioPuntoAcopioFalso();
        repo.Puntos.Add(new PuntoAcopio
        {
            IdPuntoAcopio = 1,
            NombrePunto = "Acopio Sur",
            Direccion = "Carrera 1 # 2-3",
            Latitud = 4.6500m,
            Longitud = -74.1000m
        });

        // Act
        var duplicado = await repo.ExistePuntoDuplicadoAsync("Otra Dirección", 4.6500m, -74.1000m);

        // Assert
        Assert.True(duplicado);
    }

    [Fact]
    public async Task GuardarPuntoAcopioAsync_ConDatosNuevos_AsignaIdYGuardaEnMemoria()
    {
        // Arrange
        var repo = new RepositorioPuntoAcopioFalso();
        var nuevo = new PuntoAcopioRegistroDto
        {
            NombrePunto = "Centro Veredal",
            Direccion = "Vereda El Rosal Km 3",
            Latitud = 5.01m,
            Longitud = -75.02m,
            IdMunicipio = 1,
            TipoAcopio = false
        };

        // Act
        var idGenerado = await repo.GuardarPuntoAcopioAsync(nuevo);

        // Assert
        Assert.Equal(1, idGenerado);
        Assert.Single(repo.Guardados);
        Assert.Single(repo.Puntos);
    }
}
