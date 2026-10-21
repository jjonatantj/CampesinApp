using CampesinApp.Dtos;
using CampesinApp.Repositories.Models;
using Microsoft.EntityFrameworkCore;

namespace CampesinApp.Services
{
    /// <summary>
    /// Validación de entrega y recibido (CU-025) y "Entrega realizada" del transportador (CU-024, RN-03).
    /// El pedido solo queda "Entregado y Recibido Correctamente" cuando transportador y comprador confirman sin novedad.
    /// Las confirmaciones se guardan en la tabla observacion (ver <see cref="MarcasObservacion"/>); el estado del envío se
    /// calcula a partir de ellas y de los reportes de ubicación.
    /// </summary>
    public class EntregaService
    {
        private const long TamanoMaximoEvidencia = 5 * 1024 * 1024;
        private const int LargoMaximoNovedad = 200;

        private static readonly Dictionary<string, string[]> ExtensionesPermitidas = new()
        {
            [".jpg"] = new[] { "image/jpeg" },
            [".jpeg"] = new[] { "image/jpeg" },
            [".png"] = new[] { "image/png" },
            [".webp"] = new[] { "image/webp" }
        };

        private readonly Campesinapp3Context _db;
        private readonly PedidoService _pedidos;
        private readonly NotificacionService _notificaciones;
        private readonly ILogService _log;
        private readonly IWebHostEnvironment _entorno;

        public EntregaService(
            Campesinapp3Context db,
            PedidoService pedidos,
            NotificacionService notificaciones,
            ILogService log,
            IWebHostEnvironment entorno)
        {
            _db = db;
            _pedidos = pedidos;
            _notificaciones = notificaciones;
            _log = log;
            _entorno = entorno;
        }

        public async Task<ResultadoOperacion<ValidacionEntregaDto>> ObtenerValidacionAsync(UsuarioActual comprador, int idDemanda)
        {
            var pedido = await _db.Demanda
                .AsNoTracking()
                .Where(d => d.IdDemanda == idDemanda && d.IdUsuario == comprador.Id)
                .Select(d => new
                {
                    d.IdDemanda,
                    Producto = d.IdTipoProductoNavigation.Producto + " " + d.IdVariedadNavigation.VariedadProd,
                    d.CantidadDeman
                })
                .FirstOrDefaultAsync();

            if (pedido == null || comprador.Rol != RolUsuario.Comprador)
            {
                return ResultadoOperacion<ValidacionEntregaDto>.Falla(TipoError.NoAutorizado, "Solo el comprador del pedido puede validar la entrega.");
            }

            var viaje = await _db.Viajes().AsNoTracking().FirstOrDefaultAsync(v => v.IdDemanda == idDemanda);
            if (viaje == null)
            {
                return ResultadoOperacion<ValidacionEntregaDto>.Falla(TipoError.Conflicto, "Este pedido no tiene un servicio de transporte asignado.");
            }

            return ResultadoOperacion<ValidacionEntregaDto>.Ok(new ValidacionEntregaDto
            {
                IdDemanda = idDemanda,
                CodigoPedido = CodigoPedido.Formatear(idDemanda),
                Producto = pedido.Producto,
                Cantidad = pedido.CantidadDeman,
                EstadoEnvio = viaje.EstadoEnvio,
                LlegadaNotificada = viaje.LlegadaNotificada,
                YaRespondio = viaje.CompradorConfirma
            });
        }

