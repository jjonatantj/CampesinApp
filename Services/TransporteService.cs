using System.Text.RegularExpressions;
using CampesinApp.Dtos;
using CampesinApp.Repositories.Models;
using Microsoft.EntityFrameworkCore;

namespace CampesinApp.Services
{
    /// <summary>Datos de transporte: consulta (CU-023) y registro de datos logísticos y viajes del transportador (CU-024).</summary>
    public class TransporteService
    {
        private static readonly Regex FormatoPlaca = new(@"^[A-Z0-9]{5,8}$", RegexOptions.Compiled);

        private readonly Campesinapp3Context _db;
        private readonly PedidoService _pedidos;
        private readonly CalificacionService _calificaciones;
        private readonly ILogService _log;

        public TransporteService(Campesinapp3Context db, PedidoService pedidos, CalificacionService calificaciones, ILogService log)
        {
            _db = db;
            _pedidos = pedidos;
            _calificaciones = calificaciones;
            _log = log;
        }

        /// <summary>"Detalle Logístico del Transporte" (CU-023). RN01: solo participantes del pedido o administrador.</summary>
        public async Task<ResultadoOperacion<DetalleTransporteDto>> ObtenerDetalleAsync(UsuarioActual usuario, int idDemanda)
        {
            if (!await _pedidos.ParticipaAsync(usuario, idDemanda))
            {
                // El administrador ve todos los pedidos, así que para él solo puede significar que no existe;
                // para los demás no se revela si el pedido existe.
                return usuario.EsAdmin
                    ? ResultadoOperacion<DetalleTransporteDto>.Falla(TipoError.NoEncontrado, "El pedido indicado no existe.")
                    : ResultadoOperacion<DetalleTransporteDto>.Falla(TipoError.NoAutorizado, "No tiene permisos para consultar la información logística de este pedido");
            }

            var viaje = await _db.Viajes.AsNoTracking().FirstOrDefaultAsync(v => v.IdDemanda == idDemanda);
            if (viaje == null)
            {
                return ResultadoOperacion<DetalleTransporteDto>.Falla(TipoError.NoEncontrado, "Actualmente este pedido no tiene asignado un servicio de transporte");
            }

            return ResultadoOperacion<DetalleTransporteDto>.Ok(new DetalleTransporteDto
            {
                IdDemanda = idDemanda,
                CodigoPedido = CodigoPedido.Formatear(idDemanda),
                NombreTransportador = viaje.NombreTransportador,
                TelefonoTransportador = viaje.TelefonoTransportador,
                Placa = viaje.Placa,
                Modelo = viaje.Modelo,
                Capacidad = viaje.Capacidad,
                EstadoEnvio = viaje.EstadoEnvio,
                Reputacion = await _calificaciones.PromedioAsync(viaje.IdUsuarioTransportador)
            });
        }

        /// <summary>FA-01-CU023: el administrador busca por placa del camión o por código del pedido.</summary>
        /// <summary>Datos del camión y su conductor para la tarjeta "Ver detalles del flete" (CU-023). Null si no existe.</summary>
        public async Task<CamionFleteDto?> ObtenerCamionAsync(int idCamion)
        {
            return await _db.Camions
                .AsNoTracking()
                .Where(c => c.IdCamion == idCamion)
                .Select(c => new CamionFleteDto
                {
                    IdCamion = c.IdCamion,
                    NombreTransportador = (c.IdUsuarioNavigation.NombreCom + " " + c.IdUsuarioNavigation.ApellidoCom).Trim(),
                    TelefonoTransportador = c.IdUsuarioNavigation.TelelfonoUsu.ToString(),
                    Placa = c.PlacaCami,
                    Modelo = c.ModeloCami,
                    Capacidad = c.CapacidadCami,
                    PaseAlDia = c.EstadoPase,
                    SeguroAlDia = c.EstadoSeguro,
                    TecnomecanicaAlDia = c.EstadoTecno
                })
                .FirstOrDefaultAsync();
        }

        public async Task<int?> BuscarPedidoAsync(string? texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return null;

            if (CodigoPedido.TryParsear(texto, out var idDemanda)
                && await _db.Demanda.AnyAsync(d => d.IdDemanda == idDemanda))
            {
                return idDemanda;
            }

            var placa = NormalizarPlaca(texto);
            return await _db.Viajes
                .AsNoTracking()
                .Where(v => v.Placa == placa)
                .OrderByDescending(v => v.IdViaje)
                .Select(v => (int?)v.IdDemanda)
                .FirstOrDefaultAsync();
        }

        /// <summary>Opciones para el formulario "Asignación de Datos de Transporte".</summary>
        public async Task CargarOpcionesAsignacionAsync(AsignacionTransporteDto dto)
        {
            dto.Pedidos = (await _db.Demanda
                    .AsNoTracking()
                    .Where(d => !_db.Viajes.Any(v => v.IdDemanda == d.IdDemanda))
                    .OrderBy(d => d.IdDemanda)
                    .Select(d => new
                    {
                        d.IdDemanda,
                        Producto = d.IdTipoProductoNavigation.Producto + " " + d.IdVariedadNavigation.VariedadProd,
                        d.CantidadDeman,
                        DireccionFinal = d.IdPuntoAcopioNavigation != null ? d.IdPuntoAcopioNavigation.Direccion : (d.DireccionInicial ?? "Punto de Acopio")
                    })
                    .ToListAsync())
                .Select(d => new OpcionDto
                {
                    Id = d.IdDemanda,
                    Texto = $"{CodigoPedido.Formatear(d.IdDemanda)} · {d.Producto} · {d.CantidadDeman} kg → {d.DireccionFinal}"
                })
                .ToList();

            var usuarios = await _db.Usuarios
                .AsNoTracking()
                .Where(u => u.Estado)
                .OrderBy(u => u.NombreCom)
                .Select(u => new { u.IdUsuario, u.NombreCom, u.ApellidoCom, u.TelelfonoUsu, u.IdRolNavigation.TipoRol })
                .ToListAsync();

            dto.Transportadores = usuarios
                .Where(u => RolUsuarioExtensions.Desde(u.TipoRol) == RolUsuario.Transportador)
                .Select(u => new OpcionDto
                {
                    Id = u.IdUsuario,
                    Texto = $"{u.NombreCom} {u.ApellidoCom}".Trim(),
                    Extra = u.TelelfonoUsu.ToString()
                })
                .ToList();
        }

