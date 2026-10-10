using Microsoft.EntityFrameworkCore;
using CampesinApp.Repositories.Models;

namespace CampesinApp.Repositories
{
    /// <summary>
    /// Repositorio para la entidad Demanda.
    /// Reglas arquitectónicas:
    /// 1. Se encarga únicamente del acceso a datos usando LINQ y Entity Framework Core.
    /// 2. No contiene lógica de negocio.
    /// </summary>
    public class DemandaRepository
    {
        private readonly Campesinapp3Context _context;

        public DemandaRepository(Campesinapp3Context context)
        {
            _context = context;
        }

        /// <summary>
        /// Obtiene las demandas con los 3 filtros opcionales del CU-011:
        /// Categoría (Variedad/Tipo), Producto y Región (Municipio/Departamento).
        /// </summary>
        public async Task<List<Demandum>> ObtenerDemandasFiltradasAsync(string? categoria, string? producto, string? region)
        {
            var query = _context.Demanda
                .Include(d => d.IdTipoProductoNavigation)
                .Include(d => d.IdVariedadNavigation)
                .Include(d => d.IdPuntoAcopioNavigation)
                    .ThenInclude(p => p.IdMunicipioNavigation)
                        .ThenInclude(m => m.IdDepartamentoNavigation)
                .AsQueryable();

            // 1. Filtro por Categoría / Variedad
            if (!string.IsNullOrWhiteSpace(categoria))
            {
                query = query.Where(d => d.IdVariedadNavigation.VariedadProd == categoria);
            }

            // 2. Filtro por Producto
            if (!string.IsNullOrWhiteSpace(producto))
            {
                query = query.Where(d => d.IdTipoProductoNavigation.Producto == producto);
            }

            // 3. Filtro por Región (Municipio o Departamento)
            if (!string.IsNullOrWhiteSpace(region))
            {
                query = query.Where(d => d.IdPuntoAcopioNavigation != null &&
                                         (d.IdPuntoAcopioNavigation.IdMunicipioNavigation.Municipio1 == region ||
                                          d.IdPuntoAcopioNavigation.IdMunicipioNavigation.IdDepartamentoNavigation.Departamento1 == region));
            }

            return await query.ToListAsync();
        }

        /// <summary>
        /// Obtiene la lista de Categorías / Variedades disponibles para el primer desplegable.
        /// </summary>
        public async Task<List<string>> ObtenerNombresCategoriasAsync()
        {
            return await _context.Variedads
                .Select(v => v.VariedadProd)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();
        }

        /// <summary>
        /// Obtiene la lista de nombres de Productos disponibles para el segundo desplegable.
        /// </summary>
        public async Task<List<string>> ObtenerNombresProductosAsync()
        {
            return await _context.TipoProductos
                .Select(tp => tp.Producto)
                .Distinct()
                .OrderBy(p => p)
                .ToListAsync();
        }

        /// <summary>
        /// Obtiene la lista de nombres de Regiones / Municipios para el tercer desplegable.
        /// </summary>
        public async Task<List<string>> ObtenerNombresRegionesAsync()
        {
            return await _context.Municipios
                .Select(m => m.Municipio1)
                .Distinct()
                .OrderBy(m => m)
                .ToListAsync();
        }

        /// <summary>
        /// CU-012: Consulta el detalle completo de una demanda por su ID.
        /// Incluye producto, variedad, municipio, comprador (usuario) y la colección de aportes previos.
        /// </summary>
        public async Task<Demandum?> ObtenerDemandaPorIdAsync(int idDemanda)
        {
            return await _context.Demanda
                .Include(d => d.IdTipoProductoNavigation)
                .Include(d => d.IdVariedadNavigation)
                .Include(d => d.IdPuntoAcopioNavigation)
                    .ThenInclude(p => p.IdMunicipioNavigation)
                        .ThenInclude(m => m.IdDepartamentoNavigation)
                .Include(d => d.IdUsuarioNavigation)
                    .ThenInclude(u => u.IdMunicipioNavigation)
                .Include(d => d.AporteCamps)
                .FirstOrDefaultAsync(d => d.IdDemanda == idDemanda);
        }

        /// <summary>
        /// CU-012: Calcula la suma de todos los kilos ya aportados por los campesinos a esta demanda.
        /// Permite calcular en el negocio la cantidad faltante (CantidadDeman - TotalAportado).
        /// </summary>
        public async Task<int> ObtenerTotalAportadoAsync(int idDemanda)
        {
            return await _context.AporteCamps
                .Where(a => a.IdDemanda == idDemanda)
                .SumAsync(a => a.CantAporte);
        }
    }
}

