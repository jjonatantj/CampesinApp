using System.ComponentModel.DataAnnotations;
using CampesinApp.ViewModels;

namespace CampesinApp.Tests;

public class ViewModelsValidacionTests
{
    private static List<ValidationResult> ValidarModelo(object modelo)
    {
        var contexto = new ValidationContext(modelo);
        var resultados = new List<ValidationResult>();
        Validator.TryValidateObject(modelo, contexto, resultados, validateAllProperties: true);
        return resultados;
    }

    [Fact]
    public void LoginViewModel_ConDatosCompletos_EsValido()
    {
        // Arrange
        var modelo = new LoginViewModel
        {
            Cedula = "12345678",
            Contrasena = "Password1!"
        };

        // Act
        var errores = ValidarModelo(modelo);

        // Assert
        Assert.Empty(errores);
    }

    [Fact]
    public void LoginViewModel_SinCedulaNiContrasena_RetornaErroresDeValidacion()
    {
        // Arrange
        var modelo = new LoginViewModel
        {
            Cedula = "",
            Contrasena = ""
        };

        // Act
        var errores = ValidarModelo(modelo);

        // Assert
        Assert.Equal(2, errores.Count);
    }

    [Fact]
    public void RegistroUsuarioViewModel_ConContrasenasDistintas_FallaComparacion()
    {
        // Arrange
        var modelo = new RegistroUsuarioViewModel
        {
            Nombres = "Juan",
            Apellidos = "Pérez",
            Cedula = "10203040",
            Correo = "juan@correo.com",
            Telefono = "3001234567",
            IdRol = 2,
            IdDepartamento = 1,
            IdMunicipio = 1,
            Contrasena = "Clave123!",
            ConfirmarContrasena = "OtraClave123!"
        };

        // Act
        var errores = ValidarModelo(modelo);

        // Assert
        Assert.NotEmpty(errores);
        Assert.Contains(errores, e => e.ErrorMessage!.Contains("no coinciden"));
    }

    [Fact]
    public void RegistroUsuarioViewModel_ConCorreoInvalido_FallaValidacion()
    {
        // Arrange
        var modelo = new RegistroUsuarioViewModel
        {
            Nombres = "María",
            Apellidos = "Gómez",
            Cedula = "10203040",
            Correo = "correo-sin-arroba",
            Telefono = "3001234567",
            IdRol = 2,
            IdDepartamento = 1,
            IdMunicipio = 1,
            Contrasena = "Clave123!",
            ConfirmarContrasena = "Clave123!"
        };

        // Act
        var errores = ValidarModelo(modelo);

        // Assert
        Assert.Contains(errores, e => e.ErrorMessage!.Contains("correo", StringComparison.OrdinalIgnoreCase));
    }
}

