using System.Text;
using CampesinApp.Controllers;
using CampesinApp.Repositories.Models;
using CampesinApp.Services;
using CampesinApp.Tests.Falsos;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Options;

namespace CampesinApp.Tests;

public class AsignacionVehiculoControllerTests
{
    private static AsignacionVehiculoController CrearControlador(
        RepositorioAsignacionVehiculoFalso repo,
        string? rolSesion = "Administrador")
    {
        var db = DbContextEnMemoriaHelper.CrearContexto();
        var log = new LogServiceFalso();
        var pedidos = new PedidoService(db);
        var notificaciones = new NotificacionService(db);
        var calificaciones = new CalificacionService(db, pedidos, log, Options.Create(new SoporteOptions()), notificaciones);
        var transporte = new TransporteService(db, pedidos, calificaciones, log, notificaciones);

        var httpContext = new DefaultHttpContext();
        var sesion = new SesionFalsa();
        if (!string.IsNullOrEmpty(rolSesion))
        {
            sesion.Set("RolUsuario", Encoding.UTF8.GetBytes(rolSesion));
        }
        httpContext.Session = sesion;

        var tempData = new TempDataDictionary(httpContext, new TempDataProviderFalso());

        return new AsignacionVehiculoController(repo, transporte)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext },
            TempData = tempData
        };
    }

    [Fact]
    public async Task Asignar_SinRolAdministrador_RedirigeALogin()
    {
        // Arrange
        var repo = new RepositorioAsignacionVehiculoFalso();
        var controller = CrearControlador(repo, rolSesion: "Campesino");

        // Act
        var resultado = await controller.Asignar(idDemanda: 1, idCamion: 1);

        // Assert
        var redirect = Assert.IsType<RedirectToActionResult>(resultado);
        Assert.Equal("Login", redirect.ActionName);
        Assert.Equal("InicioSesion", redirect.ControllerName);
        Assert.Empty(repo.AsignacionesGuardadas);
    }

    [Fact]
    public async Task Asignar_ConCapacidadInsuficiente_RechazaYNoGuarda()
    {
        // Arrange
        var repo = new RepositorioAsignacionVehiculoFalso();
        repo.Demandas.Add(new Demandum { IdDemanda = 1, CantidadDeman = 1000, DescripcionDeman = "Papa" });
        repo.Camiones.Add(new Camion
        {
            IdCamion = 10,
            CapacidadCami = 500, // menor a 1000 kg
            PlacaCami = "ABC123",
            ModeloCami = "2022",
            EstadoPase = true,
            EstadoSeguro = true,
            EstadoTecno = true
        });
        var controller = CrearControlador(repo);

        // Act
        var resultado = await controller.Asignar(idDemanda: 1, idCamion: 10);

        // Assert
        var redirect = Assert.IsType<RedirectToActionResult>(resultado);
        Assert.Equal("Detalle", redirect.ActionName);
        Assert.Empty(repo.AsignacionesGuardadas);
        Assert.Contains("espacio suficiente", controller.TempData["MensajeError"]?.ToString());
    }

    [Fact]
    public async Task Asignar_ConDocumentosVencidos_RechazaYNoGuarda()
    {
        // Arrange
        var repo = new RepositorioAsignacionVehiculoFalso();
        repo.Demandas.Add(new Demandum { IdDemanda = 1, CantidadDeman = 500, DescripcionDeman = "Maíz" });
        repo.Camiones.Add(new Camion
        {
            IdCamion = 10,
            CapacidadCami = 1500,
            PlacaCami = "XYZ789",
            ModeloCami = "2023",
            EstadoPase = true,
            EstadoSeguro = false, // documento vencido
            EstadoTecno = true
        });
        var controller = CrearControlador(repo);

        // Act
        var resultado = await controller.Asignar(idDemanda: 1, idCamion: 10);

        // Assert
        var redirect = Assert.IsType<RedirectToActionResult>(resultado);
        Assert.Equal("Detalle", redirect.ActionName);
        Assert.Empty(repo.AsignacionesGuardadas);
        Assert.Contains("documentos vigentes", controller.TempData["MensajeError"]?.ToString());
    }

    [Fact]
    public async Task Asignar_ConDatosValidos_GuardaAsignacionYRedirigeAIndex()
    {
        // Arrange
        var repo = new RepositorioAsignacionVehiculoFalso();
        repo.Demandas.Add(new Demandum { IdDemanda = 1, CantidadDeman = 500, DescripcionDeman = "Tomate" });
        repo.Camiones.Add(new Camion
        {
            IdCamion = 10,
            CapacidadCami = 1200,
            PlacaCami = "KLM456",
            ModeloCami = "2024",
            EstadoPase = true,
            EstadoSeguro = true,
            EstadoTecno = true
        });
        var controller = CrearControlador(repo);

        // Act
        var resultado = await controller.Asignar(idDemanda: 1, idCamion: 10);

        // Assert
        var redirect = Assert.IsType<RedirectToActionResult>(resultado);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Single(repo.AsignacionesGuardadas);
        Assert.Equal((1, 10), repo.AsignacionesGuardadas[0]);
        Assert.Equal("Asignación completa", controller.TempData["MensajeExito"]);
    }

    private sealed class TempDataProviderFalso : ITempDataProvider
    {
        public IDictionary<string, object?> LoadTempData(HttpContext context) => new Dictionary<string, object?>();
        public void SaveTempData(HttpContext context, IDictionary<string, object?> values) { }
    }
}