        /// <summary>El comprador confirma la recepción (pasos 3-4 de CU-025), con o sin observaciones.</summary>
        public async Task<ResultadoOperacion<ResultadoEntregaDto>> ConfirmarRecepcionAsync(UsuarioActual comprador, ConfirmarRecepcionDto dto)
        {
            var validacion = await ObtenerValidacionAsync(comprador, dto.IdDemanda);
            if (!validacion.Exitoso)
            {
                return ResultadoOperacion<ResultadoEntregaDto>.Falla(validacion.Error, validacion.Mensaje!);
            }

            if (!validacion.Datos!.LlegadaNotificada)
            {
                return ResultadoOperacion<ResultadoEntregaDto>.Falla(TipoError.Conflicto, "El transportador aún no ha notificado la llegada al punto de entrega.");
            }

            if (validacion.Datos.YaRespondio)
            {
                return ResultadoOperacion<ResultadoEntregaDto>.Falla(TipoError.Conflicto, "Ya registró la recepción de este pedido.");
            }

            var conNovedad = dto.Respuesta != RespuestaRecepcion.SinObservaciones;
            var observacion = dto.Observacion?.Trim();
            if (dto.Respuesta == RespuestaRecepcion.ConObservaciones && string.IsNullOrWhiteSpace(observacion))
            {
                return ResultadoOperacion<ResultadoEntregaDto>.Falla(TipoError.Validacion, "Describa la novedad: productos dañados o faltantes.");
            }

            if (observacion is { Length: > LargoMaximoNovedad })
            {
                return ResultadoOperacion<ResultadoEntregaDto>.Falla(TipoError.Validacion, $"La descripción de la novedad no puede superar los {LargoMaximoNovedad} caracteres.");
            }

            string? nombreEvidencia = null;
            if (conNovedad && dto.Evidencia is { Length: > 0 })
            {
                var guardada = await GuardarEvidenciaAsync(dto.Evidencia);
                if (!guardada.Exitoso)
                {
                    return ResultadoOperacion<ResultadoEntregaDto>.Falla(guardada.Error, guardada.Mensaje!);
                }
                nombreEvidencia = guardada.Datos;
            }

            var obs = await ObservacionDelPedidoAsync(dto.IdDemanda);
            obs.HoraObser = DateTime.Now;
            if (conNovedad)
            {
                var detalle = string.IsNullOrWhiteSpace(observacion) ? "El comprador rechazó la recepción de la entrega." : observacion;
                obs.ObserComprador = $"{MarcasObservacion.CompradorConNovedad} {detalle}";
                obs.ImagenComprador = nombreEvidencia;
            }
            else
            {
                obs.ObserComprador = MarcasObservacion.CompradorSinNovedad;
            }

            await _db.SaveChangesAsync();

            var resultado = await EvaluarAsync(dto.IdDemanda, conNovedad ? obs.ObserComprador : null);
            await _log.RegistrarAsync(
                conNovedad ? NivelesLog.Advertencia : NivelesLog.Info,
                "CU-025 Recepción",
                $"Comprador registró recepción '{dto.Respuesta}' del pedido {CodigoPedido.Formatear(dto.IdDemanda)}. Estado: {resultado.EstadoEnvio}.",
                comprador.Id);
            return ResultadoOperacion<ResultadoEntregaDto>.Ok(resultado);
        }

        /// <summary>"Entrega Realizada" del transportador (CU-024 paso 7 / CU-025 paso 5). El idViaje es el id del pedido.</summary>
        public async Task<ResultadoOperacion<ResultadoEntregaDto>> RegistrarEntregaTransportadorAsync(UsuarioActual transportador, int idViaje)
        {
            var viaje = await _db.Viajes().AsNoTracking().FirstOrDefaultAsync(v => v.IdViaje == idViaje && v.IdUsuarioTransportador == transportador.Id);
            if (viaje == null || transportador.Rol != RolUsuario.Transportador)
            {
                return ResultadoOperacion<ResultadoEntregaDto>.Falla(TipoError.NoAutorizado, "No tiene asignado este viaje.");
            }

            // RN-03-CU024: solo puede marcar la entrega cuando está físicamente en el destino (notificó la llegada).
            if (!viaje.LlegadaNotificada)
            {
                return ResultadoOperacion<ResultadoEntregaDto>.Falla(TipoError.Conflicto,
                    "Solo puede marcar la entrega cuando se encuentre en el punto de destino. Notifique la llegada primero.");
            }

            if (viaje.TransportadorConfirma)
            {
                return ResultadoOperacion<ResultadoEntregaDto>.Falla(TipoError.Conflicto, "La entrega de este viaje ya fue registrada.");
            }

            var obs = await ObservacionDelPedidoAsync(viaje.IdDemanda);
            obs.ObserTransportador = MarcasObservacion.EntregaRealizada;
            obs.HoraObser = DateTime.Now;
            await _db.SaveChangesAsync();

            var resultado = await EvaluarAsync(viaje.IdDemanda, null);
            await _log.RegistrarAsync(NivelesLog.Info, "CU-024 Entrega realizada",
                $"Transportador marcó la entrega del pedido {CodigoPedido.Formatear(viaje.IdDemanda)}. Estado: {resultado.EstadoEnvio}.", transportador.Id);
            return ResultadoOperacion<ResultadoEntregaDto>.Ok(resultado);
        }

