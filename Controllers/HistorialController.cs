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
        private readonly ILogService _log;

        public HistorialController(IUsuarioActualService usuarioActual, HistorialService historial, ILogService log) : base(usuarioActual)
        {
            _historial = historial;
            _log = log;
        }

        // ---------------------------------------------------------------- CU-027

        [HttpGet]
        [RolesPermitidos(RolUsuario.Campesino, RolUsuario.Comprador, RolUsuario.Administrador)]
        public async Task<IActionResult> Transacciones(string? busqueda)
        {
            var usuario = (await ObtenerUsuarioAsync())!;
            try
            {
                return ModalPrincipal("_HistorialTransacciones", await _historial.ListarAsync(usuario, busqueda));
            }
            catch (Exception ex)
            {
                // FE-02-CU027
                await _log.RegistrarAsync(NivelesLog.Error, "CU-027 Historial", $"{ex.GetType().Name}: {ex.Message}", usuario.Id);
                return Aviso(MensajeErrorHistorial, TipoAviso.Error, "Cerrar", cerrar: "todas");
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
                return ModalPrincipal("_HistorialGlobal", await _historial.ObtenerGlobalAsync(filtro));
            }
            catch (Exception ex)
            {
                // FE-02-CU030
                await _log.RegistrarAsync(NivelesLog.Error, "CU-030 Historial global", $"{ex.GetType().Name}: {ex.Message}", usuario.Id);
                return Aviso(MensajeErrorGlobal, TipoAviso.Error, "Cerrar", cerrar: "todas");
            }
        }
    }
}
