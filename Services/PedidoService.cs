using CampesinApp.Dtos;
using CampesinApp.Repositories.Models;
using Microsoft.EntityFrameworkCore;

namespace CampesinApp.Services
{
    /// <summary>
    /// Consulta de pedidos. Un pedido es una demanda del comprador: el comprador es quien la publicó, los campesinos
    /// son quienes aportaron (aporte_Camp) y el transportador es el dueño del camión asignado (demanda.id_Camion, CU-024).
    /// </summary>
    public class PedidoService
    {
        private readonly Campesinapp3Context _db;

        public PedidoService(Campesinapp3Context db)
        {
            _db = db;
        }

        /// <summary>Pedidos en los que el usuario figura como participante (el administrador ve todos).</summary>
        public IQueryable<Demandum> PedidosVisibles(UsuarioActual usuario)
        {
            return usuario.Rol switch
            {
                RolUsuario.Administrador => _db.Demanda,
                RolUsuario.Comprador => _db.Demanda.Where(d => d.IdUsuario == usuario.Id),
                RolUsuario.Campesino => _db.Demanda.Where(d => d.AporteCamps.Any(a => a.IdUsuario == usuario.Id)),
                RolUsuario.Transportador => _db.Demanda.Where(d => d.IdCamion != null && _db.Camions.Any(c => c.IdCamion == d.IdCamion && c.IdUsuario == usuario.Id)),
                _ => _db.Demanda.Where(d => false)
            };
        }

        public Task<bool> ParticipaAsync(UsuarioActual usuario, int idDemanda)
        {
            return PedidosVisibles(usuario).AnyAsync(d => d.IdDemanda == idDemanda);
        }

        public async Task<Dictionary<int, List<ParticipantePedido>>> ObtenerParticipantesAsync(IReadOnlyCollection<int> idsDemanda)
        {
            var resultado = idsDemanda.ToDictionary(id => id, _ => new List<ParticipantePedido>());

            var compradores = await _db.Demanda
                .AsNoTracking()
                .Where(d => idsDemanda.Contains(d.IdDemanda))
                .Select(d => new { d.IdDemanda, d.IdUsuario, d.IdUsuarioNavigation.NombreCom, d.IdUsuarioNavigation.ApellidoCom })
                .ToListAsync();
            foreach (var c in compradores)
            {
                resultado[c.IdDemanda].Add(new ParticipantePedido(c.IdUsuario, $"{c.NombreCom} {c.ApellidoCom}".Trim(), RolUsuario.Comprador));
            }

            var campesinos = await _db.AporteCamps
                .AsNoTracking()
                .Where(a => idsDemanda.Contains(a.IdDemanda))
                .Select(a => new { a.IdDemanda, a.IdUsuario, a.IdUsuarioNavigation.NombreCom, a.IdUsuarioNavigation.ApellidoCom })
                .Distinct()
                .ToListAsync();
            foreach (var c in campesinos)
            {
                resultado[c.IdDemanda].Add(new ParticipantePedido(c.IdUsuario, $"{c.NombreCom} {c.ApellidoCom}".Trim(), RolUsuario.Campesino));
            }

            var transportadores = await (
                    from d in _db.Demanda
                    where idsDemanda.Contains(d.IdDemanda) && d.IdCamion != null
                    join c in _db.Camions on d.IdCamion.Value equals c.IdCamion
                    select new { d.IdDemanda, c.IdUsuario, c.IdUsuarioNavigation.NombreCom, c.IdUsuarioNavigation.ApellidoCom })
                .AsNoTracking()
                .ToListAsync();
            foreach (var t in transportadores)
            {
                resultado[t.IdDemanda].Add(new ParticipantePedido(t.IdUsuario, $"{t.NombreCom} {t.ApellidoCom}".Trim(), RolUsuario.Transportador));
            }

            return resultado;
        }

