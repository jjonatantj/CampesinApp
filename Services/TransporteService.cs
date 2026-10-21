using CampesinApp.Dtos;
using CampesinApp.Repositories.Models;
using Microsoft.EntityFrameworkCore;

namespace CampesinApp.Services
{
    /// <summary>
    /// Datos de transporte: consulta (CU-023) y asignación de datos logísticos / viajes del transportador (CU-024).
    /// Usa las tablas existentes: el camión de un pedido es demanda.id_Camion, y los datos del conductor y del vehículo
    /// salen de camion y usuario. No hay una tabla de viajes.
    /// </summary>
    public class TransporteService
    {
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

            var viaje = await _db.Viajes().AsNoTracking().FirstOrDefaultAsync(v => v.IdDemanda == idDemanda);
            if (viaje == null)
            {
                return ResultadoOperacion<DetalleTransporteDto>.Falla(TipoError.NoEncontrado, "Actualmente este pedido no tiene asignado un servicio de transporte");
            }

            return ResultadoOperacion<DetalleTransporteDto>.Ok(new DetalleTransporteDto
            {
                IdDemanda = idDemanda,
                CodigoPedido = CodigoPedido.Formatear(idDemanda),
                IdTransportador = viaje.IdUsuarioTransportador,
                NombreTransportador = viaje.NombreTransportador,
                TelefonoTransportador = viaje.TelefonoTransportador,
                Placa = viaje.Placa,
                Modelo = viaje.Modelo,
                Capacidad = viaje.Capacidad,
                EstadoEnvio = viaje.EstadoEnvio,
                Reputacion = await _calificaciones.PromedioTransportadorAsync(viaje.IdUsuarioTransportador)
            });
        }

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

        /// <summary>FA-01-CU023: el administrador busca por placa del camión o por código del pedido.</summary>
        public async Task<int?> BuscarPedidoAsync(string? texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return null;

            if (CodigoPedido.TryParsear(texto, out var idDemanda)
                && await _db.Demanda.AnyAsync(d => d.IdDemanda == idDemanda))
            {
                return idDemanda;
            }

            var placa = NormalizarPlaca(texto);
            return await _db.Viajes()
                .AsNoTracking()
                .Where(v => v.Placa == placa)
                .OrderByDescending(v => v.IdViaje)
                .Select(v => (int?)v.IdDemanda)
                .FirstOrDefaultAsync();
        }

        /// <summary>Opciones para el formulario "Asignación de Datos de Transporte": pedidos sin camión y camiones registrados.</summary>
        public async Task CargarOpcionesAsignacionAsync(AsignacionTransporteDto dto)
        {
            dto.Pedidos = (await _db.Demanda
                    .AsNoTracking()
                    .Where(d => d.IdCamion == null)
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

            dto.Camiones = (await _db.Camions
                    .AsNoTracking()
                    .Where(c => c.IdUsuarioNavigation.Estado)
                    .OrderBy(c => c.IdUsuarioNavigation.NombreCom)
                    .ThenBy(c => c.PlacaCami)
                    .Select(c => new
                    {
                        c.IdCamion,
                        c.PlacaCami,
                        c.ModeloCami,
                        c.CapacidadCami,
                        c.IdUsuarioNavigation.NombreCom,
                        c.IdUsuarioNavigation.ApellidoCom,
                        c.IdUsuarioNavigation.TelelfonoUsu
                    })
                    .ToListAsync())
                .Select(c => new OpcionDto
                {
                    Id = c.IdCamion,
                    Texto = $"{c.PlacaCami} · {c.ModeloCami} · {c.CapacidadCami} t · {c.NombreCom} {c.ApellidoCom}".Trim(),
                    Extra = c.TelelfonoUsu.ToString()
                })
                .ToList();
        }

        /// <summary>
        /// Asigna el camión de un transportador al pedido (CU-024, RN-02): el vehículo, su capacidad y el teléfono del
        /// conductor salen de las tablas camion y usuario; solo se guarda demanda.id_Camion.
        /// </summary>
        public async Task<ResultadoOperacion> AsignarAsync(UsuarioActual admin, AsignacionTransporteDto dto)
        {
            var pedido = await _db.Demanda.FirstOrDefaultAsync(d => d.IdDemanda == dto.IdDemanda);
            if (pedido == null)
            {
                return ResultadoOperacion.Falla(TipoError.NoEncontrado, "El pedido seleccionado no existe.");
            }

            if (pedido.IdCamion != null)
            {
                return ResultadoOperacion.Falla(TipoError.Conflicto, "Este pedido ya tiene datos de transporte asignados.");
            }

            var camion = await _db.Camions
                .AsNoTracking()
                .Where(c => c.IdCamion == dto.IdCamion && c.IdUsuarioNavigation.Estado)
                .Select(c => new { c.IdCamion, c.PlacaCami, c.EstadoPase, c.EstadoSeguro, c.EstadoTecno, c.IdUsuarioNavigation.IdRolNavigation.TipoRol })
                .FirstOrDefaultAsync();

            if (camion == null || RolUsuarioExtensions.Desde(camion.TipoRol) != RolUsuario.Transportador)
            {
                return ResultadoOperacion.Falla(TipoError.Validacion, "El camión seleccionado no es válido.");
            }

            if (!camion.EstadoPase || !camion.EstadoSeguro || !camion.EstadoTecno)
            {
                return ResultadoOperacion.Falla(TipoError.Validacion, "El camión tiene documentos vencidos (pase, seguro o tecnomecánica) y no se puede asignar.");
            }

            // Otro administrador pudo asignar el mismo pedido al mismo tiempo: la actualización solo aplica si sigue sin camión.
            var filas = await _db.Demanda
                .Where(d => d.IdDemanda == pedido.IdDemanda && d.IdCamion == null)
                .ExecuteUpdateAsync(s => s.SetProperty(d => d.IdCamion, camion.IdCamion));
            if (filas == 0)
            {
                return ResultadoOperacion.Falla(TipoError.Conflicto, "Este pedido ya tiene datos de transporte asignados.");
            }

            await _log.RegistrarAsync(NivelesLog.Info, "CU-024 Asignación transporte",
                $"Pedido {CodigoPedido.Formatear(pedido.IdDemanda)} asignado al camión {camion.PlacaCami}.", admin.Id);
            return ResultadoOperacion.Ok("Datos de transporte guardados con éxito");
        }

        /// <summary>"Mis Viajes" del transportador. Los viajes entregados dejan de aparecer (RN-03-CU024).</summary>
        public async Task<List<ViajeTransportadorDto>> ListarMisViajesAsync(UsuarioActual transportador)
        {
            var filas = await (
                    from v in _db.Viajes()
                    join d in _db.Demanda on v.IdDemanda equals d.IdDemanda
                    where v.IdUsuarioTransportador == transportador.Id && v.VisibleTransportador
                    orderby v.IdViaje
                    select new
                    {
                        v.IdViaje,
                        v.IdDemanda,
                        Producto = d.IdTipoProductoNavigation.Producto + " " + d.IdVariedadNavigation.VariedadProd,
                        d.CantidadDeman,
                        DireccionFinal = d.IdPuntoAcopioNavigation != null ? d.IdPuntoAcopioNavigation.Direccion : (d.DireccionInicial ?? "Punto de Acopio"),
                        v.EstadoEnvio,
                        v.LlegadaNotificada
                    })
                .AsNoTracking()
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
