using CampesinApp.Dtos;
using CampesinApp.Repositories.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CampesinApp.Services
{
    /// <summary>Observaciones y calificación entre participantes de un pedido entregado (CU-026).</summary>
    public class CalificacionService
    {
        private readonly Campesinapp3Context _db;
        private readonly PedidoService _pedidos;
        private readonly ILogService _log;
        private readonly SoporteOptions _soporte;

        public CalificacionService(Campesinapp3Context db, PedidoService pedidos, ILogService log, IOptions<SoporteOptions> soporte)
        {
            _db = db;
            _pedidos = pedidos;
            _log = log;
            _soporte = soporte.Value;
        }

        /// <summary>
        /// Prepara el formulario "Módulo de Observaciones y Calificación" con los participantes que el usuario
        /// todavía no ha calificado en este pedido (precondición: no haber calificado antes la misma transacción).
        /// </summary>
        public async Task<ResultadoOperacion<FormularioCalificacionDto>> ObtenerFormularioAsync(UsuarioActual usuario, int idDemanda, bool porDesacuerdo = false)
        {
            if (usuario.Rol is RolUsuario.Administrador or RolUsuario.Ninguno)
            {
                return ResultadoOperacion<FormularioCalificacionDto>.Falla(TipoError.NoAutorizado, "Su rol no puede calificar pedidos.");
            }

            if (!await _pedidos.ParticipaAsync(usuario, idDemanda))
            {
                return ResultadoOperacion<FormularioCalificacionDto>.Falla(TipoError.NoAutorizado, "No tiene permisos para calificar este pedido.");
            }

            var estado = await _db.Viajes.AsNoTracking().Where(v => v.IdDemanda == idDemanda).Select(v => v.EstadoEnvio).FirstOrDefaultAsync();
            if (estado == null || !EstadosEnvio.Calificables.Contains(estado))
            {
                return ResultadoOperacion<FormularioCalificacionDto>.Falla(TipoError.Conflicto, "El pedido aún no está en estado de finalización; no se puede calificar todavía.");
            }

            var participantes = (await _pedidos.ObtenerParticipantesAsync(new[] { idDemanda }))[idDemanda];
            var yaCalificados = await _db.Calificacions
                .AsNoTracking()
                .Where(c => c.IdDemanda == idDemanda && c.IdUsuarioEvalua == usuario.Id)
                .Select(c => c.IdUsuarioEvaluado)
                .ToListAsync();

            var pendientes = participantes
                .Where(p => p.IdUsuario != usuario.Id && !yaCalificados.Contains(p.IdUsuario))
                .GroupBy(p => p.IdUsuario)
                .Select(g => g.First())
                .ToList();

            if (pendientes.Count == 0)
            {
                return ResultadoOperacion<FormularioCalificacionDto>.Falla(TipoError.Conflicto, "Ya calificaste a todos los participantes de este pedido.");
            }

            return ResultadoOperacion<FormularioCalificacionDto>.Ok(new FormularioCalificacionDto
            {
                IdDemanda = idDemanda,
                CodigoPedido = CodigoPedido.Formatear(idDemanda),
                Pendientes = pendientes,
                TelefonosSoporte = _soporte.Telefonos,
                PorDesacuerdo = porDesacuerdo
            });
        }

        public async Task<ResultadoOperacion> EnviarAsync(UsuarioActual usuario, FormularioCalificacionDto dto)
        {
            var formulario = await ObtenerFormularioAsync(usuario, dto.IdDemanda);
            if (!formulario.Exitoso)
            {
                return ResultadoOperacion.Falla(formulario.Error, formulario.Mensaje!);
            }

            if (dto.IdUsuarioEvaluado == null || formulario.Datos!.Pendientes.All(p => p.IdUsuario != dto.IdUsuarioEvaluado))
            {
                return ResultadoOperacion.Falla(TipoError.Validacion, "El usuario seleccionado no puede ser calificado en este pedido.");
            }

            if (dto.Estrellas is < 1 or > 5 || string.IsNullOrWhiteSpace(dto.Comentario))
            {
                return ResultadoOperacion.Falla(TipoError.Validacion, "Debe seleccionar las estrellas y completar el comentario obligatorio para continuar");
            }

            _db.Calificacions.Add(new Calificacion
            {
                IdDemanda = dto.IdDemanda,
                IdUsuarioEvalua = usuario.Id,
                IdUsuarioEvaluado = dto.IdUsuarioEvaluado.Value,
                Estrellas = dto.Estrellas,
                Comentario = dto.Comentario.Trim(),
                Fecha = DateTime.Now
            });

            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return ResultadoOperacion.Falla(TipoError.Conflicto, "Ya calificaste a este participante en este pedido.");
            }

            await _log.RegistrarAsync(NivelesLog.Info, "CU-026 Calificación",
                $"Calificación de {dto.Estrellas} estrellas al usuario {dto.IdUsuarioEvaluado} en el pedido {CodigoPedido.Formatear(dto.IdDemanda)}.", usuario.Id);
            return ResultadoOperacion.Ok("¡Gracias por tu opinión! Calificación guardada con éxito");
        }

        /// <summary>Reputación pública: promedio de estrellas recibidas, o null si aún no tiene calificaciones.</summary>
        public async Task<double?> PromedioAsync(int idUsuario)
        {
            return await _db.Calificacions
                .Where(c => c.IdUsuarioEvaluado == idUsuario)
                .AverageAsync(c => (double?)c.Estrellas);
        }
    }
}
