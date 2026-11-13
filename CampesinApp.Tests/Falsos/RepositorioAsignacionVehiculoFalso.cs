using CampesinApp.Repositories;
using CampesinApp.Repositories.Models;

namespace CampesinApp.Tests.Falsos;

/// <summary>
/// Repositorio en memoria para probar AsignacionVehiculoController sin conectarse a SQL Server.
/// </summary>
public class RepositorioAsignacionVehiculoFalso : IAsignacionVehiculoRepository
{
    public List<Demandum> Demandas { get; } = new();
    public List<Camion> Camiones { get; } = new();
    public List<(int IdDemanda, int IdCamion)> AsignacionesGuardadas { get; } = new();

    public Task<List<Demandum>> ObtenerCargasPendientesAsync()
    {
        var pendientes = Demandas.Where(d => d.IdCamion == null).ToList();
        return Task.FromResult(pendientes);
    }

    public Task<Demandum?> ObtenerDemandaPorIdAsync(int idDemanda)
    {
        var demanda = Demandas.FirstOrDefault(d => d.IdDemanda == idDemanda);
        return Task.FromResult(demanda);
    }

    public Task<List<Camion>> ObtenerCamionesDisponiblesAsync(int capacidadMinimaKg, int? idDemandaExcluir = null)
    {
        var ocupados = Demandas
            .Where(d => d.IdCamion != null && d.IdDemanda != idDemandaExcluir)
            .Select(d => d.IdCamion!.Value)
            .ToHashSet();

        var disponibles = Camiones
            .Where(c => c.EstadoPase
                     && c.EstadoSeguro
                     && c.EstadoTecno
                     && c.CapacidadCami >= capacidadMinimaKg
                     && !ocupados.Contains(c.IdCamion))
            .ToList();

        return Task.FromResult(disponibles);
    }

    public Task<Camion?> ObtenerCamionPorIdAsync(int idCamion)
    {
        var camion = Camiones.FirstOrDefault(c => c.IdCamion == idCamion);
        return Task.FromResult(camion);
    }

    public Task<bool> AsignarCamionADemandaAsync(int idDemanda, int idCamion)
    {
        var demanda = Demandas.FirstOrDefault(d => d.IdDemanda == idDemanda);
        if (demanda == null) return Task.FromResult(false);

        demanda.IdCamion = idCamion;
        AsignacionesGuardadas.Add((idDemanda, idCamion));
        return Task.FromResult(true);
    }
}

