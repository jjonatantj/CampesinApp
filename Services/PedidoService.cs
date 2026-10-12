using CampesinApp.Dtos;
using CampesinApp.Repositories.Models;
using Microsoft.EntityFrameworkCore;

namespace CampesinApp.Services
{
    /// <summary>
    /// Consulta de pedidos. Un pedido es una demanda del comprador: el comprador es quien la publicó, los campesinos
    /// son quienes aportaron (aporte_Camp) y el transportador es el asignado en el viaje (CU-024).
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
                RolUsuario.Transportador => _db.Demanda.Where(d => _db.Viajes.Any(v => v.IdDemanda == d.IdDemanda && v.IdUsuarioTransportador == usuario.Id)),
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

            var transportadores = await _db.Viajes
                .AsNoTracking()
                .Where(v => idsDemanda.Contains(v.IdDemanda))
                .Select(v => new { v.IdDemanda, v.IdUsuarioTransportador, v.NombreTransportador })
                .ToListAsync();
            foreach (var t in transportadores)
            {
                resultado[t.IdDemanda].Add(new ParticipantePedido(t.IdUsuarioTransportador, t.NombreTransportador, RolUsuario.Transportador));
            }

            return resultado;
        }

        /// <summary>Lista "Mis Pedidos" (CU-022), con filtro opcional por estado del envío (FA-02-CU023).</summary>
        public async Task<List<PedidoResumenDto>> ListarAsync(UsuarioActual usuario, string? estadoEnvio)
        {
            var consulta =
                from d in PedidosVisibles(usuario)
                join v in _db.Viajes on d.IdDemanda equals v.IdDemanda into viajes
                from v in viajes.DefaultIfEmpty()
                select new
                {
                    d.IdDemanda,
                    Producto = d.IdTipoProductoNavigation.Producto + " " + d.IdVariedadNavigation.VariedadProd,
                    d.CantidadDeman,
                    d.PrecioDeman,
                    d.FechaEntrega,
                    DireccionFinal = d.IdPuntoAcopioNavigation != null ? d.IdPuntoAcopioNavigation.Direccion : (d.DireccionInicial ?? "Punto de Acopio"),
                    d.IdUsuario,
                    Comprador = d.IdUsuarioNavigation.NombreCom + " " + d.IdUsuarioNavigation.ApellidoCom,
                    Estado = v == null ? EstadosEnvio.SinTransporte : v.EstadoEnvio,
                    TieneViaje = v != null,
                    Llegada = v != null && v.LlegadaNotificada,
                    CompradorConfirma = v != null && v.CompradorConfirma
                };

            if (!string.IsNullOrWhiteSpace(estadoEnvio))
            {
                consulta = consulta.Where(x => x.Estado == estadoEnvio);
            }

            var filas = await consulta.AsNoTracking().OrderByDescending(x => x.IdDemanda).ToListAsync();

            var pedidos = filas.Select(x => new PedidoResumenDto
            {
                IdDemanda = x.IdDemanda,
                Producto = x.Producto,
                Cantidad = x.CantidadDeman,
                PrecioUnitario = (decimal)x.PrecioDeman,
                FechaEntrega = x.FechaEntrega,
                DireccionFinal = x.DireccionFinal,
                EstadoEnvio = x.Estado,
                TieneViaje = x.TieneViaje,
                NombreComprador = x.Comprador,
                PuedeValidarEntrega = usuario.Rol == RolUsuario.Comprador && x.IdUsuario == usuario.Id && x.Llegada && !x.CompradorConfirma
            }).ToList();

            await MarcarCalificablesAsync(usuario, pedidos);
            return pedidos;
        }

        private async Task MarcarCalificablesAsync(UsuarioActual usuario, List<PedidoResumenDto> pedidos)
        {
            if (usuario.Rol == RolUsuario.Administrador) return;

            var candidatos = pedidos.Where(p => EstadosEnvio.Calificables.Contains(p.EstadoEnvio)).ToList();
            if (candidatos.Count == 0) return;

            var ids = candidatos.Select(p => p.IdDemanda).ToList();
            var participantes = await ObtenerParticipantesAsync(ids);
            var yaCalificados = await _db.Calificacions
                .AsNoTracking()
                .Where(c => c.IdUsuarioEvalua == usuario.Id && ids.Contains(c.IdDemanda))
                .Select(c => new { c.IdDemanda, c.IdUsuarioEvaluado })
                .ToListAsync();

            foreach (var pedido in candidatos)
            {
                var pendientes = participantes[pedido.IdDemanda]
                    .Where(p => p.IdUsuario != usuario.Id)
                    .Select(p => p.IdUsuario)
                    .Distinct()
                    .Count(id => !yaCalificados.Any(c => c.IdDemanda == pedido.IdDemanda && c.IdUsuarioEvaluado == id));
                pedido.PuedeCalificar = pendientes > 0;
            }
        }
    }
}
