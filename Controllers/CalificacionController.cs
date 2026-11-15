using CampesinApp.Dtos;
using CampesinApp.Services;
using CampesinApp.Repositories.Models;
using Microsoft.AspNetCore.Mvc;

namespace CampesinApp.Controllers
{
    /// <summary>Observaciones y calificación (CU-026).</summary>
    [RolesPermitidos(RolUsuario.Campesino, RolUsuario.Comprador, RolUsuario.Transportador)]
    public class CalificacionController : AppController
    {
        private readonly CalificacionService _calificaciones;

        public CalificacionController(IUsuarioActualService usuarioActual, CalificacionService calificaciones) : base(usuarioActual)
        {
            _calificaciones = calificaciones;
        }

        [HttpGet]
        public async Task<IActionResult> Formulario(int idDemanda)
        {
            var usuario = (await ObtenerUsuarioAsync())!;
            var resultado = await _calificaciones.ObtenerFormularioAsync(usuario, idDemanda);
            return resultado.Exitoso ? ModalSuperpuesto("_CalificacionForm", resultado.Datos) : AvisoDeError(resultado);
        }

        /// <summary>"Ver Observaciones": calificación y comentarios ya registrados del pedido.</summary>
        [HttpGet]
        public async Task<IActionResult> Ver(int idDemanda)
        {
            var usuario = (await ObtenerUsuarioAsync())!;
            var resultado = await _calificaciones.ObtenerObservacionesAsync(usuario, idDemanda);
            return resultado.Exitoso ? ModalSuperpuesto("_Observaciones", resultado.Datos) : AvisoDeError(resultado);
        }

        [HttpPost]
        public async Task<IActionResult> Enviar(FormularioCalificacionDto dto)
        {
            var usuario = (await ObtenerUsuarioAsync())!;

            // FE-01-CU026: la ventana se mantiene bloqueada hasta que estrellas (comprador) y comentario estén completos.
            var requiereEstrellas = usuario.Rol == RolUsuario.Comprador;
            if (!requiereEstrellas)
            {
                ModelState.Remove(nameof(dto.Estrellas));
            }

            if (!ModelState.IsValid)
            {
                var primerError = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .FirstOrDefault(m => !string.IsNullOrWhiteSpace(m))
                    ?? "Debe seleccionar las estrellas y completar el comentario obligatorio para continuar";

                return await ReMostrarFormularioAsync(usuario, dto, primerError);
            }

            if (string.IsNullOrWhiteSpace(dto.Comentario) || (requiereEstrellas && dto.Estrellas is < 1 or > 5))
            {
                return await ReMostrarFormularioAsync(usuario, dto, "Debe seleccionar las estrellas y completar el comentario obligatorio para continuar");
            }

            var resultado = await _calificaciones.EnviarAsync(usuario, dto);
            if (resultado.Exitoso)
            {
                return Aviso(resultado.Mensaje!, TipoAviso.Exito, textoBoton: "Aceptar", recargarPrincipalUrl: "/Pedidos/Lista");
            }

            return resultado.Error == TipoError.Validacion
                ? await ReMostrarFormularioAsync(usuario, dto, resultado.Mensaje!)
                : AvisoDeError(resultado, recargarPrincipalUrl: "/Pedidos/Lista");
        }

        private async Task<IActionResult> ReMostrarFormularioAsync(UsuarioActual usuario, FormularioCalificacionDto dto, string mensaje)
        {
            var formulario = await _calificaciones.ObtenerFormularioAsync(usuario, dto.IdDemanda ?? 0, dto.PorDesacuerdo ?? false);
            if (!formulario.Exitoso)
            {
                return AvisoDeError(formulario);
            }

            formulario.Datos!.Estrellas = dto.Estrellas;
            formulario.Datos.Comentario = dto.Comentario;
            ViewData["Error"] = mensaje;
            return ModalSuperpuesto("_CalificacionForm", formulario.Datos);
        }
    }
}
