using CampesinApp.Dtos;
using CampesinApp.Services;
using CampesinApp.Repositories.Models;
using Microsoft.AspNetCore.Mvc;

namespace CampesinApp.Controllers
{
    /// <summary>Historial de transacciones del usuario (CU-027) e historial global del aplicativo (CU-030).</summary>
    public class HistorialController : AppController
    {
        public const string MensajeErrorHistorial = "No se pudo cargar la información histórica. Intenta de nuevo más tarde";
        public const string MensajeErrorGlobal = "Error al cargar el historial global. No hay conexión con la base de datos";
        public const string MensajeSoloAdmin = "Acceso denegado. Este módulo de auditoría es exclusivo para administradores";

        private readonly HistorialService _historial;
        private readonly HistorialPdfService _pdf;
        private readonly ILogService _log;

        public HistorialController(IUsuarioActualService usuarioActual, HistorialService historial, HistorialPdfService pdf, ILogService log) : base(usuarioActual)
        {
            _historial = historial;
            _pdf = pdf;
            _log = log;
        }

        /// <summary>Pantalla completa con un mensaje de error y el botón para regresar al perfil (fallos de conexión).</summary>
        private IActionResult ErrorPagina(string mensaje) => View("~/Views/Shared/ErrorPaginaPerfil.cshtml", mensaje);

        // ---------------------------------------------------------------- CU-027

        [HttpGet]
        [RolesPermitidos(RolUsuario.Campesino, RolUsuario.Comprador, RolUsuario.Administrador)]
        public async Task<IActionResult> Transacciones(string? busqueda)
        {
            var usuario = (await ObtenerUsuarioAsync())!;
            try
            {
                var modelo = await _historial.ListarAsync(usuario, busqueda);
                // Pantalla completa; la búsqueda actualiza solo el panel.
                return EsPeticionAjax() ? PanelPagina("_HistorialTransacciones", modelo) : View("TransaccionesPagina", modelo);
            }
            catch (Exception ex)
            {
                // FE-02-CU027
                await _log.RegistrarAsync(NivelesLog.Error, "CU-027 Historial", $"{ex.GetType().Name}: {ex.Message}", usuario.Id);
                return EsPeticionAjax() ? Aviso(MensajeErrorHistorial, TipoAviso.Error, "Cerrar") : ErrorPagina(MensajeErrorHistorial);
            }
        }

        [HttpGet]
        [RolesPermitidos(RolUsuario.Campesino, RolUsuario.Comprador, RolUsuario.Administrador)]
        public async Task<IActionResult> Detalle(int idDemanda)
        {
            var usuario = (await ObtenerUsuarioAsync())!;
            try
            {
                var resultado = await _historial.ObtenerDetalleAsync(usuario, idDemanda);
                return resultado.Exitoso ? ModalSuperpuesto("_DetalleTransaccion", resultado.Datos) : AvisoDeError(resultado);
            }
            catch (Exception ex)
            {
                await _log.RegistrarAsync(NivelesLog.Error, "CU-027 Historial", $"{ex.GetType().Name}: {ex.Message}", usuario.Id);
                return Aviso(MensajeErrorHistorial, TipoAviso.Error, "Cerrar");
            }
        }

        // ---------------------------------------------------------------- CU-030

        /// <summary>FE-01-CU030: cualquier rol distinto de administrador recibe el aviso de acceso denegado.</summary>
        [HttpGet]
        [RolesPermitidos(RolUsuario.Administrador, Mensaje = MensajeSoloAdmin)]
        public async Task<IActionResult> Global([FromQuery] FiltroHistorialGlobalDto filtro)
        {
            var usuario = (await ObtenerUsuarioAsync())!;
            filtro.Pestana = filtro.Pestana == "logs" ? "logs" : "ventas";
            try
            {
                var modelo = await _historial.ObtenerGlobalAsync(filtro);
                // Pantalla completa; las pestañas y los filtros actualizan solo el panel.
                return EsPeticionAjax() ? PanelPagina("_HistorialGlobal", modelo) : View("GlobalPagina", modelo);
            }
            catch (Exception ex)
            {
                // FE-02-CU030
                await _log.RegistrarAsync(NivelesLog.Error, "CU-030 Historial global", $"{ex.GetType().Name}: {ex.Message}", usuario.Id);
                return EsPeticionAjax() ? Aviso(MensajeErrorGlobal, TipoAviso.Error, "Cerrar") : ErrorPagina(MensajeErrorGlobal);
            }
        }

        /// <summary>
        /// Descarga en PDF todo el historial global (ventas y logs) con los filtros aplicados en la ventana (CU-030).
        /// El contenido se arma con los datos actuales de la base de datos en cada descarga.
        /// </summary>
        [HttpGet]
        [RolesPermitidos(RolUsuario.Administrador, Mensaje = MensajeSoloAdmin)]
        public async Task<IActionResult> GlobalPdf([FromQuery] FiltroHistorialGlobalDto filtro)
        {
            var usuario = (await ObtenerUsuarioAsync())!;
            try
            {
                var contenido = await _pdf.GenerarAsync(filtro, $"{usuario.Nombre} ({usuario.Rol})");
                await _log.RegistrarAsync(NivelesLog.Info, "CU-030 Historial global", "Descarga del historial global en PDF.", usuario.Id);
                return File(contenido, "application/pdf", $"historial-global-campesinapp-{DateTime.Now:yyyyMMdd-HHmm}.pdf");
            }
            catch (Exception ex)
            {
                await _log.RegistrarAsync(NivelesLog.Error, "CU-030 Historial global", $"PDF: {ex.GetType().Name}: {ex.Message}", usuario.Id);
                return StatusCode(StatusCodes.Status500InternalServerError, "No se pudo generar el PDF del historial global. Intente de nuevo más tarde.");
            }
        }
    }
}
