using CampesinApp.Repositories.Models;
using Microsoft.EntityFrameworkCore;

namespace CampesinApp.Services
{
    public interface IEstimadosDiariosJob
    {
        Task EjecutarSpAsync();
        Task VerificarYEjecutarPendienteAsync();
    }

    /// <summary>
    /// Job de Hangfire encargado de invocar el procedimiento almacenado sp_CalcularEstimadosDiarios
    /// todos los días a las 8:30 AM y de verificar ejecuciones pendientes al encender el aplicativo.
    /// </summary>
    public class EstimadosDiariosJob : IEstimadosDiariosJob
    {
        private readonly Campesinapp3Context _context;
        private readonly ILogger<EstimadosDiariosJob> _logger;

        public EstimadosDiariosJob(Campesinapp3Context context, ILogger<EstimadosDiariosJob> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Ejecuta directamente el procedimiento almacenado sp_CalcularEstimadosDiarios en SQL Server.
        /// </summary>
        public async Task EjecutarSpAsync()
        {
            try
            {
                _logger.LogInformation("Iniciando cálculo de precios estimados diarios (sp_CalcularEstimadosDiarios) a las {Hora}...", DateTime.Now);

                // Ejecuta el procedimiento almacenado
                var filasAfectadas = await _context.Database.ExecuteSqlRawAsync("EXEC sp_CalcularEstimadosDiarios");

                _logger.LogInformation("sp_CalcularEstimadosDiarios ejecutado exitosamente. Filas/Resultado: {Filas}", filasAfectadas);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al ejecutar el procedimiento almacenado sp_CalcularEstimadosDiarios.");
                throw; // Se relanza para que Hangfire registre el fallo en el Dashboard y aplique reintentos automáticos
            }
        }

        /// <summary>
        /// Se invoca al arrancar la aplicación. Si el equipo estuvo apagado a las 8:30 AM y
        /// hoy aún no se han calculado los precios estimados, ejecuta el cálculo de inmediato.
        /// </summary>
        public async Task VerificarYEjecutarPendienteAsync()
        {
            try
            {
                var hoy = DateOnly.FromDateTime(DateTime.Today);
                var horaActual = DateTime.Now.TimeOfDay;
                var horaObjetivo = new TimeSpan(8, 30, 0);

                _logger.LogInformation("Verificando si sp_CalcularEstimadosDiarios requiere ejecución pendiente para hoy ({Hoy})...", hoy);

                bool yaCalculadoHoy = await _context.PrecioEstimados.AnyAsync(p => p.FechaPreci == hoy);

                if (!yaCalculadoHoy && horaActual >= horaObjetivo)
                {
                    _logger.LogWarning("No se encontraron registros de precios calculados para hoy ({Hoy}) y la hora actual ({Hora}) supera las 8:30 AM. Ejecutando ahora...", hoy, horaActual);
                    await EjecutarSpAsync();
                }
                else if (yaCalculadoHoy)
                {
                    _logger.LogInformation("Los precios estimados para el día de hoy ({Hoy}) ya se encuentran calculados.", hoy);
                }
                else
                {
                    _logger.LogInformation("Aún no son las 8:30 AM (hora actual: {Hora}). La tarea se ejecutará en su horario programado.", horaActual);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error durante la verificación de ejecución pendiente de estimados diarios.");
            }
        }
    }
}

