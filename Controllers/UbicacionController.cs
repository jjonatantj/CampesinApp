using CampesinApp.Dtos;
using CampesinApp.Services;
using CampesinApp.Repositories.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CampesinApp.Controllers
{
    /// <summary>Reporte de ubicación del transportador (CU-021) y consulta de ubicación de campesino y comprador (CU-022).</summary>
    public class UbicacionController : AppController
    {
        public const string MensajeCamposObligatorios = "Por favor complete todos los campos obligatorios para generar el reporte";
        public const string MensajeRangoFechas = "La fecha de inicio no puede ser posterior a la fecha final del reporte";
        public const string MensajeErrorBd = "Error de conexión con la base de datos. No se pudo guardar ni transmitir el reporte. Por favor, intente nuevamente";

        private readonly UbicacionService _ubicacion;
        private readonly ILogService _log;

        public UbicacionController(IUsuarioActualService usuarioActual, UbicacionService ubicacion, ILogService log) : base(usuarioActual)
        {
            _ubicacion = ubicacion;
            _log = log;
        }

        // ---------------------------------------------------------------- CU-021

        [HttpGet]
        [RolesPermitidos(RolUsuario.Transportador, RolUsuario.Administrador)]
        public async Task<IActionResult> Reportar(int? idViaje)
        {
            var usuario = (await ObtenerUsuarioAsync())!;
            var dto = new ReporteUbicacionDto
            {
                IdViaje = idViaje,
                Viajes = await _ubicacion.ObtenerViajesActivosAsync(usuario),
                Puntos = await _ubicacion.ObtenerPuntosAcopioAsync(),
                Municipios = await _ubicacion.ObtenerMunicipiosAsync()
            };
            if (idViaje == null && dto.Viajes.Count == 1)
            {
                dto.IdViaje = dto.Viajes[0].IdViaje;
            }
            // RN05: con un reporte vencido el transportador no puede salir de esta pantalla hasta registrarlo.
            ViewData["ReporteObligatorio"] = usuario.Rol == RolUsuario.Transportador
                && (await _ubicacion.ObtenerReportesVencidosAsync(usuario)).Count > 0;
            // Pantalla completa (el mapa necesita espacio); el formulario se vuelve a pintar en el panel al enviarlo.
            return View("ReportarPagina", dto);
        }

        /// <summary>
        /// Procesa el formulario "Filtros del Reporte". Con accion=llegada es el botón "Notificar Llegada" (CU-025):
        /// registra el hito Destino Final y avisa al comprador.
        /// </summary>
        [HttpPost]
        [RolesPermitidos(RolUsuario.Transportador, RolUsuario.Administrador)]
        public async Task<IActionResult> Reportar(ReporteUbicacionDto dto, string? accion)
        {
            var usuario = (await ObtenerUsuarioAsync())!;
            var llegada = accion == "llegada";
            dto.Viajes = await _ubicacion.ObtenerViajesActivosAsync(usuario);
            dto.Puntos = await _ubicacion.ObtenerPuntosAcopioAsync();
            dto.Municipios = await _ubicacion.ObtenerMunicipiosAsync();

            if (!ValidarReporte(dto, llegada))
            {
                return PanelPagina("_ReporteForm", dto);
            }

            try
            {
                var resultado = await _ubicacion.RegistrarAsync(usuario, dto, llegada);
                if (!resultado.Exitoso)
                {
                    ViewData["ErrorNegocio"] = resultado.Mensaje;
                    return PanelPagina("_ReporteForm", dto);
                }

                ViewData["Llegada"] = llegada;
                return PanelPagina("_ReporteExito", resultado.Datos);
            }
            catch (Exception ex) when (ex is DbUpdateException or Microsoft.Data.SqlClient.SqlException or TimeoutException or InvalidOperationException)
            {
                // FE-03-CU021: se conservan los datos del formulario y se ofrece "Reintentar".
                await _log.RegistrarAsync(NivelesLog.Error, "CU-021 Reporte ubicación", $"{ex.GetType().Name}: {ex.Message}", usuario.Id);
                ViewData["ErrorBd"] = MensajeErrorBd;
                ViewData["AccionPrevia"] = accion;
                return PanelPagina("_ReporteForm", dto);
            }
        }

        /// <summary>
        /// Valida a mano (en vez de con ModelState) para controlar qué campo se resalta y qué mensaje se muestra:
        /// FE-01-CU021 campos incompletos y FE-02-CU021 rango de fechas inválido.
        /// </summary>
        private bool ValidarReporte(ReporteUbicacionDto dto, bool llegada)
        {
            ModelState.Clear();

            void Falta(string campo) => ModelState.AddModelError(campo, "Campo obligatorio");

            if (dto.IdViaje == null) Falta(nameof(dto.IdViaje));
            if (dto.FechaInicial == null) Falta(nameof(dto.FechaInicial));
            if (dto.FechaFinal == null) Falta(nameof(dto.FechaFinal));
            if (string.IsNullOrWhiteSpace(dto.Region)) Falta(nameof(dto.Region));
            if (string.IsNullOrWhiteSpace(dto.PuntoOrigen)) Falta(nameof(dto.PuntoOrigen));
            if (string.IsNullOrWhiteSpace(dto.PuntoDestino)) Falta(nameof(dto.PuntoDestino));
            if (string.IsNullOrWhiteSpace(dto.Lugar)) Falta(nameof(dto.Lugar));
            if (!llegada && string.IsNullOrWhiteSpace(dto.Hito)) Falta(nameof(dto.Hito));

            if (!ModelState.IsValid)
            {
                ViewData["FaltanCampos"] = true;
                return false;
            }

            if (dto.PuntoOrigen!.Length > 100 || dto.PuntoDestino!.Length > 100 || dto.Lugar!.Length > 100)
            {
                ModelState.AddModelError(nameof(dto.Lugar), "Máximo 100 caracteres");
                ViewData["ErrorNegocio"] = "Los textos del reporte no pueden superar los 100 caracteres.";
                return false;
            }

            // FE-02-CU021 / RN03-CU021
            if (dto.FechaInicial!.Value.Date > dto.FechaFinal!.Value.Date)
            {
                ModelState.AddModelError(nameof(dto.FechaInicial), MensajeRangoFechas);
                ModelState.AddModelError(nameof(dto.FechaFinal), string.Empty);
                ViewData["RangoInvalido"] = true;
                return false;
            }

            return true;
        }

        /// <summary>Ventana obligatoria de RN05-CU021: han pasado 2 horas desde el último reporte.</summary>
        [HttpGet]
        [RolesPermitidos(RolUsuario.Transportador)]
        public async Task<IActionResult> AlertaVencida()
        {
            var usuario = (await ObtenerUsuarioAsync())!;
            var vencidos = await _ubicacion.ObtenerReportesVencidosAsync(usuario);
            if (vencidos.Count == 0)
            {
                return CerrarModales("superpuesta");
            }
            return ModalSuperpuesto("_AlertaReporteVencido", vencidos);
        }

        // ---------------------------------------------------------------- CU-022

        [HttpGet]
        [RolesPermitidos(RolUsuario.Campesino, RolUsuario.Comprador, RolUsuario.Administrador)]
        public async Task<IActionResult> Seguimiento(int idDemanda)
        {
            var usuario = (await ObtenerUsuarioAsync())!;
            var resultado = await _ubicacion.ObtenerUltimoReporteAsync(usuario, idDemanda);
            if (!resultado.Exitoso)
            {
                // FE-01-CU022: aún no hay reportes; se mantiene la Lista de Pedidos debajo.
                return AvisoDeError(resultado);
            }
            return ModalSuperpuesto("_Seguimiento", resultado.Datos);
        }

        [HttpPost]
        [RolesPermitidos(RolUsuario.Campesino, RolUsuario.Comprador, RolUsuario.Administrador)]
        public async Task<IActionResult> GuardarAlerta(int idReporte)
        {
            var usuario = (await ObtenerUsuarioAsync())!;
            var resultado = await _ubicacion.GuardarAlertaAsync(usuario, idReporte);
            return resultado.Exitoso ? CerrarModales("todas") : AvisoDeError(resultado);
        }

        [HttpGet]
        [RolesPermitidos(RolUsuario.Campesino, RolUsuario.Comprador, RolUsuario.Administrador)]
        public async Task<IActionResult> Guardados(int idDemanda)
        {
            var usuario = (await ObtenerUsuarioAsync())!;
            var resultado = await _ubicacion.ListarGuardadasAsync(usuario, idDemanda);
            return resultado.Exitoso ? ModalSuperpuesto("_ReportesGuardados", resultado.Datos) : AvisoDeError(resultado);
        }
    }
}
