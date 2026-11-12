using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CampesinApp.Repositories.Models;
using CampesinApp.Dtos;

namespace CampesinApp.Repositories
{
    /// <summary>
    /// Implementación del Repositorio de Puntos de Acopio (CU-018 y CU-019).
    /// REGLAS ARQUITECTÓNICAS:
    /// - Opera exclusivamente con EF Core y LINQ sobre las tablas 'punto_Acopio', 'usuario_Punto_Acopio', 'municipio', 'departamento' y 'demanda'.
    /// - No contiene reglas de presentación ni lógica de controladores.
    /// </summary>
    public class PuntoAcopioRepository : IPuntoAcopioRepository
    {
        private readonly Campesinapp3Context _context;

        public PuntoAcopioRepository(Campesinapp3Context context)
        {
            _context = context;
        }

        /// <summary>
        /// Valida si ya existe un punto de acopio con la misma dirección o en las mismas coordenadas GPS.
        /// Consulta la tabla física 'punto_Acopio' para la excepción FE-02-CU018.
        /// </summary>
        public async Task<bool> ExistePuntoDuplicadoAsync(string direccion, decimal latitud, decimal longitud)
        {
            string dirNormalizada = direccion.Trim().ToLower();

            return await _context.PuntoAcopios
                .AsNoTracking()
                .AnyAsync(p => p.Direccion.ToLower() == dirNormalizada ||
                              (p.Latitud == latitud && p.Longitud == longitud));
        }

        /// <summary>
        /// Flujo de persistencia del CU-018:
        /// 1. Inserta el nuevo registro físico en 'punto_Acopio'.
        /// 2. SQL Server genera el id_Punto_Acopio autoincremental (Identity).
        /// 3. Inmediatamente inserta la relación en la tabla intermedia 'usuario_Punto_Acopio'.
        /// </summary>
        public async Task<int> GuardarPuntoAcopioAsync(PuntoAcopioRegistroDto datos)
        {
            // 1. Buscamos el usuario autenticado para asociarlo en la relación muchos a muchos
            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.IdUsuario == datos.IdUsuario);

            // 2. Instanciamos la entidad física PuntoAcopio con todos los campos del formulario
            var nuevoPunto = new PuntoAcopio
            {
                IdUsuario = datos.IdUsuario,
                IdMunicipio = datos.IdMunicipio,
                NombrePunto = datos.NombrePunto.Trim(),
                Direccion = datos.Direccion.Trim(),
                Telefono = datos.Telefono.Trim(),
                HorarioAtencion = datos.HorarioAtencion.Trim(),
                Latitud = datos.Latitud,
                Longitud = datos.Longitud,
                Capacidad = datos.Capacidad,
                AccesoVehicular = datos.AccesoVehicular,
                TipoAcopio = datos.TipoAcopio,
                Descripcion = datos.Descripcion,
                FechaRegistro = DateTime.Now
            };

            // 3. Guardamos en EF Core:
            // EF Core inserta en 'punto_Acopio' que ya cuenta con la FK directa 'id_Usuario'.
            await _context.PuntoAcopios.AddAsync(nuevoPunto);
            await _context.SaveChangesAsync();

            // 4. Intentamos registrar la relación intermedia en 'usuario_Punto_Acopio' si dicha tabla existe en la BD
            if (datos.IdUsuario > 0)
            {
                try
                {
                    await _context.Database.ExecuteSqlInterpolatedAsync(
                        $"INSERT INTO usuario_Punto_Acopio (id_Usuario, id_Punto_Acopio) VALUES ({datos.IdUsuario}, {nuevoPunto.IdPuntoAcopio})"
                    );
                }
                catch
                {
                    // Si la tabla intermedia usuario_Punto_Acopio no fue creada en la base de datos de esta máquina,
                    // no se interrumpe el flujo ya que la relación principal quedó guardada en punto_Acopio.id_Usuario.
                }
            }

            // Retornamos el ID autoincremental generado
            return nuevoPunto.IdPuntoAcopio;
        }

        /// <summary>
        /// Obtiene la lista completa de departamentos ordenada alfabéticamente para el primer desplegable.
        /// </summary>
        public async Task<List<Departamento>> ObtenerDepartamentosAsync()
        {
            return await _context.Departamentos
                .AsNoTracking()
                .OrderBy(d => d.Departamento1)
                .ToListAsync();
        }

        /// <summary>
        /// Obtiene los municipios correspondientes a un departamento seleccionado.
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
        /// Obtiene todos los municipios de la base de datos.
        /// </summary>
        public async Task<List<Municipio>> ObtenerTodosMunicipiosAsync()
        {
            return await _context.Municipios
                .Include(m => m.IdDepartamentoNavigation)
                .AsNoTracking()
                .OrderBy(m => m.Municipio1)
                .ToListAsync();
        }

        /// <summary>
        /// CU-019: Consulta los puntos de acopio activos aplicando filtros opcionales de Tipo (bit: 1=Comprador, 0=Campesino), Departamento y Municipio.
        /// </summary>
        public async Task<List<PuntoAcopio>> ObtenerPuntosAcopioFiltradosAsync(bool? tipoAcopio, int? idDepartamento, int? idMunicipio, int? idDemanda = null)
        {
            var query = _context.PuntoAcopios
                .Include(p => p.IdMunicipioNavigation)
                    .ThenInclude(m => m.IdDepartamentoNavigation)
                .Include(p => p.Demanda)
                .AsNoTracking()
                .AsQueryable();

            // 1. Filtro por Tipo de Acopio en Base de Datos (bit: 1 = Comprador, 0 = Campesino)
            if (tipoAcopio.HasValue)
            {
                query = query.Where(p => p.TipoAcopio == tipoAcopio.Value);
            }

            // 2. Filtro por Departamento (a través del municipio)
            if (idDepartamento.HasValue && idDepartamento.Value > 0)
            {
                query = query.Where(p => p.IdMunicipioNavigation.IdDepartamento == idDepartamento.Value);
            }

            // 3. Filtro por Municipio
            if (idMunicipio.HasValue && idMunicipio.Value > 0)
            {
                query = query.Where(p => p.IdMunicipio == idMunicipio.Value);
            }

            // 4. Filtro por Demanda (opcional por compatibilidad)
            if (idDemanda.HasValue && idDemanda.Value > 0)
            {
                query = query.Where(p => p.Demanda.Any(d => d.IdDemanda == idDemanda.Value)
                                      || _context.Demanda.Any(d => d.IdDemanda == idDemanda.Value && d.IdPuntoAcopio == p.IdPuntoAcopio));
            }

            // Regla de Negocio RN-01-CU019: Puntos con coordenadas válidas para renderizar en el mapa
            query = query.Where(p => p.Latitud != 0 && p.Longitud != 0);

            return await query
                .OrderBy(p => p.NombrePunto)
                .ToListAsync();
        }

        /// <summary>
        /// CU-019: Consulta las demandas para alimentar la lista desplegable de filtros.
        /// </summary>
        public async Task<List<Demandum>> ObtenerDemandasParaFiltroAsync()
        {
            return await _context.Demanda
                .Include(d => d.IdTipoProductoNavigation)
                .Include(d => d.IdVariedadNavigation)
                .Include(d => d.IdPuntoAcopioNavigation)
                .AsNoTracking()
                .OrderByDescending(d => d.IdDemanda)
                .ToListAsync();
        }
    }
}
