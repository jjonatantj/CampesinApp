using CampesinApp.Dtos;
using CampesinApp.Services;
using CampesinApp.Repositories.Models;
using Microsoft.AspNetCore.Mvc;

namespace CampesinApp.Controllers
{
    /// <summary>Validación de entrega y recibido de producto (CU-025).</summary>
    public class EntregaController : AppController
    {
        private readonly EntregaService _entregas;
        private readonly CalificacionService _calificaciones;
        private readonly NotificacionService _notificaciones;

        public EntregaController(
            IUsuarioActualService usuarioActual,
            EntregaService entregas,
            CalificacionService calificaciones,
            NotificacionService notificaciones) : base(usuarioActual)
        {
            _entregas = entregas;
            _calificaciones = calificaciones;
            _notificaciones = notificaciones;
        }

        /// <summary>Ventana con las opciones de recepción del comprador (pasos 3-4).</summary>
        [HttpGet]
        [RolesPermitidos(RolUsuario.Comprador)]
        public async Task<IActionResult> Validar(int idDemanda)
        {
            var usuario = (await ObtenerUsuarioAsync())!;
            var resultado = await _entregas.ObtenerValidacionAsync(usuario, idDemanda);
            if (!resultado.Exitoso)
            {
                return AvisoDeError(resultado);
            }

            if (!resultado.Datos!.LlegadaNotificada)
            {
                return Aviso("El transportador aún no ha notificado la llegada al punto de entrega.", TipoAviso.Informacion);
            }

            if (resultado.Datos.YaRespondio)
            {
                return Aviso("Ya registró la recepción de este pedido.", TipoAviso.Informacion);
            }

            return ModalSuperpuesto("_ValidarEntrega", resultado.Datos);
        }

        [HttpPost]
        [RolesPermitidos(RolUsuario.Comprador)]
        public async Task<IActionResult> Confirmar(ConfirmarRecepcionDto dto)
        {
            var usuario = (await ObtenerUsuarioAsync())!;

            // Validación en el servidor (la del navegador se puede saltar): si los datos no cumplen las reglas del DTO
            // (por ejemplo, observación de más de 500 caracteres), se vuelve a mostrar el formulario con el aviso.
            if (!ModelState.IsValid)
            {
                var formulario = await _entregas.ObtenerValidacionAsync(usuario, dto.IdDemanda);
                if (!formulario.Exitoso)
                {
                    return AvisoDeError(formulario, recargarPrincipalUrl: "/Pedidos/Lista");
                }

                ViewData["Error"] = "Revise los datos: elija una respuesta válida y escriba una observación de máximo 500 caracteres.";
                ViewData["RespuestaPrevia"] = dto.Respuesta;
                ViewData["ObservacionPrevia"] = dto.Observacion;
                return ModalSuperpuesto("_ValidarEntrega", formulario.Datos);
            }

            var resultado = await _entregas.ConfirmarRecepcionAsync(usuario, dto);
            if (!resultado.Exitoso)
            {
                // Errores corregibles (falta la descripción de la novedad, foto inválida) se muestran en el mismo formulario.
                if (resultado.Error == TipoError.Validacion)
                {
                    var validacion = await _entregas.ObtenerValidacionAsync(usuario, dto.IdDemanda);
                    if (validacion.Exitoso)
                    {
                        ViewData["Error"] = resultado.Mensaje;
                        ViewData["RespuestaPrevia"] = dto.Respuesta;
                        ViewData["ObservacionPrevia"] = dto.Observacion;
                        return ModalSuperpuesto("_ValidarEntrega", validacion.Datos);
                    }
                }
                return AvisoDeError(resultado, recargarPrincipalUrl: "/Pedidos/Lista");
            }

            var entrega = resultado.Datos!;

            // Al confirmar la recepción, el aviso "su pedido ha arribado" deja de tener sentido.
            await _notificaciones.MarcarPopupsDeDemandaAsync(usuario.Id, dto.IdDemanda, TiposNotificacion.Llegada);

            // FA-01 / FE-01 de CU-025: con novedad o rechazo, el comprador pasa al módulo de observaciones y calificación.
            if (entrega.ConNovedad)
            {
                var formulario = await _calificaciones.ObtenerFormularioAsync(usuario, dto.IdDemanda, porDesacuerdo: true);
                if (formulario.Exitoso)
                {
                    return ModalSuperpuesto("~/Views/Calificacion/_CalificacionForm.cshtml", formulario.Datos);
                }
            }

            if (entrega.EntregaCompletada)
            {
                await _notificaciones.MarcarPopupsDeDemandaAsync(usuario.Id, dto.IdDemanda, TiposNotificacion.EntregaCompletada);
                return Aviso("¡Entrega completada con éxito y sin novedades!", TipoAviso.Exito, recargarPrincipalUrl: "/Pedidos/Lista");
            }

            return Aviso("Recepción registrada. Falta la confirmación de entrega del transportador para completar el pedido.",
                TipoAviso.Informacion, recargarPrincipalUrl: "/Pedidos/Lista");
        }

        /// <summary>Botón "Entrega realizada" del transportador en "Mis Viajes" (CU-024 paso 7 / CU-025 paso 5).</summary>
        [HttpPost]
        [RolesPermitidos(RolUsuario.Transportador)]
        public async Task<IActionResult> EntregaRealizada(int idViaje)
        {
            var usuario = (await ObtenerUsuarioAsync())!;
            var resultado = await _entregas.RegistrarEntregaTransportadorAsync(usuario, idViaje);
            if (!resultado.Exitoso)
            {
                return AvisoDeError(resultado);
            }

            if (resultado.Datos!.EntregaCompletada)
            {
                await _notificaciones.MarcarPopupsDeDemandaAsync(usuario.Id, resultado.Datos.IdDemanda, TiposNotificacion.EntregaCompletada);
                return Aviso("¡Entrega completada con éxito y sin novedades!", TipoAviso.Exito, recargarPrincipalUrl: "/Transporte/MisViajes");
            }

            var mensaje = resultado.Datos.ConNovedad
                ? "Entrega registrada. El comprador reportó observaciones y el administrador revisará el pedido."
                : "Entrega registrada. Quedó pendiente la confirmación de recibido del comprador.";
            return Aviso(mensaje, TipoAviso.Exito, recargarPrincipalUrl: "/Transporte/MisViajes");
        }
    }
}
