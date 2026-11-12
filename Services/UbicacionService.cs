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

        /// <summary>Fechas y puntos que definen un viaje; el transportador no los escribe.</summary>
        private sealed record DatosViaje(DateTime Inicio, DateTime Final, string? Origen, string? Destino);

        /// <summary>
        /// Calcula, con lo que ya está en la base de datos, cuándo empezó el viaje, cuándo debe terminar y de dónde a dónde va:
        ///  - fecha inicial: la del primer reporte del viaje; antes del primero, el día en que se asignó el transporte
        ///    (aviso o bitácora de la asignación) y, si no hay registro, hoy;
        ///  - fecha final: la fecha de entrega propuesta en el pedido;
        ///  - origen: el del primer reporte o el punto de acopio donde el campesino aporta (el de mayor aporte);
        ///  - destino: el del primer reporte, el punto de acopio de entrega del pedido o, si no tiene, su dirección.
        /// </summary>
        private async Task<Dictionary<int, DatosViaje>> ObtenerDatosViajesAsync(IReadOnlyCollection<int> idsDemanda)
        {
            var resultado = new Dictionary<int, DatosViaje>();
            if (idsDemanda.Count == 0) return resultado;

            var demandas = await _db.Demanda.AsNoTracking()
                .Where(d => idsDemanda.Contains(d.IdDemanda))
                .Select(d => new
                {
                    d.IdDemanda,
                    d.FechaEntrega,
                    d.DireccionInicial,
                    PuntoEntrega = d.IdPuntoAcopioNavigation != null ? d.IdPuntoAcopioNavigation.NombrePunto : null
                })
                .ToListAsync();

            var primeros = (await _db.ReporteTransportadors.AsNoTracking()
                    .Where(r => idsDemanda.Contains(r.IdDemanda))
                    .OrderBy(r => r.FechaActual).ThenBy(r => r.IdReporte)
                    .ToListAsync())
                .GroupBy(r => r.IdDemanda)
                .ToDictionary(g => g.Key, g => g.First());

            var puntosAporte = (await _db.AporteCamps.AsNoTracking()
                    .Where(a => idsDemanda.Contains(a.IdDemanda) && a.IdPuntoAcopio != null)
                    .OrderByDescending(a => a.CantAporte).ThenBy(a => a.IdAporte)
                    .Select(a => new { a.IdDemanda, Punto = a.IdPuntoAcopioNavigation!.NombrePunto })
                    .ToListAsync())
                .GroupBy(a => a.IdDemanda)
                .ToDictionary(g => g.Key, g => g.First().Punto);

            var asignaciones = (await _db.Notificacions.AsNoTracking()
                    .Where(n => n.Tipo == TiposNotificacion.Asignacion && n.IdDemanda != null && idsDemanda.Contains(n.IdDemanda.Value))
                    .Select(n => new { IdDemanda = n.IdDemanda!.Value, n.Fecha })
                    .ToListAsync())
                .GroupBy(n => n.IdDemanda)
                .ToDictionary(g => g.Key, g => g.Min(n => n.Fecha));

            foreach (var d in demandas)
            {
                primeros.TryGetValue(d.IdDemanda, out var primero);

                DateTime? asignado = asignaciones.TryGetValue(d.IdDemanda, out var fechaAviso) ? fechaAviso : null;
                if (primero == null && asignado == null)
                {
                    // Asignaciones anteriores a los avisos: se busca el registro de la bitácora.
                    var codigo = CodigoPedido.Formatear(d.IdDemanda);
                    asignado = await _db.LogSistemas.AsNoTracking()
                        .Where(l => l.Origen == "CU-024 Asignación transporte" && l.Mensaje.Contains(codigo))
                        .OrderBy(l => l.Fecha)
                        .Select(l => (DateTime?)l.Fecha)
                        .FirstOrDefaultAsync();
                }

                var inicio = primero?.FechaInicio.Date ?? asignado?.Date ?? DateTime.Today;
                var fin = d.FechaEntrega.Date;

                var origen = !string.IsNullOrWhiteSpace(primero?.PuntoOrigen)
                    ? primero!.PuntoOrigen
                    : puntosAporte.GetValueOrDefault(d.IdDemanda);
                var destino = !string.IsNullOrWhiteSpace(primero?.PuntoDestino)
                    ? primero!.PuntoDestino
                    : d.PuntoEntrega ?? d.DireccionInicial;

                resultado[d.IdDemanda] = new DatosViaje(inicio, fin, origen, destino);
            }

            return resultado;
        }

        /// <summary>Viajes sobre los que el usuario puede reportar: los suyos (transportador) o todos los activos (administrador).</summary>
        public async Task<List<ViajeOpcionDto>> ObtenerViajesActivosAsync(UsuarioActual usuario)
        {
            var consulta = _db.Viajes().AsNoTracking().Where(v => !EstadosEnvio.Finalizados.Contains(v.EstadoEnvio));
            if (!usuario.EsAdmin)
            {
                // Solo el viaje en curso: un transportador no puede tener más de uno a la vez.
                var enCurso = await _db.ViajeEnCursoAsync(usuario.Id);
                consulta = consulta.Where(v => v.IdUsuarioTransportador == usuario.Id && v.VisibleTransportador && v.IdViaje == enCurso);
            }

            var filas = await (
                    from v in consulta
                    join d in _db.Demanda on v.IdDemanda equals d.IdDemanda
                    orderby v.IdViaje
                    select new
                    {
                        v.IdViaje,
                        v.IdDemanda,
                        Producto = d.IdTipoProductoNavigation.Producto + " " + d.IdVariedadNavigation.VariedadProd,
                        DireccionFinal = d.IdPuntoAcopioNavigation != null ? d.IdPuntoAcopioNavigation.Direccion : (d.DireccionInicial ?? "Punto de Acopio"),
                        v.Placa
                    })
                .ToListAsync();

            var demandas = filas.Select(f => f.IdDemanda).Distinct().ToList();
            var datos = await ObtenerDatosViajesAsync(demandas);
            var conReportes = (await _db.ReporteTransportadors.AsNoTracking()
                    .Where(r => demandas.Contains(r.IdDemanda))
                    .Select(r => r.IdDemanda)
                    .Distinct()
                    .ToListAsync())
                .ToHashSet();

            var hoy = DateTime.Today;
            return filas.Select(f =>
            {
                datos.TryGetValue(f.IdDemanda, out var d);
                var final = d?.Final ?? hoy;
                return new ViajeOpcionDto
                {
                    IdViaje = f.IdViaje,
                    Texto = $"{CodigoPedido.Formatear(f.IdDemanda)} · {f.Producto} · {f.Placa} → {f.DireccionFinal}",
                    Origen = d?.Origen,
                    Destino = d?.Destino,
                    TieneReportes = conReportes.Contains(f.IdDemanda),
                    FechaInicio = d?.Inicio ?? hoy,
                    FechaFinal = final,
                    Retrasado = hoy > final,
                    DiasRetraso = hoy > final ? (hoy - final).Days : 0
                };
            }).ToList();
        }

        /// <summary>
        /// Registra un reporte de ubicación (CU-021), avisa al comprador y a los campesinos del pedido (RN03-CU021)
        /// y, si es el hito de llegada, avisa al comprador para que verifique la mercancía (CU-025).
        /// </summary>
        public async Task<ResultadoOperacion<ReporteVistaDto>> RegistrarAsync(UsuarioActual usuario, ReporteUbicacionDto dto, bool notificarLlegada)
        {
            var viaje = await _db.Viajes().AsNoTracking().FirstOrDefaultAsync(v => v.IdViaje == dto.IdViaje);
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

            if (!usuario.EsAdmin)
            {
                var enCurso = await _db.ViajeEnCursoAsync(usuario.Id);
                if (enCurso != null && enCurso != viaje.IdViaje)
                {
                    return ResultadoOperacion<ReporteVistaDto>.Falla(TipoError.Conflicto,
                        $"Ya tiene un viaje en curso ({CodigoPedido.Formatear(enCurso.Value)}). Termínelo antes de reportar otro.");
                }
            }

            if (viaje.LlegadaNotificada)
            {
                return ResultadoOperacion<ReporteVistaDto>.Falla(TipoError.Conflicto, "La llegada de este viaje ya fue notificada.");
            }

            var hito = notificarLlegada ? Hitos.DestinoFinal : dto.Hito!;
            if (!Hitos.Todos.Contains(hito))
            {
                return ResultadoOperacion<ReporteVistaDto>.Falla(TipoError.Validacion, "El tipo de reporte seleccionado no es válido.");
            }

            // RN01-CU021: el primer registro del servicio es la posición de arranque; su origen y destino se reutilizan después.
            var primero = await _db.ReporteTransportadors.AsNoTracking()
                .Where(r => r.IdDemanda == viaje.IdDemanda)
                .OrderBy(r => r.FechaActual).ThenBy(r => r.IdReporte)
                .FirstOrDefaultAsync();

            // Fechas, origen y destino salen del viaje, no del formulario: el transportador no los cambia.
            var datosViaje = (await ObtenerDatosViajesAsync(new[] { viaje.IdDemanda })).GetValueOrDefault(viaje.IdDemanda);
            if (datosViaje == null)
            {
                return ResultadoOperacion<ReporteVistaDto>.Falla(TipoError.NoEncontrado, "No se encontró el pedido del viaje.");
            }

            dto.FechaInicial = datosViaje.Inicio;
            dto.FechaFinal = datosViaje.Final;
            if (!string.IsNullOrWhiteSpace(datosViaje.Origen)) dto.PuntoOrigen = datosViaje.Origen;
            if (!string.IsNullOrWhiteSpace(datosViaje.Destino)) dto.PuntoDestino = datosViaje.Destino;
            if (string.IsNullOrWhiteSpace(dto.PuntoOrigen) || string.IsNullOrWhiteSpace(dto.PuntoDestino))
            {
                return ResultadoOperacion<ReporteVistaDto>.Falla(TipoError.Validacion,
                    "Este viaje no tiene definido su punto de origen o de destino. Avise al administrador para completarlo.");
            }

            // El lugar es un punto de acopio registrado (tabla punto_Acopio) o, solo en el reporte de avance cada 2 horas,
            // "En carretera" con las coordenadas que tomó el navegador.
            string ubicacion;
            double latitud, longitud;
            if (dto.Lugar!.Trim() == Carretera.Nombre)
            {
                var c = System.Globalization.CultureInfo.InvariantCulture;
                if (!double.TryParse(dto.Latitud, System.Globalization.NumberStyles.Float, c, out latitud)
                    || !double.TryParse(dto.Longitud, System.Globalization.NumberStyles.Float, c, out longitud)
                    || latitud is < -90 or > 90 || longitud is < -180 or > 180)
                {
                    return ResultadoOperacion<ReporteVistaDto>.Falla(TipoError.Validacion, "Falta la ubicación en carretera. Permita el acceso a la ubicación en el navegador o toque en el mapa el lugar donde va, e intente de nuevo.");
                }
                ubicacion = Carretera.Formatear(latitud, longitud);
                // En carretera no hay municipio: se conserva el del reporte anterior o el del destino del viaje.
                dto.Region = primero?.Region;
            }
            else
            {
                var punto = await BuscarPuntoPorNombreAsync(dto.Lugar);
                if (punto == null)
                {
                    return ResultadoOperacion<ReporteVistaDto>.Falla(TipoError.Validacion, "El lugar seleccionado no es un punto de acopio registrado.");
                }
                ubicacion = punto.NombrePunto;
                latitud = (double)punto.Latitud;
                longitud = (double)punto.Longitud;
                // La región del reporte es el municipio del punto de acopio donde se encuentra.
                dto.Region = await _db.Municipios.AsNoTracking().Where(m => m.IdMunicipio == punto.IdMunicipio).Select(m => m.Municipio1).FirstOrDefaultAsync();
            }

            if (string.IsNullOrWhiteSpace(dto.Region))
            {
                dto.Region = await _db.PuntoAcopios.AsNoTracking()
                    .Where(p => p.NombrePunto == dto.PuntoDestino!.Trim())
                    .Select(p => p.IdMunicipioNavigation.Municipio1)
                    .FirstOrDefaultAsync()
                    ?? await _db.Municipios.AsNoTracking().OrderBy(m => m.IdMunicipio).Select(m => m.Municipio1).FirstOrDefaultAsync();
            }

            var ahora = DateTime.Now;
            // Tabla reporte_Transportador: ubicacion_Actual = punto de acopio, fecha_Actual = fecha y hora del reporte.
            // El estado del envío (En tránsito) y la llegada se calculan a partir de estos reportes.
            var reporte = new ReporteTransportador
            {
                IdDemanda = viaje.IdDemanda,
                IdUsuario = usuario.Id,
                FechaInicio = dto.FechaInicial!.Value.Date,
                FechaFinal = dto.FechaFinal!.Value.Date,
                Region = dto.Region!,
                PuntoOrigen = dto.PuntoOrigen!.Trim(),
                PuntoDestino = dto.PuntoDestino!.Trim(),
                UbicacionActual = ubicacion,
                Hito = hito,
                FechaActual = ahora
            };

            await using var transaccion = await _db.Database.BeginTransactionAsync();
            _db.ReporteTransportadors.Add(reporte);
            await _db.SaveChangesAsync();

            var participantes = (await _pedidos.ObtenerParticipantesAsync(new[] { viaje.IdDemanda }))[viaje.IdDemanda];
            var codigo = CodigoPedido.Formatear(viaje.IdDemanda);
            var destinatarios = participantes
                .Where(p => p.Rol is RolUsuario.Campesino or RolUsuario.Comprador && p.IdUsuario != usuario.Id)
                .Select(p => p.IdUsuario);

            _notificaciones.Agregar(
                destinatarios,
                TiposNotificacion.Ubicacion,
                $"Nueva actualización de ubicación recibida: {reporte.UbicacionActual} ({hito}) - pedido {codigo}.",
                viaje.IdDemanda,
                reporte.IdReporte);

            if (hito == Hitos.DestinoFinal)
            {
                var compradores = participantes.Where(p => p.Rol == RolUsuario.Comprador).Select(p => p.IdUsuario);
                _notificaciones.Agregar(
                    compradores,
                    TiposNotificacion.Llegada,
                    "Su pedido ha arribado al punto de entrega. Por favor verifique la mercancía.",
                    viaje.IdDemanda,
                    reporte.IdReporte);
            }

            await _db.SaveChangesAsync();
            await transaccion.CommitAsync();

            await _log.RegistrarAsync(NivelesLog.Info, "CU-021 Reporte ubicación", $"Reporte '{hito}' en '{reporte.UbicacionActual}' del pedido {codigo}.", usuario.Id);

            var vista = ConvertirReporte(reporte, codigo, false);
            vista.Latitud = latitud;
            vista.Longitud = longitud;
            return ResultadoOperacion<ReporteVistaDto>.Ok(vista);
        }

        /// <summary>Último reporte del pedido, que es el que muestra "Seguimiento de Pedido" (CU-022).</summary>
        public async Task<ResultadoOperacion<ReporteVistaDto>> ObtenerUltimoReporteAsync(UsuarioActual usuario, int idDemanda)
        {
            if (!await _pedidos.ParticipaAsync(usuario, idDemanda))
            {
                return ResultadoOperacion<ReporteVistaDto>.Falla(TipoError.NoAutorizado, "No tiene permisos para consultar la ubicación de este pedido.");
            }

            var reporte = await _db.ReporteTransportadors
                .AsNoTracking()
                .Where(r => r.IdDemanda == idDemanda)
                .OrderByDescending(r => r.FechaActual)
                .ThenByDescending(r => r.IdReporte)
                .FirstOrDefaultAsync();

            if (reporte == null)
            {
                return ResultadoOperacion<ReporteVistaDto>.Falla(TipoError.NoEncontrado, "La ubicación no se encuentra disponible momentáneamente");
            }

            var guardado = await _db.AlertaGuardada.AnyAsync(a => a.IdReporte == reporte.IdReporte && a.IdUsuario == usuario.Id);
            var dto = ConvertirReporte(reporte, CodigoPedido.Formatear(idDemanda), guardado);

            // Recorrido del pedido para dibujarlo en el mapa de "Seguimiento de Pedido": cada reporte se ubica con las
            // coordenadas de su punto de acopio (tabla punto_Acopio).
            var reportes = await _db.ReporteTransportadors
                .AsNoTracking()
                .Where(r => r.IdDemanda == idDemanda)
                .OrderBy(r => r.FechaActual)
                .ThenBy(r => r.IdReporte)
                .ToListAsync();
            var nombres = reportes.Select(r => r.UbicacionActual).Distinct().ToList();
            var puntos = (await _db.PuntoAcopios.AsNoTracking().Where(p => nombres.Contains(p.NombrePunto)).ToListAsync())
                .GroupBy(p => p.NombrePunto, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            bool TryCoordenadas(string ubicacion, out double lat, out double lng)
            {
                if (puntos.TryGetValue(ubicacion, out var punto))
                {
                    lat = (double)punto.Latitud;
                    lng = (double)punto.Longitud;
                    return true;
                }
                return Carretera.TryLeer(ubicacion, out lat, out lng);
            }

            dto.Recorrido = reportes
                .Where(r => TryCoordenadas(r.UbicacionActual, out _, out _))
                .Select(r =>
                {
                    TryCoordenadas(r.UbicacionActual, out var lat, out var lng);
                    return new PuntoMapaDto
                    {
                    Lat = lat,
                    Lng = lng,
                    Titulo = r.UbicacionActual,
                    Hito = r.Hito ?? string.Empty,
                    Fecha = Formato.FechaHora(r.FechaActual)
                    };
                }).ToList();

            if (TryCoordenadas(reporte.UbicacionActual, out var latActual, out var lngActual))
            {
                dto.Latitud = latActual;
                dto.Longitud = lngActual;
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
            var reporte = await _db.ReporteTransportadors.AsNoTracking().FirstOrDefaultAsync(r => r.IdReporte == idReporte);
            if (reporte == null)
            {
                return ResultadoOperacion.Falla(TipoError.NoEncontrado, "El reporte de ubicación ya no está disponible.");
            }

            if (!await _pedidos.ParticipaAsync(usuario, reporte.IdDemanda))
            {
                return ResultadoOperacion.Falla(TipoError.NoAutorizado, "No tiene permisos para guardar reportes de este pedido.");
            }

            var existe = await _db.AlertaGuardada.AnyAsync(a => a.IdReporte == idReporte && a.IdUsuario == usuario.Id);
            if (!existe)
            {
                _db.AlertaGuardada.Add(new AlertaGuardadum
                {
                    IdReporte = idReporte,
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

            var reportes = await _db.AlertaGuardada
                .AsNoTracking()
                .Where(a => a.IdUsuario == usuario.Id && a.IdReporteNavigation.IdDemanda == idDemanda)
                .OrderByDescending(a => a.IdReporteNavigation.FechaActual)
                .Select(a => a.IdReporteNavigation)
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
            var enCurso = await _db.ViajeEnCursoAsync(usuario.Id);
            var filas = await (
                    from v in _db.Viajes()
                    where v.IdUsuarioTransportador == usuario.Id
                          && v.VisibleTransportador
                          && v.IdViaje == enCurso
                          && v.EstadoEnvio == EstadosEnvio.EnTransito
                          && !v.LlegadaNotificada
                    select new
                    {
                        v.IdViaje,
                        v.IdDemanda,
                        Ultimo = _db.ReporteTransportadors.Where(r => r.IdDemanda == v.IdDemanda).Max(r => (DateTime?)r.FechaActual)
                    })
                .AsNoTracking()
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

        /// <summary>Municipios registrados con su región natural; las coordenadas salen del promedio de sus puntos de acopio.</summary>
        public async Task<List<MunicipioOpcionDto>> ObtenerMunicipiosAsync()
        {
            var municipios = await _db.Municipios.AsNoTracking()
                .Select(m => new { m.IdMunicipio, m.Municipio1, Departamento = m.IdDepartamentoNavigation.Departamento1 })
                .ToListAsync();
            var coordenadas = (await _db.PuntoAcopios.AsNoTracking()
                    .Select(p => new { p.IdMunicipio, p.Latitud, p.Longitud })
                    .ToListAsync())
                .GroupBy(p => p.IdMunicipio)
                .ToDictionary(g => g.Key, g => (Lat: (double)g.Average(p => p.Latitud), Lng: (double)g.Average(p => p.Longitud)));

            return municipios
                .OrderBy(m => m.Municipio1)
                .Select(m => new MunicipioOpcionDto
                {
                    Nombre = m.Municipio1,
                    Departamento = m.Departamento,
                    Region = Regiones.DeDepartamento(m.Departamento),
                    Lat = coordenadas.TryGetValue(m.IdMunicipio, out var c) ? c.Lat : null,
                    Lng = coordenadas.TryGetValue(m.IdMunicipio, out var c2) ? c2.Lng : null
                })
                .ToList();
        }

        private async Task<PuntoAcopio?> BuscarPuntoPorNombreAsync(string? nombre)
        {
            var buscado = nombre?.Trim();
            if (string.IsNullOrEmpty(buscado)) return null;
            return await _db.PuntoAcopios.AsNoTracking().OrderBy(p => p.IdPuntoAcopio).FirstOrDefaultAsync(p => p.NombrePunto == buscado);
        }

        private static ReporteVistaDto ConvertirReporte(ReporteTransportador r, string codigo, bool guardado) => new()
        {
            IdReporteUbicacion = r.IdReporte,
            IdDemanda = r.IdDemanda,
            CodigoPedido = codigo,
            Lugar = r.UbicacionActual,
            Hito = r.Hito ?? string.Empty,
            FechaHora = r.FechaActual,
            Guardado = guardado
        };
    }
}
