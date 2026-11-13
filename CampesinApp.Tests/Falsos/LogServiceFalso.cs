using CampesinApp.Services;

namespace CampesinApp.Tests.Falsos;

/// <summary>
/// Implementación falsa de ILogService para pruebas unitarias (no escribe en BD).
/// </summary>
public class LogServiceFalso : ILogService
{
    public List<(string Nivel, string Origen, string Mensaje, int? IdUsuario)> Registros { get; } = new();

    public Task RegistrarAsync(string nivel, string origen, string mensaje, int? idUsuario = null)
    {
        Registros.Add((nivel, origen, mensaje, idUsuario));
        return Task.CompletedTask;
    }
}

