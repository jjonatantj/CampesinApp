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
        /// Obtiene las demandas aplicando los nuevos filtros jerárquicos:
        /// 1. Tipo de Producto
        /// 2. Variedad (dependiente del tipo de producto)
        /// 3. Departamento
        /// 4. Municipio (dependiente del departamento)
        /// </summary>
        public async Task<List<Demandum>> ObtenerDemandasFiltradasAsync(int? idTipoProducto, int? idVariedad, int? idDepartamento, int? idMunicipio)
        {
            DateTime hoy = DateTime.Today;

            var query = _context.Demanda
                .AsNoTracking()
                .Include(d => d.IdTipoProductoNavigation)
                .Include(d => d.IdVariedadNavigation)
                .Include(d => d.IdPuntoAcopioNavigation)
                    .ThenInclude(p => p.IdMunicipioNavigation)
                        .ThenInclude(m => m.IdDepartamentoNavigation)
                .Include(d => d.IdUsuarioNavigation)
                    .ThenInclude(u => u.IdMunicipioNavigation)
                        .ThenInclude(m => m.IdDepartamentoNavigation)
                .AsQueryable();

            // Regla de Negocio 1 (CU-011): Fecha límite de oferta
            // El día límite es el último día disponible para ofertar. A partir del día siguiente ya no aparece en el catálogo.
            query = query.Where(d => d.LimTiemDeman.Date >= hoy);

            // Regla de Negocio 2 (CU-011): Demanda activa y cupo disponible
            // Se excluyen demandas en tránsito (3) o finalizadas (4),
            // y aquellas que ya hayan completado o superado el cupo total de kilos solicitados.
            query = query.Where(d => (d.IdEstadoDeman == 1 || d.IdEstadoDeman == 2)
                                  && (d.AporteCamps.Sum(a => (int?)a.CantAporte) ?? 0) < d.CantidadDeman);

            // 1. Filtro por Tipo de Producto
            if (idTipoProducto.HasValue && idTipoProducto.Value > 0)
            {
                query = query.Where(d => d.IdTipoProducto == idTipoProducto.Value);
            }

            // 2. Filtro por Variedad
            if (idVariedad.HasValue && idVariedad.Value > 0)
            {
                query = query.Where(d => d.IdVariedad == idVariedad.Value);
            }

            // 3. Filtro por Departamento
            if (idDepartamento.HasValue && idDepartamento.Value > 0)
            {
                query = query.Where(d =>
                    (d.IdPuntoAcopioNavigation != null && d.IdPuntoAcopioNavigation.IdMunicipioNavigation.IdDepartamento == idDepartamento.Value) ||
                    (d.IdPuntoAcopioNavigation == null && d.IdUsuarioNavigation != null && d.IdUsuarioNavigation.IdMunicipioNavigation.IdDepartamento == idDepartamento.Value));
            }

            // 4. Filtro por Municipio
            if (idMunicipio.HasValue && idMunicipio.Value > 0)
            {
                query = query.Where(d =>
                    (d.IdPuntoAcopioNavigation != null && d.IdPuntoAcopioNavigation.IdMunicipio == idMunicipio.Value) ||
                    (d.IdPuntoAcopioNavigation == null && d.IdUsuarioNavigation != null && d.IdUsuarioNavigation.IdMunicipio == idMunicipio.Value));
            }

            return await query.ToListAsync();
        }

        /// <summary>
        /// Obtiene los tipos de producto disponibles para el primer filtro.
        /// </summary>
        public async Task<List<TipoProducto>> ObtenerTiposProductoAsync()
        {
            return await _context.TipoProductos
                .AsNoTracking()
                .OrderBy(tp => tp.Producto)
                .ToListAsync();
        }

        /// <summary>
        /// Obtiene las variedades asociadas a un tipo de producto específico (cascada).
        /// </summary>
        public async Task<List<Variedad>> ObtenerVariedadesPorTipoProductoAsync(int idTipoProducto)
        {
            return await _context.Variedads
                .AsNoTracking()
                .Where(v => v.IdTipoProducto == idTipoProducto)
                .OrderBy(v => v.VariedadProd)
                .ToListAsync();
        }

        /// <summary>
        /// Obtiene los departamentos de Colombia para el filtro geográfico.
        /// </summary>
        public async Task<List<Departamento>> ObtenerDepartamentosAsync()
        {
            return await _context.Departamentos
                .AsNoTracking()
                .OrderBy(d => d.Departamento1)
                .ToListAsync();
        }

        /// <summary>
        /// Obtiene los municipios asociados a un departamento específico (cascada).
        /// </summary>
        public async Task<List<Municipio>> ObtenerMunicipiosPorDepartamentoAsync(int idDepartamento)
        {
            return await _context.Municipios
                .AsNoTracking()
                .Where(m => m.IdDepartamento == idDepartamento)
                .OrderBy(m => m.Municipio1)
                .ToListAsync();
        }

        /// <summary>
        /// Obtiene las demandas con los 3 filtros opcionales del CU-011:
        /// Categoría (Variedad/Tipo), Producto y Región (Municipio/Departamento).
        /// </summary>
        public async Task<List<Demandum>> ObtenerDemandasFiltradasAsync(string? categoria, string? producto, string? region)
        {
            DateTime hoy = DateTime.Today;

            var query = _context.Demanda
                .AsNoTracking()
                .Include(d => d.IdTipoProductoNavigation)
                .Include(d => d.IdVariedadNavigation)
                .Include(d => d.IdPuntoAcopioNavigation)
                    .ThenInclude(p => p.IdMunicipioNavigation)
                        .ThenInclude(m => m.IdDepartamentoNavigation)
                .AsQueryable();

            // Regla de Negocio 1 (CU-011): Fecha límite de oferta
            query = query.Where(d => d.LimTiemDeman.Date >= hoy);

            // Regla de Negocio 2 (CU-011): Demanda activa y cupo disponible
            query = query.Where(d => (d.IdEstadoDeman == 1 || d.IdEstadoDeman == 2)
                                  && (d.AporteCamps.Sum(a => (int?)a.CantAporte) ?? 0) < d.CantidadDeman);

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

