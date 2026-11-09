using CampesinApp.Dtos;
using CampesinApp.Repositories.Models;
using Microsoft.EntityFrameworkCore;

namespace CampesinApp.Services
{
    public class NotificacionService
    {
        public const int NotificacionesPorPagina = 6;

        private readonly Campesinapp3Context _db;

        public NotificacionService(Campesinapp3Context db)
        {
            _db = db;
        }

        /// <summary>Agrega notificaciones al contexto; las guarda quien llama junto con su propia operación.</summary>
        public void Agregar(IEnumerable<int> idsUsuario, string tipo, string mensaje, int? idDemanda = null, int? idReporte = null)
        {
            var ahora = DateTime.Now;
            foreach (var idUsuario in idsUsuario.Distinct())
            {
                _db.Notificacions.Add(new Notificacion
                {
                    IdUsuario = idUsuario,
                    IdDemanda = idDemanda,
                    IdReporte = idReporte,
                    Tipo = tipo,
                    Mensaje = mensaje.Length <= 600 ? mensaje : mensaje[..600],
                    Fecha = ahora
                });
            }
        }

        /// <summary>Agrega y guarda de una vez; para quien no tiene una operación propia que guardar (p. ej. un controlador).</summary>
        public async Task EnviarAsync(IEnumerable<int> idsUsuario, string tipo, string mensaje, int? idDemanda = null, int? idReporte = null)
        {
            Agregar(idsUsuario, tipo, mensaje, idDemanda, idReporte);
            await _db.SaveChangesAsync();
        }

        public async Task<List<int>> ObtenerIdsAdministradoresAsync()
        {
            var admins = await _db.Usuarios
                .Where(u => u.Estado)
                .Select(u => new { u.IdUsuario, u.IdRolNavigation.TipoRol })
                .ToListAsync();

            return admins
                .Where(u => RolUsuarioExtensions.Desde(u.TipoRol) == RolUsuario.Administrador)
                .Select(u => u.IdUsuario)
                .ToList();
        }

        /// <summary>
        /// Lo que el navegador consulta cada pocos segundos: contador de la campana, ventanas emergentes pendientes y los
        /// avisos nuevos desde la última notificación que ya vio (<paramref name="ultimoVisto"/>; null = primera consulta).
        /// </summary>
        public async Task<ResumenNotificacionesDto> ObtenerResumenAsync(int idUsuario, int? ultimoVisto = null)
        {
            var noLeidas = await _db.Notificacions.CountAsync(n => n.IdUsuario == idUsuario && !n.Leida);

            var popups = await _db.Notificacions
                .AsNoTracking()
                .Where(n => n.IdUsuario == idUsuario && !n.PopupMostrado && TiposNotificacion.ConPopup.Contains(n.Tipo))
                .OrderBy(n => n.Fecha)
                .Select(n => new NotificacionDto
                {
                    IdNotificacion = n.IdNotificacion,
                    Tipo = n.Tipo,
                    Mensaje = n.Mensaje,
                    Fecha = n.Fecha,
                    Leida = n.Leida
                })
                .ToListAsync();

            var ultimoId = await _db.Notificacions
                .Where(n => n.IdUsuario == idUsuario)
                .Select(n => (int?)n.IdNotificacion)
                .MaxAsync() ?? 0;

            var nuevas = new List<NotificacionDto>();
            if (ultimoVisto != null && ultimoId > ultimoVisto)
            {
                // Los tipos con ventana emergente ya se muestran como ventana; el resto aparece como tarjeta flotante.
                nuevas = await _db.Notificacions
                    .AsNoTracking()
                    .Where(n => n.IdUsuario == idUsuario && n.IdNotificacion > ultimoVisto && !n.Leida && !TiposNotificacion.ConPopup.Contains(n.Tipo))
                    .OrderBy(n => n.IdNotificacion)
                    .Take(4)
                    .Select(n => new NotificacionDto
                    {
                        IdNotificacion = n.IdNotificacion,
                        Tipo = n.Tipo,
                        Mensaje = n.Mensaje,
                        Fecha = n.Fecha,
                        Leida = n.Leida
                    })
                    .ToListAsync();
            }

            return new ResumenNotificacionesDto { NoLeidas = noLeidas, Popups = popups, Nuevas = nuevas, UltimoId = ultimoId };
        }

        public async Task MarcarPopupMostradoAsync(int idUsuario, int idNotificacion)
        {
            var notificacion = await _db.Notificacions
                .FirstOrDefaultAsync(n => n.IdNotificacion == idNotificacion && n.IdUsuario == idUsuario);
            if (notificacion == null) return;

            notificacion.PopupMostrado = true;
            await _db.SaveChangesAsync();
        }

        /// <summary>Evita repetir la ventana emergente a quien ya vio el resultado en pantalla (p. ej. quien completó la entrega).</summary>
        public async Task MarcarPopupsDeDemandaAsync(int idUsuario, int idDemanda, string tipo)
        {
            var pendientes = await _db.Notificacions
                .Where(n => n.IdUsuario == idUsuario && n.IdDemanda == idDemanda && n.Tipo == tipo && !n.PopupMostrado)
                .ToListAsync();
            foreach (var notificacion in pendientes)
            {
                notificacion.PopupMostrado = true;
            }

            await _db.SaveChangesAsync();
        }

        /// <summary>Una página de la campana, de la más reciente a la más antigua.</summary>
        public async Task<ListaNotificacionesDto> ListarAsync(int idUsuario, int? pagina = null)
        {
            var consulta = _db.Notificacions.AsNoTracking().Where(n => n.IdUsuario == idUsuario);
            var total = await consulta.CountAsync();
            var paginacion = PaginacionDto.Crear(pagina, total, NotificacionesPorPagina, "notificaciones", "/Notificaciones/Lista?pagina={p}", "principal");

            var items = await consulta
                .OrderByDescending(n => n.Fecha)
                .ThenByDescending(n => n.IdNotificacion)
                .Skip((paginacion.Pagina - 1) * NotificacionesPorPagina)
                .Take(NotificacionesPorPagina)
                .Select(n => new NotificacionDto
                {
                    IdNotificacion = n.IdNotificacion,
                    Tipo = n.Tipo,
                    Mensaje = n.Mensaje,
                    Fecha = n.Fecha,
                    Leida = n.Leida
                })
                .ToListAsync();

            return new ListaNotificacionesDto
            {
                Notificaciones = items,
                NoLeidas = await consulta.CountAsync(n => !n.Leida),
                Paginacion = paginacion
            };
        }

        public async Task MarcarLeidaAsync(int idUsuario, int idNotificacion)
        {
            var notificacion = await _db.Notificacions
                .FirstOrDefaultAsync(n => n.IdNotificacion == idNotificacion && n.IdUsuario == idUsuario);
            if (notificacion == null || notificacion.Leida) return;

            notificacion.Leida = true;
            notificacion.PopupMostrado = true;
            await _db.SaveChangesAsync();
        }

        public async Task MarcarTodasLeidasAsync(int idUsuario)
        {
            var sinLeer = await _db.Notificacions
                .Where(n => n.IdUsuario == idUsuario && !n.Leida)
                .ToListAsync();
            foreach (var notificacion in sinLeer)
            {
                notificacion.Leida = true;
                notificacion.PopupMostrado = true;
            }

            await _db.SaveChangesAsync();
        }
    }
}
