using CampesinApp.Repositories.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CampesinApp.Services
{
    /// <summary>
    /// Servicio nativo en segundo plano (.NET BackgroundService) para la automatización diaria de precios.
    /// Reemplaza a Hangfire: no necesita scripts .sql, no crea tablas adicionales en la base de datos
    /// y utiliza la misma lógica del botón manual del Administrador.
    /// 
    /// Horario programado: Todos los días a las 8:30 AM (hora Colombia).
    /// </summary>
    public class EstimadosDiariosBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<EstimadosDiariosBackgroundService> _logger;
        private readonly TimeZoneInfo _zonaColombia;

        public EstimadosDiariosBackgroundService(
            IServiceScopeFactory scopeFactory,
            ILogger<EstimadosDiariosBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _zonaColombia = ObtenerZonaHorariaColombia();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Servicio nativo de Estimados Diarios iniciado correctamente.");

            // 1. Al encender la aplicación: verificar si hay ejecución pendiente para hoy
            await VerificarEjecucionPendienteAlArrancarAsync(stoppingToken);

            // 2. Bucle principal: esperar hasta cada día a las 8:30 AM y ejecutar
            while (!stoppingToken.IsCancellationRequested)
            {
                var tiempoEspera = CalcularTiempoHastaProximaEjecucion(_zonaColombia);

                _logger.LogInformation(
                    "Próximo cálculo automático de estimados programado para dentro de {Horas}h {Minutos}m (a las 8:30 AM hora Colombia).",
                    (int)tiempoEspera.TotalHours,
                    tiempoEspera.Minutes);

                try
                {
                    // Esperar de forma asíncrona y eficiente sin consumir CPU
                    await Task.Delay(tiempoEspera, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    // La aplicación se está apagando normalmente
                    break;
                }

                if (stoppingToken.IsCancellationRequested)
                    break;

                // 3. Ejecutar el procedimiento almacenado
                await EjecutarCalculoDiarioAsync();
            }

            _logger.LogInformation("Servicio nativo de Estimados Diarios detenido.");
        }

        /// <summary>
        /// Si la máquina estuvo apagada a las 8:30 AM y hoy no se han calculado los precios,
        /// los calcula de inmediato al encender el aplicativo.
        /// </summary>
        private async Task VerificarEjecucionPendienteAlArrancarAsync(CancellationToken stoppingToken)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var job = scope.ServiceProvider.GetRequiredService<IEstimadosDiariosJob>();
                await job.VerificarYEjecutarPendienteAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al verificar la ejecución pendiente de estimados diarios al arrancar.");
            }
        }

        /// <summary>
        /// Ejecuta el procedimiento almacenado sp_CalcularEstimadosDiarios creando un ámbito (scope) nuevo,
        /// exactamente igual a como lo hace el botón manual del Administrador.
        /// </summary>
        private async Task EjecutarCalculoDiarioAsync()
        {
            try
            {
                _logger.LogInformation("Son las 8:30 AM. Iniciando ejecución automática de sp_CalcularEstimadosDiarios...");

                using var scope = _scopeFactory.CreateScope();
                var job = scope.ServiceProvider.GetRequiredService<IEstimadosDiariosJob>();
                await job.EjecutarSpAsync();

                _logger.LogInformation("Ejecución automática de sp_CalcularEstimadosDiarios completada con éxito.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error durante la ejecución programada de sp_CalcularEstimadosDiarios a las 8:30 AM.");
                // Esperar 1 minuto antes de reintentar el bucle para evitar saturación en caso de fallo
                await Task.Delay(TimeSpan.FromMinutes(1));
            }
        }

        /// <summary>
        /// Calcula con precisión el tiempo restante hasta las 8:30 AM de hoy o de mañana.
        /// </summary>
        private static TimeSpan CalcularTiempoHastaProximaEjecucion(TimeZoneInfo zona)
        {
            var ahoraUtc = DateTimeOffset.UtcNow;
            var ahoraCol = TimeZoneInfo.ConvertTime(ahoraUtc, zona);

            // Hora objetivo: 8:30 AM de hoy
            var hoy830Am = new DateTimeOffset(
                ahoraCol.Year, ahoraCol.Month, ahoraCol.Day,
                8, 30, 0, zona.GetUtcOffset(ahoraCol));

            // Si hoy ya pasaron las 8:30 AM, programar para mañana a las 8:30 AM
            var proximaEjecucion = ahoraCol < hoy830Am
                ? hoy830Am
                : hoy830Am.AddDays(1);

            var diferencia = proximaEjecucion - ahoraCol;
            return diferencia > TimeSpan.Zero ? diferencia : TimeSpan.FromSeconds(10);
        }

        /// <summary>
        /// Obtiene la zona horaria de Colombia compatible tanto con Windows como con Linux/macOS.
        /// </summary>
        private static TimeZoneInfo ObtenerZonaHorariaColombia()
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("SA Pacific Standard Time"); // Windows
            }
            catch (TimeZoneNotFoundException)
            {
                try
                {
                    return TimeZoneInfo.FindSystemTimeZoneById("America/Bogota"); // Linux / Mac
                }
                catch (TimeZoneNotFoundException)
                {
                    return TimeZoneInfo.Local;
                }
            }
        }
    }
}