        /// <summary>Lista "Mis Pedidos" (CU-022), con filtro opcional por estado del envío (FA-02-CU023).</summary>
        public async Task<List<PedidoResumenDto>> ListarAsync(UsuarioActual usuario, string? estadoEnvio)
        {
            var filas = await PedidosVisibles(usuario)
                .AsNoTracking()
                .OrderByDescending(d => d.IdDemanda)
                .Select(d => new
                {
                    d.IdDemanda,
                    Producto = d.IdTipoProductoNavigation.Producto + " " + d.IdVariedadNavigation.VariedadProd,
                    d.CantidadDeman,
                    d.PrecioDeman,
                    d.FechaEntrega,
                    DireccionFinal = d.IdPuntoAcopioNavigation != null ? d.IdPuntoAcopioNavigation.Direccion : (d.DireccionInicial ?? "Punto de Acopio"),
                    d.IdUsuario,
                    Comprador = d.IdUsuarioNavigation.NombreCom + " " + d.IdUsuarioNavigation.ApellidoCom
                })
                .ToListAsync();

            var ids = filas.Select(f => f.IdDemanda).ToList();
            var viajes = (await _db.Viajes().AsNoTracking().Where(v => ids.Contains(v.IdDemanda)).ToListAsync())
                .ToDictionary(v => v.IdDemanda);

            var pedidos = filas.Select(x =>
            {
                viajes.TryGetValue(x.IdDemanda, out var v);
                return new PedidoResumenDto
                {
                    IdDemanda = x.IdDemanda,
                    Producto = x.Producto,
                    Cantidad = x.CantidadDeman,
                    PrecioUnitario = (decimal)x.PrecioDeman,
                    FechaEntrega = x.FechaEntrega,
                    DireccionFinal = x.DireccionFinal,
                    EstadoEnvio = v == null ? EstadosEnvio.SinTransporte : v.EstadoEnvio,
                    TieneViaje = v != null,
                    NombreComprador = x.Comprador,
                    PuedeValidarEntrega = usuario.Rol == RolUsuario.Comprador && x.IdUsuario == usuario.Id && v is { LlegadaNotificada: true, CompradorConfirma: false }
                };
            }).ToList();

            if (!string.IsNullOrWhiteSpace(estadoEnvio))
            {
                pedidos = pedidos.Where(p => p.EstadoEnvio == estadoEnvio).ToList();
            }

            await MarcarObservacionesAsync(usuario, pedidos);
            return pedidos;
        }

        /// <summary>
        /// Marca en cada pedido finalizado si el usuario puede calificar (comprador: estrellas y comentario; transportador:
        /// comentario) o solo consultar las observaciones (CU-026). Los datos están en la tabla observacion.
        /// </summary>
        private async Task MarcarObservacionesAsync(UsuarioActual usuario, List<PedidoResumenDto> pedidos)
        {
            if (usuario.Rol == RolUsuario.Administrador) return;

            var candidatos = pedidos.Where(p => EstadosEnvio.Calificables.Contains(p.EstadoEnvio)).ToList();
            if (candidatos.Count == 0) return;

            var ids = candidatos.Select(p => p.IdDemanda).ToList();
            var observaciones = (await _db.Observacions
                    .AsNoTracking()
                    .Where(o => ids.Contains(o.IdDemanda))
                    .OrderBy(o => o.IdObservacion)
                    .ToListAsync())
                .GroupBy(o => o.IdDemanda)
                .ToDictionary(g => g.Key, g => g.Last());

            foreach (var pedido in candidatos)
            {
                observaciones.TryGetValue(pedido.IdDemanda, out var obs);
                pedido.PuedeCalificar = usuario.Rol switch
                {
                    RolUsuario.Comprador => obs == null || obs.CalificacionEstre < 1,
                    RolUsuario.Transportador => obs?.ObserTransportador == null || !obs.ObserTransportador.Contains(MarcasObservacion.Separador),
                    _ => false
                };
                pedido.PuedeVerObservaciones = !pedido.PuedeCalificar;
            }
        }
    }
}
