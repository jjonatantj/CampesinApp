using CampesinApp.Repositories.Models;
using CampesinApp.Services;

namespace CampesinApp.Tests;

public class FormatoYHelpersTests
{
    [Fact]
    public void Fecha_ConFechaEspecifica_FormateaDiaMesAnio()
    {
        // Arrange
        var fecha = new DateTime(2026, 5, 9);

        // Act
        var texto = Formato.Fecha(fecha);

        // Assert
        Assert.Equal("09/05/2026", texto);
    }

    [Fact]
    public void Promedio_ConDecimal_FormateaAUnDecimal()
    {
        // Arrange & Act
        var texto = Formato.Promedio(4.56);

        // Assert
        Assert.Contains("4", texto);
        Assert.Contains("6", texto);
    }

    [Fact]
    public void Coordenadas_ConValoresNulos_RetornaNoRegistradas()
    {
        // Arrange & Act
        var texto = VistaHelpers.Coordenadas(null, -74.064);

        // Assert
        Assert.Equal("No registradas", texto);
    }

    [Fact]
    public void Coordenadas_ConValoresValidos_FormateaConPuntoDecimal()
    {
        // Arrange & Act
        var texto = VistaHelpers.Coordenadas(4.628, -74.064);

        // Assert
        Assert.Equal("4.628000, -74.064000", texto);
    }

    [Theory]
    [InlineData("Administrador", RolUsuario.Administrador)]
    [InlineData("admin", RolUsuario.Administrador)]
    [InlineData("Campesino", RolUsuario.Campesino)]
    [InlineData("Comprador", RolUsuario.Comprador)]
    [InlineData("Transportador", RolUsuario.Transportador)]
    [InlineData("OtroRol", RolUsuario.Ninguno)]
    [InlineData(null, RolUsuario.Ninguno)]
    public void RolUsuarioDesde_ConDiferentesTextos_MapeaAlEnumCorrecto(string? textoRol, RolUsuario esperado)
    {
        // Arrange & Act
        var rol = RolUsuarioExtensions.Desde(textoRol);

        // Assert
        Assert.Equal(esperado, rol);
    }
}

