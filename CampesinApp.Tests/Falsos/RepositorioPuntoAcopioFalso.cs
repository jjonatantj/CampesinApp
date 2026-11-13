using CampesinApp.Dtos;
using CampesinApp.Repositories;
using CampesinApp.Repositories.Models;

namespace CampesinApp.Tests.Falsos;

/// <summary>
/// Repositorio en memoria para probar reglas de puntos de acopio sin conectarse a SQL Server.
/// </summary>
public class RepositorioPuntoAcopioFalso : IPuntoAcopioRepository
{
    public List<PuntoAcopio> Puntos { get; } = new();
    public List<Departamento> Departamentos { get; } = new();
    public List<Municipio> Municipios { get; } = new();
    public List<Demandum> Demandas { get; } = new();
    public List<PuntoAcopioRegistroDto> Guardados { get; } = new();

    public Task<bool> ExistePuntoDuplicadoAsync(string direccion, decimal latitud, decimal longitud)
    {
        var existe = Puntos.Any(p =>
            p.Direccion.Equals(direccion.Trim(), StringComparison.OrdinalIgnoreCase)
            || (p.Latitud == latitud && p.Longitud == longitud));

        return Task.FromResult(existe);
    }

    public Task<int> GuardarPuntoAcopioAsync(PuntoAcopioRegistroDto datos)
    {
        Guardados.Add(datos);
        var nuevoId = Puntos.Count + 1;
        Puntos.Add(new PuntoAcopio
        {
            IdPuntoAcopio = nuevoId,
            NombrePunto = datos.NombrePunto,
            Direccion = datos.Direccion,
            Latitud = datos.Latitud,
            Longitud = datos.Longitud,
            IdMunicipio = datos.IdMunicipio,
            TipoAcopio = datos.TipoAcopio
        });
        return Task.FromResult(nuevoId);
    }

    public Task<List<Departamento>> ObtenerDepartamentosAsync() =>
        Task.FromResult(Departamentos.ToList());

    public Task<List<Municipio>> ObtenerMunicipiosPorDepartamentoAsync(int idDepartamento) =>
        Task.FromResult(Municipios.Where(m => m.IdDepartamento == idDepartamento).ToList());

    public Task<List<Municipio>> ObtenerTodosMunicipiosAsync() =>
        Task.FromResult(Municipios.ToList());

    public Task<List<PuntoAcopio>> ObtenerPuntosAcopioFiltradosAsync(
        bool? tipoAcopio,
        int? idDepartamento,
        int? idMunicipio,
        int? idDemanda = null)
    {
        var consulta = Puntos.AsEnumerable();

        if (tipoAcopio.HasValue)
            consulta = consulta.Where(p => p.TipoAcopio == tipoAcopio.Value);

        if (idMunicipio.HasValue)
            consulta = consulta.Where(p => p.IdMunicipio == idMunicipio.Value);

        return Task.FromResult(consulta.ToList());
    }

    public Task<List<Demandum>> ObtenerDemandasParaFiltroAsync() =>
        Task.FromResult(Demandas.ToList());
}
