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

            var resultado = await EvaluarAsync(dto.IdDemanda, conNovedad ? obs.ObserComprador : null, RolUsuario.Comprador);
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

            var resultado = await EvaluarAsync(viaje.IdDemanda, null, RolUsuario.Transportador);
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
        private async Task<ResultadoEntregaDto> EvaluarAsync(int idDemanda, string? observacionNovedad, RolUsuario quienConfirmo)
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
                    var novedad = CalificacionService.ParsearComprador(observacionNovedad).Novedad;
                    _notificaciones.Agregar(admins, TiposNotificacion.Novedad,
                        $"Novedad en la entrega del pedido {codigo}: {novedad}", idDemanda);

                    // El transportador y los campesinos del pedido también deben enterarse.
                    var participantes = (await _pedidos.ObtenerParticipantesAsync(new[] { idDemanda }))[idDemanda];
                    _notificaciones.Agregar(
                        participantes.Where(p => p.Rol is RolUsuario.Transportador or RolUsuario.Campesino).Select(p => p.IdUsuario),
                        TiposNotificacion.Novedad,
                        $"El comprador reportó una novedad en la entrega del pedido {codigo}: {novedad}", idDemanda);
                }
            }
            else if (viaje.CompradorConfirma && viaje.TransportadorConfirma)
            {
                resultado.EntregaCompletada = true;
                var comprador = await _db.Demanda.Where(d => d.IdDemanda == idDemanda).Select(d => d.IdUsuario).FirstAsync();
                _notificaciones.Agregar(new[] { comprador, viaje.IdUsuarioTransportador }, TiposNotificacion.EntregaCompletada,
                    "¡Entrega completada con éxito y sin novedades!", idDemanda);

                // =========================================================================
                // LIQUIDACIÓN AUTOMÁTICA DE FONDOS (CU-025 y CU-017)
                // Se distribuye el pago retenido al transportador y a los campesinos según los
                // valores calculados en base de datos (flete_Estimado y precio_Campesino).
                // =========================================================================
                await DistribuirFondosEntregaAsync(idDemanda, viaje.IdUsuarioTransportador);
            }
            else if (quienConfirmo == RolUsuario.Transportador && !viaje.CompradorConfirma)
            {
                // El transportador ya entregó: el comprador debe revisar la mercancía y confirmar.
                var comprador = await _db.Demanda.Where(d => d.IdDemanda == idDemanda).Select(d => d.IdUsuario).FirstAsync();
                _notificaciones.Agregar(new[] { comprador }, TiposNotificacion.EntregaPendiente,
                    $"El transportador confirmó la entrega del pedido {codigo}. Revise la mercancía y valide la recepción en «Mis Pedidos».", idDemanda);
            }
            else if (quienConfirmo == RolUsuario.Comprador && viaje.CompradorConfirma && !viaje.TransportadorConfirma)
            {
                // El comprador ya recibió: falta que el transportador marque «Entrega realizada».
                _notificaciones.Agregar(new[] { viaje.IdUsuarioTransportador }, TiposNotificacion.EntregaPendiente,
                    $"El comprador confirmó la recepción del pedido {codigo}. Marque «Entrega realizada» en «Mis Viajes» para cerrar el pedido.", idDemanda);
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

        /// <summary>
        /// Distribuye el dinero retenido del comprador cuando la entrega finaliza con éxito.
        /// Utiliza el flete calculado en flete_Estimado para el transportador y el precio por kilo
        /// calculado por el trigger trg_CalcularPrecioCampesino (demanda.precio_Campesino) para cada aporte de los campesinos.
        /// </summary>
        private async Task DistribuirFondosEntregaAsync(int idDemanda, int idUsuarioTransportador)
        {
            try
            {
                var demanda = await _db.Demanda
                    .Include(d => d.AporteCamps)
                    .Include(d => d.PagoCompradors)
                    .FirstOrDefaultAsync(d => d.IdDemanda == idDemanda);

                if (demanda == null) return;

                // 1. LIQUIDACIÓN DEL TRANSPORTADOR
                // Verificamos que no se haya pagado ya para evitar pagos duplicados
                bool transportadorYaPagado = await _db.PagoTransportadors.AnyAsync(p => p.IdDemanda == idDemanda);
                if (!transportadorYaPagado)
                {
                    double fleteFinal = demanda.FleteEstimado ?? 150000.0;

                    // Buscamos el perfil de pago del transportador, si no tiene tomamos el primero del sistema
                    var perfilTrans = await _db.PerfilPagos.FirstOrDefaultAsync(p => p.IdUsuario == idUsuarioTransportador);
                    int idPerfilPagoTrans = perfilTrans?.IdPerfilPago 
                        ?? await _db.PerfilPagos.Select(p => p.IdPerfilPago).FirstOrDefaultAsync();

                    if (idPerfilPagoTrans > 0)
                    {
                        var nuevoPagoTrans = new PagoTransportador
                        {
                            IdUsuario = idUsuarioTransportador,
                            IdDemanda = idDemanda,
                            IdPerfilPago = idPerfilPagoTrans,
                            AnticipoTrans = Math.Round(fleteFinal * 0.30, 2),
                            PagoCompleTrans = Math.Round(fleteFinal, 2),
                            FechaAntiTrans = DateTime.Now,
                            FechaPagoTrans = DateTime.Now
                        };
                        _db.PagoTransportadors.Add(nuevoPagoTrans);
                    }
                }

                // 2. LIQUIDACIÓN DE CADA CAMPESINO APORTANTE
                // Usamos directamente precio_Campesino calculado por el trigger en la base de datos
                double precioKiloCampesino = demanda.PrecioCampesino ?? demanda.PrecioDeman;

                foreach (var aporte in demanda.AporteCamps)
                {
                    // Si ya existe consignación registrada para este aporte, no duplicarla
                    bool aporteYaConsignado = await _db.Consignacions.AnyAsync(c => c.IdAporte == aporte.IdAporte);
                    if (aporteYaConsignado) continue;

                    double valorConsignar = Math.Round(aporte.CantAporte * precioKiloCampesino, 2);

                    var perfilCampesino = await _db.PerfilPagos.FirstOrDefaultAsync(p => p.IdUsuario == aporte.IdUsuario);
                    int idPerfilPagoCamp = perfilCampesino?.IdPerfilPago 
                        ?? await _db.PerfilPagos.Select(p => p.IdPerfilPago).FirstOrDefaultAsync();

                    if (idPerfilPagoCamp > 0)
                    {
                        var nuevaConsignacion = new Consignacion
                        {
                            IdUsuario = aporte.IdUsuario,
                            IdAporte = aporte.IdAporte,
                            IdPerfilPago = idPerfilPagoCamp,
                            ConsigCampesino = valorConsignar,
                            FechaConsigCam = DateTime.Now
                        };
                        _db.Consignacions.Add(nuevaConsignacion);
                    }
                }

                // 3. ACTUALIZAR ESTADO DE LA DEMANDA A FINALIZADA (Estado 4: Finalizada / Culminada)
                demanda.IdEstadoDeman = 4;
            }
            catch (Exception ex)
            {
                await _log.RegistrarAsync(NivelesLog.Error, "CU-025 Liquidación Entrega",
                    $"Error al distribuir fondos de la demanda {idDemanda}: {ex.Message}");
            }
        }
    }
}
