using System;
using System.Data;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using CampesinApp.Repositories.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CampesinApp.Services;

/// <summary>
/// Implementación del servicio logístico de cálculo de fletes.
/// Consume Google Routes API para la distancia por carretera y ejecuta sp_CalcularFleteDemanda en SQL Server.
/// </summary>
public class LogisticaFleteService : ILogisticaFleteService
{
    private readonly Campesinapp3Context _context;
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;
    private readonly ILogger<LogisticaFleteService> _logger;

    public LogisticaFleteService(
        Campesinapp3Context context,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        ILogger<LogisticaFleteService> logger)
    {
        _context = context;
        _configuration = configuration;
        _httpClient = httpClientFactory.CreateClient();
        _logger = logger;
    }

    public async Task<double> CalcularFleteAsync(double kilos, int idPuntoAcopio)
    {
        // 1. Obtener coordenadas del Punto de Acopio de destino
        var puntoAcopio = await _context.PuntoAcopios
            .FirstOrDefaultAsync(p => p.IdPuntoAcopio == idPuntoAcopio);

        double latDestino = puntoAcopio != null ? (double)puntoAcopio.Latitud : 4.7110;
        double lngDestino = puntoAcopio != null ? (double)puntoAcopio.Longitud : -74.0721;

        // 2. Coordenadas de origen: Región del Tequendama desde appsettings.json
        double latOrigen = _configuration.GetValue<double>("Logistica:CoordenadasTequendama:Latitud", 4.6315);
        double lngOrigen = _configuration.GetValue<double>("Logistica:CoordenadasTequendama:Longitud", -74.4632);

        // 3. Consultar distancia por carretera con Google Routes API
        double distanciaKm = await ObtenerDistanciaGoogleRoutesAsync(latOrigen, lngOrigen, latDestino, lngDestino);

        // 4. Parámetros logísticos desde appsettings.json
        double precioGalon = _configuration.GetValue<double>("Logistica:PrecioGalonGasolina", 15600.0);
        double peajes = _configuration.GetValue<double>("Logistica:PeajesPromedio", 35000.0);
        double desgaste = _configuration.GetValue<double>("Logistica:DesgastePorKm", 300.0);
        double margen = _configuration.GetValue<double>("Logistica:MargenUtilidadTransportador", 0.15);

        // 5. Ejecutar sp_CalcularFleteDemanda en SQL Server
        return await EjecutarSpCalcularFleteAsync(kilos, distanciaKm, precioGalon, peajes, desgaste, margen);
    }

    private async Task<double> ObtenerDistanciaGoogleRoutesAsync(double latOrigen, double lngOrigen, double latDestino, double lngDestino)
    {
        string apiKey = _configuration["GoogleMaps:ApiKey"] ?? string.Empty;
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning("No se encontró GoogleMaps:ApiKey en appsettings.json. Usando distancia por defecto 75.0 km.");
            return 75.0;
        }

        try
        {
            var requestBody = new
            {
                origin = new
                {
                    location = new
                    {
                        latLng = new { latitude = latOrigen, longitude = lngOrigen }
                    }
                },
                destination = new
                {
                    location = new
                    {
                        latLng = new { latitude = latDestino, longitude = lngDestino }
                    }
                },
                travelMode = "DRIVE",
                routingPreference = "TRAFFIC_UNAWARE"
            };

            var request = new HttpRequestMessage(HttpMethod.Post, "https://routes.googleapis.com/directions/v2:computeRoutes")
            {
                Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json")
            };

            request.Headers.Add("X-Goog-Api-Key", apiKey);
            request.Headers.Add("X-Goog-FieldMask", "routes.distanceMeters");

            var response = await _httpClient.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                string json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("routes", out var routes) && routes.GetArrayLength() > 0)
                {
                    var firstRoute = routes[0];
                    if (firstRoute.TryGetProperty("distanceMeters", out var distElement))
                    {
                        double metros = distElement.GetDouble();
                        double km = Math.Round(metros / 1000.0, 2);
                        if (km > 0) return km;
                    }
                }
            }
            else
            {
                string errorBody = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Google Routes API retornó código {StatusCode}: {ErrorBody}", response.StatusCode, errorBody);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error comunicándose con Google Routes API.");
        }

        return 75.0; // Distancia estimada de contingencia
    }

    private async Task<double> EjecutarSpCalcularFleteAsync(double kilos, double distanciaKm, double precioGalon, double peajes, double desgaste, double margen)
    {
        var conn = _context.Database.GetDbConnection();
        bool cerrarConexion = false;
        if (conn.State != ConnectionState.Open)
        {
            await conn.OpenAsync();
            cerrarConexion = true;
        }

        try
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "EXEC [dbo].[sp_CalcularFleteDemanda] @Kilos, @DistanciaKm, @PrecioGalon, @PeajesPromedio, @DesgastePorKm, @MargenUtilidad";

            cmd.Parameters.Add(CrearParametro(cmd, "@Kilos", kilos));
            cmd.Parameters.Add(CrearParametro(cmd, "@DistanciaKm", distanciaKm));
            cmd.Parameters.Add(CrearParametro(cmd, "@PrecioGalon", precioGalon));
            cmd.Parameters.Add(CrearParametro(cmd, "@PeajesPromedio", peajes));
            cmd.Parameters.Add(CrearParametro(cmd, "@DesgastePorKm", desgaste));
            cmd.Parameters.Add(CrearParametro(cmd, "@MargenUtilidad", margen));

            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return Convert.ToDouble(reader["FleteTotal"]);
            }
        }
        finally
        {
            if (cerrarConexion && conn.State == ConnectionState.Open)
            {
                await conn.CloseAsync();
            }
        }

        return 150000.0; // Flete de respaldo si el SP no arroja filas
    }

    private static IDbDataParameter CrearParametro(IDbCommand cmd, string nombre, object valor)
    {
        var p = cmd.CreateParameter();
        p.ParameterName = nombre;
        p.Value = valor;
        return p;
    }
}

