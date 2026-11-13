using CampesinApp.Dtos;

namespace CampesinApp.Tests;

public class PaginacionDtoTests
{
    [Theory]
    [InlineData(0, 10, 1)]
    [InlineData(10, 10, 1)]
    [InlineData(11, 10, 2)]
    [InlineData(25, 8, 4)]
    public void TotalPaginas_ConDiferentesTotales_CalculaCorrectamente(int totalRegistros, int tamano, int paginasEsperadas)
    {
        // Arrange
        var paginacion = new PaginacionDto
        {
            TotalRegistros = totalRegistros,
            TamanoPagina = tamano
        };

        // Act
        var totalPaginas = paginacion.TotalPaginas;

        // Assert
        Assert.Equal(paginasEsperadas, totalPaginas);
    }

    [Fact]
    public void Crear_ConPaginaFueraDeRango_AjustaDentroDeLimites()
    {
        // Arrange & Act
        var menor = PaginacionDto.Crear(-3, total: 20, tamano: 5, "pedidos", "/Pedidos?p={p}", "principal");
        var mayor = PaginacionDto.Crear(99, total: 20, tamano: 5, "pedidos", "/Pedidos?p={p}", "principal");

        // Assert
        Assert.Equal(1, menor.Pagina);
        Assert.Equal(4, mayor.Pagina);
    }

    [Fact]
    public void Url_ReemplazaMarcadorConNumeroDePagina()
    {
        // Arrange
        var paginacion = new PaginacionDto { Plantilla = "/Historial?pagina={p}" };

        // Act
        var url = paginacion.Url(3);

        // Assert
        Assert.Equal("/Historial?pagina=3", url);
    }

    [Fact]
    public void Ventana_ConMuchasPaginas_InsertaPuntosSuspensivosComoNull()
    {
        // Arrange
        var paginacion = new PaginacionDto
        {
            Pagina = 5,
            TamanoPagina = 10,
            TotalRegistros = 100 // 10 páginas
        };

        // Act
        var ventana = paginacion.Ventana().ToList();

        // Assert
        Assert.Contains(null, ventana);
        Assert.Equal(1, ventana.First());
        Assert.Equal(10, ventana.Last());
        Assert.Contains(5, ventana);
    }
}

