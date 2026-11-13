using CampesinApp.Services;

namespace CampesinApp.Tests;

public class EncriptacionServiceTests
{
    [Fact]
    public void Encriptar_ConTextoValido_GeneraHashDistintoAlOriginal()
    {
        // Arrange
        var claveOriginal = "ClaveSegura123!";

        // Act
        var hash = EncriptacionService.Encriptar(claveOriginal);

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(hash));
        Assert.NotEqual(claveOriginal, hash);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Encriptar_ConTextoVacioONulo_RetornaCadenaVacia(string? texto)
    {
        // Arrange & Act
        var resultado = EncriptacionService.Encriptar(texto!);

        // Assert
        Assert.Equal(string.Empty, resultado);
    }

    [Fact]
    public void Verificar_ConContrasenaCorrecta_RetornaTrue()
    {
        // Arrange
        var clave = "MiPassword2026*";
        var hash = EncriptacionService.Encriptar(clave);

        // Act
        var coincide = EncriptacionService.Verificar(hash, clave);

        // Assert
        Assert.True(coincide);
    }

    [Fact]
    public void Verificar_ConContrasenaIncorrecta_RetornaFalse()
    {
        // Arrange
        var hash = EncriptacionService.Encriptar("ClaveCorrecta1!");

        // Act
        var coincide = EncriptacionService.Verificar(hash, "ClaveEquivocada");

        // Assert
        Assert.False(coincide);
    }

    [Theory]
    [InlineData("", "algo")]
    [InlineData("hash", "")]
    [InlineData(null, "algo")]
    public void Verificar_ConEntradasVaciasONulas_RetornaFalse(string? hash, string? clave)
    {
        // Arrange & Act
        var coincide = EncriptacionService.Verificar(hash!, clave!);

        // Assert
        Assert.False(coincide);
    }
}

