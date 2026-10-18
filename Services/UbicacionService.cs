using CampesinApp.Dtos;
using CampesinApp.Repositories.Models;
using Microsoft.EntityFrameworkCore;

namespace CampesinApp.Services
{
    /// <summary>Reporte de ubicación del transportador (CU-021) y consulta de ubicación del campesino/comprador (CU-022).</summary>
    public class UbicacionService
    {
        /// <summary>Tiempo entre reportes de avance exigido por RN02-CU021 / RN05-CU021.</summary>
        public static readonly TimeSpan IntervaloReporte = TimeSpan.FromHours(2);

        private readonly Campesinapp3Context _db;
        private readonly PedidoService _pedidos;
        private readonly NotificacionService _notificaciones;
        private readonly ILogService _log;

        public UbicacionService(Campesinapp3Context db, PedidoService pedidos, NotificacionService notificaciones, ILogService log)
        {
            _db = db;
            _pedidos = pedidos;
            _notificaciones = notificaciones;
            _log = log;
        }

        /// <summary>Viajes sobre los que el usuario puede reportar: los suyos (transportador) o todos los activos (administrador).</summary>
        public async Task<List<ViajeOpcionDto>> ObtenerViajesActivosAsync(UsuarioActual usuario)
        {
            var consulta = _db.Viajes.AsNoTracking().Where(v => !EstadosEnvio.Finalizados.Contains(v.EstadoEnvio));
            if (!usuario.EsAdmin)
            {
                consulta = consulta.Where(v => v.IdUsuarioTransportador == usuario.Id && v.VisibleTransportador);
            }

            var filas = await consulta
                .OrderBy(v => v.IdViaje)
                .Select(v => new
                {
                    v.IdViaje,
                    v.IdDemanda,
                    Producto = v.IdDemandaNavigation.IdTipoProductoNavigation.Producto + " " + v.IdDemandaNavigation.IdVariedadNavigation.VariedadProd,
                    DireccionFinal = v.IdDemandaNavigation.IdPuntoAcopioNavigation != null ? v.IdDemandaNavigation.IdPuntoAcopioNavigation.Direccion : (v.IdDemandaNavigation.DireccionInicial ?? "Punto de Acopio"),
                    v.Placa
                })
                .ToListAsync();

            return filas.Select(f => new ViajeOpcionDto
            {
                IdViaje = f.IdViaje,
                Texto = $"{CodigoPedido.Formatear(f.IdDemanda)} · {f.Producto} · {f.Placa} → {f.DireccionFinal}"
            }).ToList();
        }

