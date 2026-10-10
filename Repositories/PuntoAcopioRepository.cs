using System.Globalization;
using Microsoft.EntityFrameworkCore;
using CampesinApp.Repositories.Models;
using CampesinApp.Dtos;

namespace CampesinApp.Repositories
{
    /// <summary>
    /// Implementación del Repositorio de Puntos de Acopio (CU-018).
    /// REGLAS ARQUITECTÓNICAS:
    /// - Opera ÚNICAMENTE sobre las tablas existentes en la BD (demanda, municipio, departamento).
    /// - No requiere permisos DDL (CREATE TABLE) ni scripts SQL adicionales.
    /// - Los datos del punto se guardan en la tabla existente 'demanda' usando direccion_Inicial,
    ///   direccion_Final, descripcion_Deman, cantidad_Deman, etc.
    /// </summary>
    public class PuntoAcopioRepository : IPuntoAcopioRepository
    {
        private readonly Campesinapp3Context _context;

        public PuntoAcopioRepository(Campesinapp3Context context)
        {
            _context = context;
        }

        /// <summary>
        /// Comprueba si ya existe un punto de acopio con la misma dirección o coordenadas.
        /// Consulta la tabla existente 'demanda' (direccion_Inicial, direccion_Final y descripcion_Deman).
        /// Cumple con la excepción FE-02-CU018.
        /// </summary>
        public async Task<bool> ExistePuntoDuplicadoAsync(string direccion, decimal latitud, decimal longitud)
        {
            string dirNormalizada = direccion.Trim().ToLower();
            string latStr = latitud.ToString("F4", CultureInfo.InvariantCulture);

            return await _context.Demanda
                .AsNoTracking()
                .AnyAsync(d => (d.DireccionInicial != null && d.DireccionInicial.ToLower() == dirNormalizada) ||
                               (d.DireccionFinal != null && d.DireccionFinal.ToLower() == dirNormalizada) ||
                               (d.DescripcionDeman != null && d.DescripcionDeman.Contains(latStr)));
        }

        /// <summary>
        /// Guarda el nuevo registro en la tabla existente 'demanda' de la base de datos.
        /// Utiliza direccion_Inicial (VARCHAR 50), direccion_Final (VARCHAR 50) y descripcion_Deman (VARCHAR 255).
        /// </summary>
        public async Task<bool> GuardarPuntoAcopioAsync(PuntoAcopioRegistroDto datos)
        {
            // 1. Obtenemos IDs foráneos válidos existentes en la BD para garantizar integridad referencial
            int tipoProductoId = await _context.TipoProductos
                .Select(tp => tp.IdTipoProducto)
                .FirstOrDefaultAsync();

            int variedadId = await _context.Variedads
                .Where(v => v.IdTipoProducto == tipoProductoId)
                .Select(v => v.IdVariedad)
                .FirstOrDefaultAsync();

            if (variedadId == 0)
            {
                variedadId = await _context.Variedads
                    .Select(v => v.IdVariedad)
                    .FirstOrDefaultAsync();
            }

            int estadoId = await _context.EstadoDemanda
                .Select(e => e.IdEstadoDeman)
                .FirstOrDefaultAsync();

            if (estadoId == 0) estadoId = 1;

            // 2. Respetamos la longitud máxima de VARCHAR(50) de la columna direccion_Inicial
            string direccionRecortada = datos.Direccion.Length > 50 
                ? datos.Direccion.Substring(0, 50) 
                : datos.Direccion;

            // 3. En direccion_Final (VARCHAR 50) guardamos el Nombre del Punto de Acopio
            string nombreRecortado = datos.NombrePunto.Length > 50 
                ? datos.NombrePunto.Substring(0, 50) 
                : datos.NombrePunto;

            // 4. En descripcion_Deman (VARCHAR 255) almacenamos las coordenadas GPS y el acceso vehicular
            string latLngStr = $"{datos.Latitud.ToString(CultureInfo.InvariantCulture)},{datos.Longitud.ToString(CultureInfo.InvariantCulture)}";
            string descripcionCompleta = $"Punto: {datos.NombrePunto} | GPS: {latLngStr} | Acceso: {(datos.AccesoVehicular ? "SI" : "NO")}";
            if (descripcionCompleta.Length > 255)
            {
                descripcionCompleta = descripcionCompleta.Substring(0, 255);
            }

            // 5. Instanciamos la entidad Demandum de EF Core
            var nuevoRegistro = new Demandum
            {
                IdUsuario = datos.IdUsuario,
                IdMunicipio = datos.IdMunicipio,
                IdTipoProducto = tipoProductoId > 0 ? tipoProductoId : 1,
                IdVariedad = variedadId > 0 ? variedadId : 1,
                IdEstadoDeman = estadoId,
                DireccionInicial = direccionRecortada,
                DireccionFinal = nombreRecortado,
                CantidadDeman = (int)datos.Capacidad,
                PrecioDeman = 0,
                FechaEntrega = DateTime.Now.AddDays(30),
                LimTiemDeman = DateTime.Now.AddDays(30),
                DescripcionDeman = descripcionCompleta
            };

            await _context.Demanda.AddAsync(nuevoRegistro);
            int filasAfectadas = await _context.SaveChangesAsync();
            return filasAfectadas > 0;
        }

        public async Task<List<Departamento>> ObtenerDepartamentosAsync()
        {
            return await _context.Departamentos
                .AsNoTracking()
                .OrderBy(d => d.Departamento1)
                .ToListAsync();
        }

        public async Task<List<Municipio>> ObtenerMunicipiosPorDepartamentoAsync(int idDepartamento)
        {
            return await _context.Municipios
                .AsNoTracking()
                .Where(m => m.IdDepartamento == idDepartamento)
                .OrderBy(m => m.Municipio1)
                .ToListAsync();
        }

        public async Task<List<Municipio>> ObtenerTodosMunicipiosAsync()
        {
            return await _context.Municipios
                .AsNoTracking()
                .OrderBy(m => m.Municipio1)
                .ToListAsync();
        }
    }
}

