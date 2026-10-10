using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CampesinApp.Repositories.Models;

namespace CampesinApp.Repositories
{
    /// <summary>
    /// Implementación del repositorio para el Caso de Uso CU-020: Asignación de Vehículo.
    /// REGLA ARQUITECTÓNICA: Contiene exclusivamente consultas y persistencia con EF Core / LINQ.
    /// No contiene reglas de validación de presentación.
    /// </summary>
    public class AsignacionVehiculoRepository : IAsignacionVehiculoRepository
    {
        private readonly Campesinapp3Context _context;

        public AsignacionVehiculoRepository(Campesinapp3Context context)
        {
            _context = context;
        }

        /// <summary>
        /// Obtiene todas las demandas que están pendientes de asignación de transporte (id_Camion == null).
        /// </summary>
        public async Task<List<Demandum>> ObtenerCargasPendientesAsync()
        {
            return await _context.Demanda
                .Include(d => d.IdTipoProductoNavigation)
                .Include(d => d.IdVariedadNavigation)
                .Include(d => d.IdMunicipioNavigation)
                    .ThenInclude(m => m.IdDepartamentoNavigation)
                .Include(d => d.IdUsuarioNavigation)
                .Where(d => d.IdCamion == null)
                .OrderByDescending(d => d.FechaEntrega)
                .AsNoTracking()
                .ToListAsync();
        }

        /// <summary>
        /// Consulta el detalle de una demanda por su ID con todas las relaciones requeridas para el paso 4 del CU-020.
        /// </summary>
        public async Task<Demandum?> ObtenerDemandaPorIdAsync(int idDemanda)
        {
            return await _context.Demanda
                .Include(d => d.IdTipoProductoNavigation)
                .Include(d => d.IdVariedadNavigation)
                .Include(d => d.IdMunicipioNavigation)
                    .ThenInclude(m => m.IdDepartamentoNavigation)
                .Include(d => d.IdUsuarioNavigation)
                    .ThenInclude(u => u.IdMunicipioNavigation)
                .Include(d => d.AporteCamps)
                    .ThenInclude(a => a.IdUsuarioNavigation)
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.IdDemanda == idDemanda);
        }

        /// <summary>
        /// Consulta los camiones idóneos para la carga:
        /// 1. Papeles al día: pase, seguro y tecnomecánica vigentes (RN-02-CU020).
        /// 2. Capacidad igual o superior a la carga demandada (RN-01-CU020).
        /// 3. Sin viaje activo en otra demanda no finalizada.
        /// </summary>
        public async Task<List<Camion>> ObtenerCamionesDisponiblesAsync(int capacidadMinimaKg, int? idDemandaExcluir = null)
        {
            // Obtenemos los IDs de camiones asignados actualmente a otras demandas activas
            var camionesOcupadosQuery = _context.Demanda
                .Where(d => d.IdCamion != null);

            if (idDemandaExcluir.HasValue)
            {
                camionesOcupadosQuery = camionesOcupadosQuery.Where(d => d.IdDemanda != idDemandaExcluir.Value);
            }

            var idsCamionesOcupados = await camionesOcupadosQuery
                .Select(d => d.IdCamion!.Value)
                .Distinct()
                .ToListAsync();

            // Consultamos los camiones que cumplen las reglas de capacidad, documentos y disponibilidad
            return await _context.Camions
                .Include(c => c.IdUsuarioNavigation)
                    .ThenInclude(u => u.IdMunicipioNavigation)
                .Where(c => c.EstadoPase && c.EstadoSeguro && c.EstadoTecno)
                .Where(c => c.CapacidadCami >= capacidadMinimaKg)
                .Where(c => !idsCamionesOcupados.Contains(c.IdCamion))
                .OrderBy(c => c.CapacidadCami)
                .AsNoTracking()
                .ToListAsync();
        }

        /// <summary>
        /// Consulta un camión por su identificador único junto con la entidad de su conductor.
        /// </summary>
        public async Task<Camion?> ObtenerCamionPorIdAsync(int idCamion)
        {
            return await _context.Camions
                .Include(c => c.IdUsuarioNavigation)
                    .ThenInclude(u => u.IdMunicipioNavigation)
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.IdCamion == idCamion);
        }

        /// <summary>
        /// Persiste la asignación del camión en la demanda actualizando id_Camion.
        /// </summary>
        public async Task<bool> AsignarCamionADemandaAsync(int idDemanda, int idCamion)
        {
            var demanda = await _context.Demanda.FirstOrDefaultAsync(d => d.IdDemanda == idDemanda);
            if (demanda == null)
            {
                return false;
            }

            demanda.IdCamion = idCamion;
            await _context.SaveChangesAsync();
            return true;
        }
    }
}