        /// <summary>
        /// Registra un reporte de ubicación (CU-021), avisa al comprador y a los campesinos del pedido (RN03-CU021)
        /// y, si es el hito de llegada, avisa al comprador para que verifique la mercancía (CU-025).
        /// </summary>
        public async Task<ResultadoOperacion<ReporteVistaDto>> RegistrarAsync(UsuarioActual usuario, ReporteUbicacionDto dto, bool notificarLlegada)
        {
            var viaje = await _db.Viajes.FirstOrDefaultAsync(v => v.IdViaje == dto.IdViaje);
            if (viaje == null)
            {
                return ResultadoOperacion<ReporteVistaDto>.Falla(TipoError.NoEncontrado, "El viaje seleccionado no existe.");
            }

            if (!usuario.EsAdmin && viaje.IdUsuarioTransportador != usuario.Id)
            {
                return ResultadoOperacion<ReporteVistaDto>.Falla(TipoError.NoAutorizado, "No puede reportar la ubicación de un viaje que no tiene asignado.");
            }

            if (EstadosEnvio.Finalizados.Contains(viaje.EstadoEnvio))
            {
                return ResultadoOperacion<ReporteVistaDto>.Falla(TipoError.Conflicto, "Este viaje ya fue entregado; no admite más reportes.");
            }

            if (viaje.LlegadaNotificada)
            {
                return ResultadoOperacion<ReporteVistaDto>.Falla(TipoError.Conflicto, "La llegada de este viaje ya fue notificada.");
            }

            // El lugar debe ser un punto de acopio registrado (tabla punto_Acopio): de ahí salen las coordenadas del mapa.
            var punto = await BuscarPuntoPorNombreAsync(dto.Lugar);
            if (punto == null)
            {
                return ResultadoOperacion<ReporteVistaDto>.Falla(TipoError.Validacion, "El lugar seleccionado no es un punto de acopio registrado.");
            }

            var hito = notificarLlegada ? Hitos.DestinoFinal : dto.Hito!;
            if (!Hitos.Todos.Contains(hito) || !Regiones.Todas.Contains(dto.Region!))
            {
                return ResultadoOperacion<ReporteVistaDto>.Falla(TipoError.Validacion, "El hito o la región seleccionados no son válidos.");
            }

            // RN01-CU021: el primer registro del servicio es la posición de arranque.
            var tieneReportes = await _db.ReporteUbicacions.AnyAsync(r => r.IdViaje == viaje.IdViaje);
            if (!tieneReportes && hito != Hitos.Salida)
            {
                return ResultadoOperacion<ReporteVistaDto>.Falla(TipoError.Validacion, "El primer reporte del viaje debe ser la salida del punto de origen.");
            }

            var ahora = DateTime.Now;
            var reporte = new ReporteUbicacion
            {
                IdViaje = viaje.IdViaje,
                IdDemanda = viaje.IdDemanda,
                IdUsuario = usuario.Id,
                FechaInicial = dto.FechaInicial!.Value.Date,
                FechaFinal = dto.FechaFinal!.Value.Date,
                Region = dto.Region!,
                PuntoOrigen = dto.PuntoOrigen!.Trim(),
                PuntoDestino = dto.PuntoDestino!.Trim(),
                Lugar = punto.NombrePunto,
                Hito = hito,
                FechaHora = ahora
            };

            await using var transaccion = await _db.Database.BeginTransactionAsync();
            _db.ReporteUbicacions.Add(reporte);

            if (viaje.EstadoEnvio == EstadosEnvio.Asignado)
            {
                viaje.EstadoEnvio = EstadosEnvio.EnTransito;
            }

            if (hito == Hitos.DestinoFinal)
            {
                viaje.LlegadaNotificada = true;
                viaje.FechaLlegada = ahora;
            }

            await _db.SaveChangesAsync();

            var participantes = (await _pedidos.ObtenerParticipantesAsync(new[] { viaje.IdDemanda }))[viaje.IdDemanda];
            var codigo = CodigoPedido.Formatear(viaje.IdDemanda);
            var destinatarios = participantes
                .Where(p => p.Rol is RolUsuario.Campesino or RolUsuario.Comprador && p.IdUsuario != usuario.Id)
                .Select(p => p.IdUsuario);

            _notificaciones.Agregar(
                destinatarios,
                TiposNotificacion.Ubicacion,
                $"Nueva actualización de ubicación recibida: {reporte.Lugar} ({hito}) - pedido {codigo}.",
                viaje.IdDemanda,
                reporte.IdReporteUbicacion);

            if (hito == Hitos.DestinoFinal)
            {
                var compradores = participantes.Where(p => p.Rol == RolUsuario.Comprador).Select(p => p.IdUsuario);
                _notificaciones.Agregar(
                    compradores,
                    TiposNotificacion.Llegada,
                    "Su pedido ha arribado al punto de entrega. Por favor verifique la mercancía.",
                    viaje.IdDemanda,
                    reporte.IdReporteUbicacion);
            }

            await _db.SaveChangesAsync();
            await transaccion.CommitAsync();

            await _log.RegistrarAsync(NivelesLog.Info, "CU-021 Reporte ubicación", $"Reporte '{hito}' en '{reporte.Lugar}' del pedido {codigo}.", usuario.Id);

            var vista = ConvertirReporte(reporte, codigo, false);
            vista.Latitud = (double)punto.Latitud;
            vista.Longitud = (double)punto.Longitud;
            return ResultadoOperacion<ReporteVistaDto>.Ok(vista);
        }

