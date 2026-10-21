using Microsoft.EntityFrameworkCore;

namespace CampesinApp.Repositories.Models;

/// <summary>
/// Marcas que se guardan en la tabla observacion para registrar la confirmación de la entrega (CU-025) sin necesitar
/// columnas nuevas: obser_Comprador empieza con [OK] (recibido sin novedad) o [NOV] (con observaciones) y
/// obser_Transportador empieza con "Entrega realizada".
/// </summary>
public static class MarcasObservacion
{
    public const string CompradorSinNovedad = "[OK]";
    public const string CompradorConNovedad = "[NOV]";
    public const string EntregaRealizada = "Entrega realizada";

    /// <summary>Separa la confirmación de la entrega del comentario de calificación (CU-026) dentro de la misma columna.</summary>
    public const string Separador = " | ";
}

/// <summary>
/// Servicio de transporte de un pedido (lo que antes era una tabla "viaje"), calculado con las tablas que ya existen:
/// demanda (id_Camion), camion (placa, modelo, capacidad y transportador), usuario, reporte_Transportador (reportes de
/// ubicación) y observacion (confirmaciones de entrega). No es una tabla: no se guarda nada aquí, es solo una consulta.
/// El identificador del viaje es el de la demanda (un pedido tiene un solo servicio de transporte).
/// </summary>
public class ViajeVista
{
    public int IdViaje { get; set; }
    public int IdDemanda { get; set; }
    public int IdUsuarioTransportador { get; set; }
    public string NombreTransportador { get; set; } = string.Empty;
    public string TelefonoTransportador { get; set; } = string.Empty;
    public string Placa { get; set; } = string.Empty;
    public string Modelo { get; set; } = string.Empty;

    /// <summary>Capacidad de carga en toneladas (camion.capacidad_Cami).</summary>
    public int Capacidad { get; set; }

    public string EstadoEnvio { get; set; } = string.Empty;

    /// <summary>Fecha pactada de entrega del pedido (demanda.fecha_Entrega): orden de respaldo en los historiales.</summary>
    public DateTime FechaAsignacion { get; set; }

    public bool LlegadaNotificada { get; set; }
    public DateTime? FechaLlegada { get; set; }
    public bool TransportadorConfirma { get; set; }
    public bool CompradorConfirma { get; set; }
    public bool CompradorConObservaciones { get; set; }
    public string? ObservacionComprador { get; set; }
    public string? ImagenEvidencia { get; set; }

    /// <summary>true mientras el viaje está activo para el transportador (hasta que marca "Entrega realizada", RN-03-CU024).</summary>
    public bool VisibleTransportador { get; set; }

    public DateTime? FechaEntrega { get; set; }
}

public static class ConsultasTransporte
{
    /// <summary>Pedidos con camión asignado, con su estado de envío derivado de reportes y confirmaciones.</summary>
    public static IQueryable<ViajeVista> Viajes(this Campesinapp3Context db)
    {
        return
            from d in db.Demanda
            where d.IdCamion != null
            join c in db.Camions on d.IdCamion.Value equals c.IdCamion
            let hayReportes = db.ReporteTransportadors.Any(r => r.IdDemanda == d.IdDemanda)
            let llegada = db.ReporteTransportadors.Any(r => r.IdDemanda == d.IdDemanda && r.Hito == Hitos.DestinoFinal)
            let transportador = db.Observacions.Any(o => o.IdDemanda == d.IdDemanda && o.ObserTransportador != null && o.ObserTransportador.StartsWith(MarcasObservacion.EntregaRealizada))
            let compradorOk = db.Observacions.Any(o => o.IdDemanda == d.IdDemanda && o.ObserComprador != null && o.ObserComprador.StartsWith(MarcasObservacion.CompradorSinNovedad))
            let compradorNovedad = db.Observacions.Any(o => o.IdDemanda == d.IdDemanda && o.ObserComprador != null && o.ObserComprador.StartsWith(MarcasObservacion.CompradorConNovedad))
            select new ViajeVista
            {
                IdViaje = d.IdDemanda,
                IdDemanda = d.IdDemanda,
                IdUsuarioTransportador = c.IdUsuario,
                NombreTransportador = c.IdUsuarioNavigation.NombreCom + " " + c.IdUsuarioNavigation.ApellidoCom,
                TelefonoTransportador = c.IdUsuarioNavigation.TelelfonoUsu.ToString(),
                Placa = c.PlacaCami,
                Modelo = c.ModeloCami,
                Capacidad = c.CapacidadCami,
                EstadoEnvio = compradorNovedad ? EstadosEnvio.EntregadoConObservaciones
                    : compradorOk && transportador ? EstadosEnvio.EntregadoRecibido
                    : transportador ? EstadosEnvio.Entregado
                    : hayReportes || llegada ? EstadosEnvio.EnTransito
                    : EstadosEnvio.Asignado,
                FechaAsignacion = d.FechaEntrega,
                LlegadaNotificada = llegada,
                FechaLlegada = db.ReporteTransportadors
                    .Where(r => r.IdDemanda == d.IdDemanda && r.Hito == Hitos.DestinoFinal)
                    .Max(r => (DateTime?)r.FechaActual),
                TransportadorConfirma = transportador,
                CompradorConfirma = compradorOk || compradorNovedad,
                CompradorConObservaciones = compradorNovedad,
                ObservacionComprador = db.Observacions
                    .Where(o => o.IdDemanda == d.IdDemanda && o.ObserComprador != null && o.ObserComprador.StartsWith(MarcasObservacion.CompradorConNovedad))
                    .Select(o => o.ObserComprador)
                    .FirstOrDefault(),
                ImagenEvidencia = db.Observacions
                    .Where(o => o.IdDemanda == d.IdDemanda && o.ImagenComprador != null)
                    .Select(o => o.ImagenComprador)
                    .FirstOrDefault(),
                VisibleTransportador = !transportador,
                FechaEntrega = db.Observacions
                    .Where(o => o.IdDemanda == d.IdDemanda)
                    .Max(o => (DateTime?)o.HoraObser)
            };
    }
}