        /// <summary>Fila de observacion del pedido (una por pedido): la existente o una nueva, ya rastreada por el contexto.</summary>
        private async Task<Observacion> ObservacionDelPedidoAsync(int idDemanda)
        {
            var obs = await _db.Observacions.Where(o => o.IdDemanda == idDemanda).OrderByDescending(o => o.IdObservacion).FirstOrDefaultAsync();
            if (obs == null)
            {
                obs = new Observacion { IdDemanda = idDemanda, CalificacionEstre = 0, HoraObser = DateTime.Now };
                _db.Observacions.Add(obs);
            }

            return obs;
        }

        /// <summary>
        /// Cruza las confirmaciones del transportador y del comprador (RN-01-CU025, RN-02-CU025) a partir de lo ya guardado
        /// y genera las notificaciones. El estado del envío se calcula en la consulta de viajes.
        /// </summary>
        private async Task<ResultadoEntregaDto> EvaluarAsync(int idDemanda, string? observacionNovedad)
        {
            var codigo = CodigoPedido.Formatear(idDemanda);
            var viaje = await _db.Viajes().AsNoTracking().FirstAsync(v => v.IdDemanda == idDemanda);
            var resultado = new ResultadoEntregaDto { IdDemanda = idDemanda, EstadoEnvio = viaje.EstadoEnvio };

            if (viaje.CompradorConfirma && viaje.CompradorConObservaciones)
            {
                resultado.ConNovedad = true;
                // Solo se avisa al administrador cuando el comprador acaba de registrar la novedad.
                if (observacionNovedad != null)
                {
                    var admins = await _notificaciones.ObtenerIdsAdministradoresAsync();
                    _notificaciones.Agregar(admins, TiposNotificacion.Novedad,
                        $"Novedad en la entrega del pedido {codigo}: {CalificacionService.ParsearComprador(observacionNovedad).Novedad}", idDemanda);
                }
            }
            else if (viaje.CompradorConfirma && viaje.TransportadorConfirma)
            {
                resultado.EntregaCompletada = true;
                var comprador = await _db.Demanda.Where(d => d.IdDemanda == idDemanda).Select(d => d.IdUsuario).FirstAsync();
                _notificaciones.Agregar(new[] { comprador, viaje.IdUsuarioTransportador }, TiposNotificacion.EntregaCompletada,
                    "¡Entrega completada con éxito y sin novedades!", idDemanda);
            }

            await _db.SaveChangesAsync();
            return resultado;
        }

        private async Task<ResultadoOperacion<string>> GuardarEvidenciaAsync(IFormFile archivo)
        {
            if (archivo.Length > TamanoMaximoEvidencia)
            {
                return ResultadoOperacion<string>.Falla(TipoError.Validacion, "La fotografía no puede superar los 5 MB.");
            }

            var extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();
            if (!ExtensionesPermitidas.TryGetValue(extension, out var tiposPermitidos)
                || !tiposPermitidos.Contains(archivo.ContentType.ToLowerInvariant()))
            {
                return ResultadoOperacion<string>.Falla(TipoError.Validacion, "La evidencia debe ser una imagen JPG, PNG o WEBP.");
            }

            var carpeta = Path.Combine(_entorno.WebRootPath, "uploads", "evidencias");
            Directory.CreateDirectory(carpeta);

            // El nombre lo genera el servidor: nunca se usa el nombre que envía el usuario (32 caracteres + extensión, cabe en imagen_Comprador).
            var nombre = $"{Guid.NewGuid():N}{extension}";
            await using var destino = File.Create(Path.Combine(carpeta, nombre));
            await archivo.CopyToAsync(destino);
            return ResultadoOperacion<string>.Ok(nombre);
        }
    }
}
