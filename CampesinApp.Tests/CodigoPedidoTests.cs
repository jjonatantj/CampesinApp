using CampesinApp.Dtos;

namespace CampesinApp.Tests;

public class CodigoPedidoTests
{
    [Theory]
    [InlineData(1, "PED-0001")]
    [InlineData(12, "PED-0012")]
    [InlineData(1234, "PED-1234")]
    public void Formatear_ConIdValido_RetornaCodigoConPrefijo(int idDemanda, string esperado)
    {
        // Arrange & Act
        var codigo = CodigoPedido.Formatear(idDemanda);

        // Assert
        Assert.Equal(esperado, codigo);
    }

    [Theory]
    [InlineData("PED-0012", 12)]
    [InlineData("ped12", 12)]
    [InlineData("  PED 0007 ", 7)]
    [InlineData("45", 45)]
    public void TryParsear_ConFormatosValidos_RetornaTrueYExtraeId(string texto, int idEsperado)
    {
        // Arrange & Act
        var valido = CodigoPedido.TryParsear(texto, out var idObtenido);

        // Assert
        Assert.True(valido);
        Assert.Equal(idEsperado, idObtenido);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("PED-0")]
    [InlineData("-10")]
    [InlineData("PED-ABC")]
    public void TryParsear_ConFormatosInvalidos_RetornaFalse(string? texto)
    {
        // Arrange & Act
        var valido = CodigoPedido.TryParsear(texto, out var idObtenido);

        // Assert
        Assert.False(valido);
    }
}

