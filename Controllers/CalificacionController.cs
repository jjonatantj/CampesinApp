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

        [HttpPost]
        public async Task<IActionResult> Enviar(FormularioCalificacionDto dto)
        {
            var usuario = (await ObtenerUsuarioAsync())!;

            // FE-01-CU026: la ventana se mantiene bloqueada hasta que estrellas y comentario estén completos.
            if (dto.Estrellas is < 1 or > 5 || string.IsNullOrWhiteSpace(dto.Comentario) || dto.IdUsuarioEvaluado == null)
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
            var formulario = await _calificaciones.ObtenerFormularioAsync(usuario, dto.IdDemanda, dto.PorDesacuerdo);
            if (!formulario.Exitoso)
            {
                return AvisoDeError(formulario);
            }

            formulario.Datos!.Estrellas = dto.Estrellas;
            formulario.Datos.Comentario = dto.Comentario;
            formulario.Datos.IdUsuarioEvaluado = dto.IdUsuarioEvaluado;
            ViewData["Error"] = mensaje;
            return ModalSuperpuesto("_CalificacionForm", formulario.Datos);
        }
    }
}
