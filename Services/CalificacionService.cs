using CampesinApp.Dtos;
using CampesinApp.Repositories.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CampesinApp.Services
{
    /// <summary>
    /// Observaciones y calificación de un pedido entregado (CU-026). Usa la tabla observacion, que tiene una fila por pedido:
    /// calificacion_Estre (estrellas del comprador), obser_Comprador y obser_Transportador (comentarios) e imagen_Comprador.
    /// El comprador califica con estrellas y comentario; el transportador deja su comentario; el campesino consulta.
    /// </summary>
    public class CalificacionService
    {
        private const int LargoMaximoObservacion = 255;

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
        /// Prepara el formulario "Módulo de Observaciones y Calificación". Precondiciones: pedido finalizado y que el usuario
        /// no haya calificado antes esta misma transacción.
        /// </summary>
        public async Task<ResultadoOperacion<FormularioCalificacionDto>> ObtenerFormularioAsync(UsuarioActual usuario, int idDemanda, bool porDesacuerdo = false)
        {
            if (usuario.Rol is not (RolUsuario.Comprador or RolUsuario.Transportador))
            {
                return ResultadoOperacion<FormularioCalificacionDto>.Falla(TipoError.NoAutorizado,
                    usuario.Rol == RolUsuario.Campesino
                        ? "Los productores consultan las observaciones y la calificación del pedido, pero no la registran."
                        : "Su rol no puede calificar pedidos.");
            }

            if (!await _pedidos.ParticipaAsync(usuario, idDemanda))
            {
                return ResultadoOperacion<FormularioCalificacionDto>.Falla(TipoError.NoAutorizado, "No tiene permisos para calificar este pedido.");
            }

            var estado = await _db.Viajes().AsNoTracking().Where(v => v.IdDemanda == idDemanda).Select(v => v.EstadoEnvio).FirstOrDefaultAsync();
            if (estado == null || !EstadosEnvio.Calificables.Contains(estado))
            {
                return ResultadoOperacion<FormularioCalificacionDto>.Falla(TipoError.Conflicto, "El pedido aún no está en estado de finalización; no se puede calificar todavía.");
            }

            var obs = await UltimaObservacionAsync(idDemanda, rastrear: false);
            var yaCalifico = usuario.Rol == RolUsuario.Comprador
                ? obs is { CalificacionEstre: >= 1 }
                : obs?.ObserTransportador != null && obs.ObserTransportador.Contains(MarcasObservacion.Separador);
            if (yaCalifico)
            {
                return ResultadoOperacion<FormularioCalificacionDto>.Falla(TipoError.Conflicto, "Ya calificaste este pedido.");
            }

            return ResultadoOperacion<FormularioCalificacionDto>.Ok(new FormularioCalificacionDto
            {
                IdDemanda = idDemanda,
                CodigoPedido = CodigoPedido.Formatear(idDemanda),
                EsComprador = usuario.Rol == RolUsuario.Comprador,
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

            var comentario = dto.Comentario?.Trim();
            var requiereEstrellas = formulario.Datos!.EsComprador;
            if (string.IsNullOrWhiteSpace(comentario) || (requiereEstrellas && dto.Estrellas is < 1 or > 5))
            {
                return ResultadoOperacion.Falla(TipoError.Validacion, "Debe seleccionar las estrellas y completar el comentario obligatorio para continuar");
            }

            var obs = await UltimaObservacionAsync(dto.IdDemanda, rastrear: true);
            if (obs == null)
            {
                obs = new Observacion { IdDemanda = dto.IdDemanda, CalificacionEstre = 0, HoraObser = DateTime.Now };
                _db.Observacions.Add(obs);
            }

            if (requiereEstrellas)
            {
                obs.CalificacionEstre = dto.Estrellas;
                obs.ObserComprador = Combinar(obs.ObserComprador, comentario);
            }
            else
            {
                obs.ObserTransportador = Combinar(obs.ObserTransportador ?? MarcasObservacion.EntregaRealizada, comentario);
            }

            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return ResultadoOperacion.Falla(TipoError.Conflicto, "No se pudo guardar la calificación. Intenta de nuevo.");
            }

            await _log.RegistrarAsync(NivelesLog.Info, "CU-026 Calificación",
                requiereEstrellas
                    ? $"Calificación de {dto.Estrellas} estrellas del pedido {CodigoPedido.Formatear(dto.IdDemanda)}."
                    : $"Observación del transportador sobre el pedido {CodigoPedido.Formatear(dto.IdDemanda)}.",
                usuario.Id);
            return ResultadoOperacion.Ok("¡Gracias por tu opinión! Calificación guardada con éxito");
        }

        /// <summary>"Ver Observaciones": consulta de la calificación y los comentarios de un pedido finalizado.</summary>
        public async Task<ResultadoOperacion<ObservacionesPedidoDto>> ObtenerObservacionesAsync(UsuarioActual usuario, int idDemanda)
        {
            if (!await _pedidos.ParticipaAsync(usuario, idDemanda))
            {
                return ResultadoOperacion<ObservacionesPedidoDto>.Falla(TipoError.NoAutorizado, "No tiene permisos para consultar las observaciones de este pedido.");
            }

            var obs = await UltimaObservacionAsync(idDemanda, rastrear: false);
            if (obs == null)
            {
                return ResultadoOperacion<ObservacionesPedidoDto>.Falla(TipoError.NoEncontrado, "Este pedido todavía no tiene observaciones registradas.");
            }

            var comprador = ParsearComprador(obs.ObserComprador);
            var transportador = ParsearTransportador(obs.ObserTransportador);
            return ResultadoOperacion<ObservacionesPedidoDto>.Ok(new ObservacionesPedidoDto
            {
                IdDemanda = idDemanda,
                CodigoPedido = CodigoPedido.Formatear(idDemanda),
                Estrellas = obs.CalificacionEstre,
                ComentarioComprador = comprador.Comentario,
                HayNovedad = comprador.EsNovedad,
                DetalleNovedad = comprador.Novedad,
                ComentarioTransportador = transportador,
                Evidencia = string.IsNullOrWhiteSpace(obs.ImagenComprador) ? null : $"/uploads/evidencias/{Path.GetFileName(obs.ImagenComprador)}",
                Fecha = obs.HoraObser
            });
        }

        /// <summary>Reputación del transportador: promedio de las estrellas que recibieron los pedidos que entregó con su camión.</summary>
        public async Task<double?> PromedioTransportadorAsync(int idUsuario)
        {
            return await (
                    from o in _db.Observacions
                    where o.CalificacionEstre > 0
                    join d in _db.Demanda on o.IdDemanda equals d.IdDemanda
                    where d.IdCamion != null && _db.Camions.Any(c => c.IdCamion == d.IdCamion && c.IdUsuario == idUsuario)
                    select (double?)o.CalificacionEstre)
                .AverageAsync();
        }

        /// <summary>Reputación del campesino: promedio de las estrellas de los pedidos en los que aportó producto.</summary>
        public async Task<double?> PromedioCampesinoAsync(int idUsuario)
        {
            return await (
                    from o in _db.Observacions
                    where o.CalificacionEstre > 0 && _db.AporteCamps.Any(a => a.IdDemanda == o.IdDemanda && a.IdUsuario == idUsuario)
                    select (double?)o.CalificacionEstre)
                .AverageAsync();
        }

        // ------------------------------------------------------------------------------------------ apoyo

        private Task<Observacion?> UltimaObservacionAsync(int idDemanda, bool rastrear)
        {
            var consulta = rastrear ? _db.Observacions.AsQueryable() : _db.Observacions.AsNoTracking();
            return consulta.Where(o => o.IdDemanda == idDemanda).OrderByDescending(o => o.IdObservacion).FirstOrDefaultAsync();
        }

        /// <summary>Agrega el comentario de la calificación a la confirmación guardada, sin pasar de los 255 caracteres de la columna.</summary>
        public static string Combinar(string? existente, string comentario)
        {
            var baseTexto = existente ?? string.Empty;
            var espacio = LargoMaximoObservacion - MarcasObservacion.Separador.Length;
            if (baseTexto.Length > espacio - 40)
            {
                baseTexto = baseTexto[..Math.Max(0, espacio - 40)];
            }

            var disponible = LargoMaximoObservacion - baseTexto.Length - MarcasObservacion.Separador.Length;
            var recorte = comentario.Length <= disponible ? comentario : comentario[..Math.Max(0, disponible)];
            return baseTexto + MarcasObservacion.Separador + recorte;
        }

        public static (bool EsNovedad, string? Novedad, string? Comentario) ParsearComprador(string? texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return (false, null, null);

            var esNovedad = texto.StartsWith(MarcasObservacion.CompradorConNovedad, StringComparison.Ordinal);
            var sinMarca = texto.StartsWith(MarcasObservacion.CompradorConNovedad, StringComparison.Ordinal)
                ? texto[MarcasObservacion.CompradorConNovedad.Length..]
                : texto.StartsWith(MarcasObservacion.CompradorSinNovedad, StringComparison.Ordinal)
                    ? texto[MarcasObservacion.CompradorSinNovedad.Length..]
                    : texto;

            var partes = sinMarca.Split(MarcasObservacion.Separador, 2, StringSplitOptions.None);
            var primera = partes[0].Trim();
            var comentario = partes.Length > 1 ? partes[1].Trim() : null;
            return esNovedad ? (true, string.IsNullOrEmpty(primera) ? null : primera, comentario) : (false, null, comentario);
        }

        public static string? ParsearTransportador(string? texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return null;
            var partes = texto.Split(MarcasObservacion.Separador, 2, StringSplitOptions.None);
            return partes.Length > 1 ? partes[1].Trim() : null;
        }
    }
}
