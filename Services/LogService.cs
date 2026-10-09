using CampesinApp.Repositories.Models;
using Microsoft.EntityFrameworkCore;

namespace CampesinApp.Services
{
    public interface ILogService
    {
        Task RegistrarAsync(string nivel, string origen, string mensaje, int? idUsuario = null);
    }

    /// <summary>
    /// Guarda eventos en la tabla log_Sistema (se consultan en CU-030). Usa su propio DbContext para que un
    /// error de guardado en la operación original no afecte al registro, y nunca lanza excepciones.
    /// </summary>
    public class LogService : ILogService
    {
        private readonly IServiceScopeFactory _ambitos;
        private readonly ILogger<LogService> _logger;

        public LogService(IServiceScopeFactory ambitos, ILogger<LogService> logger)
        {
            _ambitos = ambitos;
            _logger = logger;
        }

        public async Task RegistrarAsync(string nivel, string origen, string mensaje, int? idUsuario = null)
        {
            var nivelLog = nivel == NivelesLog.Error ? LogLevel.Error : nivel == NivelesLog.Advertencia ? LogLevel.Warning : LogLevel.Information;
            _logger.Log(nivelLog, "[{Origen}] {Mensaje}", origen, mensaje);

            try
            {
                // Un ámbito nuevo da un contexto independiente del de la petición, con la misma base de datos.
                using var ambito = _ambitos.CreateScope();
                var db = ambito.ServiceProvider.GetRequiredService<Campesinapp3Context>();
                db.LogSistemas.Add(new LogSistema
                {
                    Fecha = DateTime.Now,
                    Nivel = nivel,
                    IdUsuario = idUsuario,
                    Origen = Recortar(origen, 60),
                    Mensaje = Recortar(mensaje, 1000)
                });
                await db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "No se pudo guardar el evento en log_Sistema");
            }
        }

        private static string Recortar(string texto, int max) => texto.Length <= max ? texto : texto[..max];
    }
}
