using CampesinApp.Dtos;
using CampesinApp.Repositories.Models;
using Microsoft.EntityFrameworkCore;

namespace CampesinApp.Services
{
    /// <summary>Historial de transacciones del usuario (CU-027) e historial global del administrador (CU-030).</summary>
    public class HistorialService
    {
        private const int MaximoFilas = 200;

        private readonly Campesinapp3Context _db;
        private readonly PedidoService _pedidos;

        public HistorialService(Campesinapp3Context db, PedidoService pedidos)
        {
            _db = db;
            _pedidos = pedidos;
        }

        /// <summary>
        /// Pedidos finalizados del usuario, del más reciente al más antiguo (RN-02-CU027). Cada rol ve lo suyo
        /// (RN-01-CU027). Si se indica búsqueda, filtra por código o ID del pedido (FA-01-CU027).
        /// </summary>
        public async Task<HistorialTransaccionesDto> ListarAsync(UsuarioActual usuario, string? busqueda)
        {
            var consulta = FinalizadosDe(_pedidos.PedidosVisibles(usuario));

            if (!string.IsNullOrWhiteSpace(busqueda))
            {
                if (!CodigoPedido.TryParsear(busqueda, out var idBuscado))
                {
                    return new HistorialTransaccionesDto { Busqueda = busqueda, EsCompra = usuario.Rol == RolUsuario.Comprador };
                }
                consulta = consulta.Where(x => x.Pedido.IdDemanda == idBuscado);
            }

            var transacciones = await ProyectarAsync(consulta.OrderByDescending(x => x.Viaje.FechaEntrega ?? x.Viaje.FechaLlegada ?? x.Viaje.FechaAsignacion).Take(MaximoFilas), usuario);
            return new HistorialTransaccionesDto
            {
                Busqueda = busqueda,
                EsCompra = usuario.Rol == RolUsuario.Comprador,
                Transacciones = transacciones
            };
        }

        public async Task<ResultadoOperacion<DetalleTransaccionDto>> ObtenerDetalleAsync(UsuarioActual usuario, int idDemanda)
        {
            var consulta = FinalizadosDe(_pedidos.PedidosVisibles(usuario)).Where(x => x.Pedido.IdDemanda == idDemanda);
            var transaccion = (await ProyectarAsync(consulta, usuario)).FirstOrDefault();
            if (transaccion == null)
            {
                return ResultadoOperacion<DetalleTransaccionDto>.Falla(TipoError.NoEncontrado, "No se encontró la transacción solicitada.");
            }

            var viaje = await _db.Viajes.AsNoTracking().FirstAsync(v => v.IdDemanda == idDemanda);
            var calificaciones = await _db.Calificacions
                .AsNoTracking()
                .Where(c => c.IdDemanda == idDemanda)
                .OrderBy(c => c.Fecha)
                .Select(c => new CalificacionVistaDto
                {
                    Evaluador = c.IdUsuarioEvaluaNavigation.NombreCom + " " + c.IdUsuarioEvaluaNavigation.ApellidoCom,
                    Evaluado = c.IdUsuarioEvaluadoNavigation.NombreCom + " " + c.IdUsuarioEvaluadoNavigation.ApellidoCom,
                    Estrellas = c.Estrellas,
                    Comentario = c.Comentario
                })
                .ToListAsync();

            return ResultadoOperacion<DetalleTransaccionDto>.Ok(new DetalleTransaccionDto
            {
                Transaccion = transaccion,
                TelefonoTransportador = viaje.TelefonoTransportador,
                Placa = viaje.Placa,
                Calificaciones = calificaciones
            });
        }