        /// <summary>Último reporte del pedido, que es el que muestra "Seguimiento de Pedido" (CU-022).</summary>
        public async Task<ResultadoOperacion<ReporteVistaDto>> ObtenerUltimoReporteAsync(UsuarioActual usuario, int idDemanda)
        {
            if (!await _pedidos.ParticipaAsync(usuario, idDemanda))
            {
                return ResultadoOperacion<ReporteVistaDto>.Falla(TipoError.NoAutorizado, "No tiene permisos para consultar la ubicación de este pedido.");
            }

            var reporte = await _db.ReporteUbicacions
                .AsNoTracking()
                .Where(r => r.IdDemanda == idDemanda)
                .OrderByDescending(r => r.FechaHora)
                .ThenByDescending(r => r.IdReporteUbicacion)
                .FirstOrDefaultAsync();

            if (reporte == null)
            {
                return ResultadoOperacion<ReporteVistaDto>.Falla(TipoError.NoEncontrado, "La ubicación no se encuentra disponible momentáneamente");
            }

            var guardado = await _db.AlertaGuardadas.AnyAsync(a => a.IdReporteUbicacion == reporte.IdReporteUbicacion && a.IdUsuario == usuario.Id);
            var dto = ConvertirReporte(reporte, CodigoPedido.Formatear(idDemanda), guardado);

            // Recorrido del pedido para dibujarlo en el mapa de "Seguimiento de Pedido": cada reporte se ubica con las
            // coordenadas de su punto de acopio (tabla punto_Acopio).
            var reportes = await _db.ReporteUbicacions
                .AsNoTracking()
                .Where(r => r.IdDemanda == idDemanda)
                .OrderBy(r => r.FechaHora)
                .ThenBy(r => r.IdReporteUbicacion)
                .ToListAsync();
            var nombres = reportes.Select(r => r.Lugar).Distinct().ToList();
            var puntos = (await _db.PuntoAcopios.AsNoTracking().Where(p => nombres.Contains(p.NombrePunto)).ToListAsync())
                .GroupBy(p => p.NombrePunto, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            dto.Recorrido = reportes
                .Where(r => puntos.ContainsKey(r.Lugar))
                .Select(r => new PuntoMapaDto
                {
                    Lat = (double)puntos[r.Lugar].Latitud,
                    Lng = (double)puntos[r.Lugar].Longitud,
                    Titulo = r.Lugar,
                    Hito = r.Hito,
                    Fecha = Formato.FechaHora(r.FechaHora)
                }).ToList();

            if (puntos.TryGetValue(reporte.Lugar, out var actual))
            {
                dto.Latitud = (double)actual.Latitud;
                dto.Longitud = (double)actual.Longitud;
            }

            // Punto de acopio de entrega del pedido (destino), si lo tiene.
            var destino = await _db.Demanda
                .AsNoTracking()
                .Where(d => d.IdDemanda == idDemanda && d.IdPuntoAcopio != null)
                .Select(d => d.IdPuntoAcopioNavigation)
                .FirstOrDefaultAsync();
            if (destino != null)
            {
                dto.Destino = new PuntoMapaDto
                {
                    Lat = (double)destino.Latitud,
                    Lng = (double)destino.Longitud,
                    Titulo = destino.NombrePunto,
                    Hito = "Destino del pedido",
                    Tipo = "destino"
                };
            }

            return ResultadoOperacion<ReporteVistaDto>.Ok(dto);
        }

        /// <summary>"Guardar reporte" (CU-022, RN-03): guarda la alerta en el historial permanente del usuario.</summary>
        public async Task<ResultadoOperacion> GuardarAlertaAsync(UsuarioActual usuario, int idReporte)
        {
            var reporte = await _db.ReporteUbicacions.AsNoTracking().FirstOrDefaultAsync(r => r.IdReporteUbicacion == idReporte);
            if (reporte == null)
            {
                return ResultadoOperacion.Falla(TipoError.NoEncontrado, "El reporte de ubicación ya no está disponible.");
            }

            if (!await _pedidos.ParticipaAsync(usuario, reporte.IdDemanda))
            {
                return ResultadoOperacion.Falla(TipoError.NoAutorizado, "No tiene permisos para guardar reportes de este pedido.");
            }

            var existe = await _db.AlertaGuardadas.AnyAsync(a => a.IdReporteUbicacion == idReporte && a.IdUsuario == usuario.Id);
            if (!existe)
            {
                _db.AlertaGuardadas.Add(new AlertaGuardada
                {
                    IdReporteUbicacion = idReporte,
                    IdUsuario = usuario.Id,
                    FechaGuardado = DateTime.Now
                });
                await _db.SaveChangesAsync();
            }

            return ResultadoOperacion.Ok();
        }

        /// <summary>"Control de Reportes Guardados": historial de alertas que el usuario guardó para el pedido.</summary>
        public async Task<ResultadoOperacion<ReporteGuardadoVistaDto>> ListarGuardadasAsync(UsuarioActual usuario, int idDemanda)
        {
            if (!await _pedidos.ParticipaAsync(usuario, idDemanda))
            {
                return ResultadoOperacion<ReporteGuardadoVistaDto>.Falla(TipoError.NoAutorizado, "No tiene permisos para consultar los reportes de este pedido.");
            }

            var reportes = await _db.AlertaGuardadas
                .AsNoTracking()
                .Where(a => a.IdUsuario == usuario.Id && a.IdReporteUbicacionNavigation.IdDemanda == idDemanda)
                .OrderByDescending(a => a.IdReporteUbicacionNavigation.FechaHora)
                .Select(a => a.IdReporteUbicacionNavigation)
                .ToListAsync();

            var codigo = CodigoPedido.Formatear(idDemanda);
            return ResultadoOperacion<ReporteGuardadoVistaDto>.Ok(new ReporteGuardadoVistaDto
            {
                IdDemanda = idDemanda,
                CodigoPedido = codigo,
                Reportes = reportes.Select(r => ConvertirReporte(r, codigo, true)).ToList()
            });
        }

        /// <summary>
        /// Viajes en tránsito cuyo último reporte tiene 2 horas o más (RN05-CU021): el navegador del transportador
        /// consulta esto periódicamente para mostrar la alerta obligatoria del siguiente informe de avance.
        /// </summary>
        public async Task<List<ReporteVencidoDto>> ObtenerReportesVencidosAsync(UsuarioActual usuario)
        {
            if (usuario.Rol != RolUsuario.Transportador) return new List<ReporteVencidoDto>();

            var limite = DateTime.Now - IntervaloReporte;
            var filas = await _db.Viajes
                .AsNoTracking()
                .Where(v => v.IdUsuarioTransportador == usuario.Id
                            && v.VisibleTransportador
                            && v.EstadoEnvio == EstadosEnvio.EnTransito
                            && !v.LlegadaNotificada)
                .Select(v => new
                {
                    v.IdViaje,
                    v.IdDemanda,
                    Ultimo = v.ReporteUbicacions.Max(r => (DateTime?)r.FechaHora)
                })
                .ToListAsync();

            return filas
                .Where(f => f.Ultimo != null && f.Ultimo <= limite)
                .Select(f => new ReporteVencidoDto { IdViaje = f.IdViaje, CodigoPedido = CodigoPedido.Formatear(f.IdDemanda) })
                .ToList();
        }

        /// <summary>Puntos de acopio registrados (tabla punto_Acopio) con sus coordenadas, para elegir el lugar y dibujarlos en el mapa.</summary>
        public async Task<List<PuntoAcopioOpcionDto>> ObtenerPuntosAcopioAsync()
        {
            var puntos = await _db.PuntoAcopios.AsNoTracking().OrderBy(p => p.NombrePunto).ToListAsync();
            return puntos.Select(p => new PuntoAcopioOpcionDto
            {
                Id = p.IdPuntoAcopio,
                Nombre = p.NombrePunto,
                Direccion = p.Direccion,
                Lat = (double)p.Latitud,
                Lng = (double)p.Longitud
            }).ToList();
        }

        private async Task<PuntoAcopio?> BuscarPuntoPorNombreAsync(string? nombre)
        {
            var buscado = nombre?.Trim();
            if (string.IsNullOrEmpty(buscado)) return null;
            return await _db.PuntoAcopios.AsNoTracking().OrderBy(p => p.IdPuntoAcopio).FirstOrDefaultAsync(p => p.NombrePunto == buscado);
        }

        private static ReporteVistaDto ConvertirReporte(ReporteUbicacion r, string codigo, bool guardado) => new()
        {
            IdReporteUbicacion = r.IdReporteUbicacion,
            IdDemanda = r.IdDemanda,
            CodigoPedido = codigo,
            Lugar = r.Lugar,
            Hito = r.Hito,
            FechaHora = r.FechaHora,
            Guardado = guardado
        };
    }
}
