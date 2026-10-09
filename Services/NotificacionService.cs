using CampesinApp.Dtos;
using CampesinApp.Repositories.Models;
using Microsoft.EntityFrameworkCore;

namespace CampesinApp.Services
{
    public class NotificacionService
    {
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
                    IdReporteUbicacion = idReporte,
                    Tipo = tipo,
                    Mensaje = mensaje.Length <= 300 ? mensaje : mensaje[..300],
                    Fecha = ahora
                });
            }
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

        /// <summary>Contador de la campana y ventanas emergentes pendientes (se consulta cada pocos segundos).</summary>
        public async Task<ResumenNotificacionesDto> ObtenerResumenAsync(int idUsuario)
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

            return new ResumenNotificacionesDto { NoLeidas = noLeidas, Popups = popups };
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

        public Task<List<NotificacionDto>> ListarAsync(int idUsuario, int maximo = 30)
        {
            return _db.Notificacions
                .AsNoTracking()
                .Where(n => n.IdUsuario == idUsuario)
                .OrderByDescending(n => n.Fecha)
                .ThenByDescending(n => n.IdNotificacion)
                .Take(maximo)
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