        /// <summary>Guarda los datos logísticos del viaje (CU-024, RN-02): transportador, teléfono, placa, modelo y capacidad.</summary>
        public async Task<ResultadoOperacion> AsignarAsync(UsuarioActual admin, AsignacionTransporteDto dto)
        {
            var placa = NormalizarPlaca(dto.Placa!);
            if (!FormatoPlaca.IsMatch(placa) || dto.Capacidad is null or <= 0)
            {
                return ResultadoOperacion.Falla(TipoError.Validacion, "La placa o la capacidad de carga no son válidas.");
            }

            var pedido = await _db.Demanda.AsNoTracking().AnyAsync(d => d.IdDemanda == dto.IdDemanda);
            if (!pedido)
            {
                return ResultadoOperacion.Falla(TipoError.NoEncontrado, "El pedido seleccionado no existe.");
            }

            if (await _db.Viajes.AnyAsync(v => v.IdDemanda == dto.IdDemanda))
            {
                return ResultadoOperacion.Falla(TipoError.Conflicto, "Este pedido ya tiene datos de transporte asignados.");
            }

            var transportador = await _db.Usuarios
                .AsNoTracking()
                .Where(u => u.IdUsuario == dto.IdTransportador && u.Estado)
                .Select(u => new { u.IdUsuario, u.NombreCom, u.ApellidoCom, u.IdRolNavigation.TipoRol })
                .FirstOrDefaultAsync();

            if (transportador == null || RolUsuarioExtensions.Desde(transportador.TipoRol) != RolUsuario.Transportador)
            {
                return ResultadoOperacion.Falla(TipoError.Validacion, "El transportador seleccionado no es válido.");
            }

            _db.Viajes.Add(new Viaje
            {
                IdDemanda = dto.IdDemanda!.Value,
                IdUsuarioTransportador = transportador.IdUsuario,
                IdUsuarioAdmin = admin.Id,
                NombreTransportador = $"{transportador.NombreCom} {transportador.ApellidoCom}".Trim(),
                TelefonoTransportador = dto.Telefono!.Trim(),
                Placa = placa,
                Modelo = dto.Modelo!.Trim(),
                Capacidad = dto.Capacidad!.Value,
                EstadoEnvio = EstadosEnvio.Asignado,
                FechaAsignacion = DateTime.Now,
                VisibleTransportador = true
            });

            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // Otro administrador asignó el mismo pedido al mismo tiempo (índice único de viaje.id_Demanda).
                return ResultadoOperacion.Falla(TipoError.Conflicto, "Este pedido ya tiene datos de transporte asignados.");
            }

            await _log.RegistrarAsync(NivelesLog.Info, "CU-024 Asignación transporte",
                $"Pedido {CodigoPedido.Formatear(dto.IdDemanda!.Value)} asignado a {placa}.", admin.Id);
            return ResultadoOperacion.Ok("Datos de transporte guardados con éxito");
        }

        /// <summary>"Mis Viajes" del transportador. Los viajes entregados dejan de aparecer (RN-03-CU024).</summary>
        public async Task<List<ViajeTransportadorDto>> ListarMisViajesAsync(UsuarioActual transportador)
        {
            var filas = await _db.Viajes
                .AsNoTracking()
                .Where(v => v.IdUsuarioTransportador == transportador.Id && v.VisibleTransportador)
                .OrderBy(v => v.IdViaje)
                .Select(v => new
                {
                    v.IdViaje,
                    v.IdDemanda,
                    Producto = v.IdDemandaNavigation.IdTipoProductoNavigation.Producto + " " + v.IdDemandaNavigation.IdVariedadNavigation.VariedadProd,
                    v.IdDemandaNavigation.CantidadDeman,
                    DireccionFinal = v.IdDemandaNavigation.IdPuntoAcopioNavigation != null ? v.IdDemandaNavigation.IdPuntoAcopioNavigation.Direccion : (v.IdDemandaNavigation.DireccionInicial ?? "Punto de Acopio"),
                    v.EstadoEnvio,
                    v.LlegadaNotificada
                })
                .ToListAsync();

            return filas.Select(f => new ViajeTransportadorDto
            {
                IdViaje = f.IdViaje,
                IdDemanda = f.IdDemanda,
                CodigoPedido = CodigoPedido.Formatear(f.IdDemanda),
                Producto = f.Producto,
                Cantidad = f.CantidadDeman,
                DireccionFinal = f.DireccionFinal,
                EstadoEnvio = f.EstadoEnvio,
                Activo = !EstadosEnvio.Finalizados.Contains(f.EstadoEnvio),
                LlegadaNotificada = f.LlegadaNotificada
            }).ToList();
        }

        public static string NormalizarPlaca(string placa) =>
            placa.Replace("-", string.Empty).Replace(" ", string.Empty).Trim().ToUpperInvariant();
    }
}