        /// <summary>Pestaña "Historial de Ventas General" y "Logs del Sistema" del historial global (CU-030).</summary>
        public async Task<HistorialGlobalDto> ObtenerGlobalAsync(FiltroHistorialGlobalDto filtro)
        {
            var resultado = new HistorialGlobalDto { Filtro = filtro };
            var desde = filtro.Desde?.Date;
            var hasta = filtro.Hasta?.Date.AddDays(1); // incluye todo el día final

            if (filtro.Pestana == "logs")
            {
                var logs = _db.LogSistemas.AsNoTracking().AsQueryable();
                if (filtro.IdUsuario != null) logs = logs.Where(l => l.IdUsuario == filtro.IdUsuario);
                if (desde != null) logs = logs.Where(l => l.Fecha >= desde);
                if (hasta != null) logs = logs.Where(l => l.Fecha < hasta);

                var filas = await logs
                    .OrderByDescending(l => l.Fecha)
                    .ThenByDescending(l => l.IdLog)
                    .Take(MaximoFilas)
                    .ToListAsync();

                var idsUsuario = filas.Where(l => l.IdUsuario != null).Select(l => l.IdUsuario!.Value).Distinct().ToList();
                var nombres = await _db.Usuarios
                    .AsNoTracking()
                    .Where(u => idsUsuario.Contains(u.IdUsuario))
                    .ToDictionaryAsync(u => u.IdUsuario, u => u.NombreCom + " " + u.ApellidoCom);

                resultado.Logs = filas.Select(l => new LogVistaDto
                {
                    Fecha = l.Fecha,
                    Nivel = l.Nivel,
                    Origen = l.Origen,
                    Mensaje = l.Mensaje,
                    Usuario = l.IdUsuario != null && nombres.TryGetValue(l.IdUsuario.Value, out var n) ? n : "Sistema"
                }).ToList();
                return resultado;
            }

            var ventas = FinalizadosDe(_db.Demanda);
            if (filtro.IdUsuario != null)
            {
                // Con usuario indicado, el rol acota en qué papel participó; sin rol, en cualquiera.
                var id = filtro.IdUsuario;
                ventas = RolUsuarioExtensions.Desde(filtro.Rol) switch
                {
                    RolUsuario.Comprador => ventas.Where(x => x.Pedido.IdUsuario == id),
                    RolUsuario.Campesino => ventas.Where(x => x.Pedido.AporteCamps.Any(a => a.IdUsuario == id)),
                    RolUsuario.Transportador => ventas.Where(x => x.Viaje.IdUsuarioTransportador == id),
                    _ => ventas.Where(x => x.Pedido.IdUsuario == id
                                           || x.Viaje.IdUsuarioTransportador == id
                                           || x.Pedido.AporteCamps.Any(a => a.IdUsuario == id))
                };
            }

            if (desde != null) ventas = ventas.Where(x => (x.Viaje.FechaEntrega ?? x.Viaje.FechaLlegada ?? x.Viaje.FechaAsignacion) >= desde);
            if (hasta != null) ventas = ventas.Where(x => (x.Viaje.FechaEntrega ?? x.Viaje.FechaLlegada ?? x.Viaje.FechaAsignacion) < hasta);

            var admin = new UsuarioActual(0, string.Empty, string.Empty, RolUsuario.Administrador);
            resultado.Ventas = await ProyectarAsync(
                ventas.OrderByDescending(x => x.Viaje.FechaEntrega ?? x.Viaje.FechaLlegada ?? x.Viaje.FechaAsignacion).Take(MaximoFilas),
                admin);
            return resultado;
        }

        private IQueryable<PedidoViaje> FinalizadosDe(IQueryable<Demandum> pedidos)
        {
            return from d in pedidos
                   join v in _db.Viajes on d.IdDemanda equals v.IdDemanda
                   where EstadosEnvio.Finalizados.Contains(v.EstadoEnvio)
                   select new PedidoViaje { Pedido = d, Viaje = v };
        }

        private async Task<List<TransaccionDto>> ProyectarAsync(IQueryable<PedidoViaje> consulta, UsuarioActual usuario)
        {
            var filas = await consulta
                .AsNoTracking()
                .Select(x => new
                {
                    x.Pedido.IdDemanda,
                    Producto = x.Pedido.IdTipoProductoNavigation.Producto + " " + x.Pedido.IdVariedadNavigation.VariedadProd,
                    x.Pedido.CantidadDeman,
                    x.Pedido.PrecioDeman,
                    Fecha = x.Viaje.FechaEntrega ?? x.Viaje.FechaLlegada ?? x.Viaje.FechaAsignacion,
                    Estado = x.Viaje.EstadoEnvio,
                    Comprador = x.Pedido.IdUsuarioNavigation.NombreCom + " " + x.Pedido.IdUsuarioNavigation.ApellidoCom,
                    Transportador = x.Viaje.NombreTransportador,
                    CantidadAportada = x.Pedido.AporteCamps.Where(a => a.IdUsuario == usuario.Id).Sum(a => (int?)a.CantAporte) ?? 0
                })
                .ToListAsync();

            var ids = filas.Select(f => f.IdDemanda).ToList();
            var campesinos = await _db.AporteCamps
                .AsNoTracking()
                .Where(a => ids.Contains(a.IdDemanda))
                .Select(a => new { a.IdDemanda, Nombre = a.IdUsuarioNavigation.NombreCom + " " + a.IdUsuarioNavigation.ApellidoCom })
                .Distinct()
                .ToListAsync();

            return filas.Select(f =>
            {
                // El campesino ve solo lo que él aportó; comprador y administrador, el pedido completo.
                var esVentaCampesino = usuario.Rol == RolUsuario.Campesino;
                var cantidad = esVentaCampesino ? f.CantidadAportada : f.CantidadDeman;
                return new TransaccionDto
                {
                    IdDemanda = f.IdDemanda,
                    Producto = f.Producto,
                    Cantidad = cantidad,
                    Valor = Math.Round((decimal)f.PrecioDeman * cantidad, 2),
                    Fecha = f.Fecha,
                    Estado = f.Estado,
                    Comprador = f.Comprador,
                    Transportador = f.Transportador,
                    Vendedores = string.Join(", ", campesinos.Where(c => c.IdDemanda == f.IdDemanda).Select(c => c.Nombre))
                };
            }).ToList();
        }

        /// <summary>Par pedido-viaje para las consultas del historial.</summary>
        public class PedidoViaje
        {
            public Demandum Pedido { get; set; } = null!;

            public Viaje Viaje { get; set; } = null!;
        }
    }
}
