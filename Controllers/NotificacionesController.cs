using CampesinApp.Dtos;
using CampesinApp.Services;
using Microsoft.AspNetCore.Mvc;

namespace CampesinApp.Controllers
{
    /// <summary>
    /// Notificaciones del usuario. El navegador consulta "Sondeo" cada pocos segundos para actualizar el contador de la
    /// campana, mostrar ventanas emergentes pendientes (llegada del pedido, entrega completada) y la alerta de
    /// reporte de ubicación vencido (RN05-CU021).
    /// </summary>
    public class NotificacionesController : AppController
    {
        private readonly NotificacionService _notificaciones;
        private readonly UbicacionService _ubicacion;

        public NotificacionesController(IUsuarioActualService usuarioActual, NotificacionService notificaciones, UbicacionService ubicacion)
            : base(usuarioActual)
        {
            _notificaciones = notificaciones;
            _ubicacion = ubicacion;
        }

        [HttpGet]
        public async Task<IActionResult> Sondeo(int? ultimo)
        {
            var usuario = await ObtenerUsuarioAsync();
            if (usuario == null)
            {
                return Unauthorized();
            }

            var resumen = await _notificaciones.ObtenerResumenAsync(usuario.Id, ultimo);
            var vencidos = await _ubicacion.ObtenerReportesVencidosAsync(usuario);
            return Json(new
            {
                idUsuario = usuario.Id,
                noLeidas = resumen.NoLeidas,
                hayPopup = resumen.Popups.Count > 0,
                reporteVencido = vencidos.Count > 0,
                ultimoId = resumen.UltimoId,
                // Avisos nuevos para mostrar como tarjeta flotante (el navegador los muestra una sola vez).
                nuevas = resumen.Nuevas.Select(n => new
                {
                    id = n.IdNotificacion,
                    tipo = n.Tipo,
                    mensaje = n.Mensaje,
                    cuando = Formato.Relativo(n.Fecha)
                })
            });
        }

        /// <summary>Ventana emergente de la notificación pendiente más antigua (ej. "Su pedido ha arribado...").</summary>
        [HttpGet]
        public async Task<IActionResult> Popup()
        {
            var usuario = await ObtenerUsuarioAsync();
            if (usuario == null)
            {
                return SinPermiso(null);
            }

            var resumen = await _notificaciones.ObtenerResumenAsync(usuario.Id);
            var pendiente = resumen.Popups.FirstOrDefault();
            return pendiente == null ? CerrarModales("superpuesta") : ModalSuperpuesto("_PopupNotificacion", pendiente);
        }

        [HttpPost]
        public async Task<IActionResult> PopupMostrado(int id)
        {
            var usuario = await ObtenerUsuarioAsync();
            if (usuario == null)
            {
                return SinPermiso(null);
            }

            await _notificaciones.MarcarPopupMostradoAsync(usuario.Id, id);
            return CerrarModales("superpuesta");
        }

        [HttpGet]
        public async Task<IActionResult> Lista(int? pagina)
        {
            var usuario = await ObtenerUsuarioAsync();
            if (usuario == null)
            {
                return SinPermiso(null);
            }

            return ModalPrincipal("_ListaNotificaciones", await _notificaciones.ListarAsync(usuario.Id, pagina));
        }

        /// <summary>Marca una notificación como leída (el botón de cada aviso) y vuelve a pintar la misma página.</summary>
        [HttpPost]
        public async Task<IActionResult> Leer(int id, int? pagina)
        {
            var usuario = await ObtenerUsuarioAsync();
            if (usuario == null)
            {
                return SinPermiso(null);
            }

            await _notificaciones.MarcarLeidaAsync(usuario.Id, id);
            return ModalPrincipal("_ListaNotificaciones", await _notificaciones.ListarAsync(usuario.Id, pagina));
        }

        [HttpPost]
        public async Task<IActionResult> MarcarLeidas(int? pagina)
        {
            var usuario = await ObtenerUsuarioAsync();
            if (usuario == null)
            {
                return SinPermiso(null);
            }

            await _notificaciones.MarcarTodasLeidasAsync(usuario.Id);
            return ModalPrincipal("_ListaNotificaciones", await _notificaciones.ListarAsync(usuario.Id, pagina));
        }
    }
}
