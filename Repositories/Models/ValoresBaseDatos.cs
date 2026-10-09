// Valores de texto que se guardan en la base de datos (estados, hitos, tipos). Archivo nuevo: no modifica ningún modelo existente.
namespace CampesinApp.Repositories.Models
{
    /// <summary>Estados del envío de un pedido (CU-021, CU-023, CU-024, CU-025).</summary>
    public static class EstadosEnvio
    {
        public const string SinTransporte = "Sin transporte";
        public const string Asignado = "Asignado";
        public const string EnTransito = "En tránsito";
        public const string Entregado = "Entregado";
        public const string EntregadoRecibido = "Entregado y Recibido Correctamente";
        public const string EntregadoConObservaciones = "Entregado con Observaciones";

        public static readonly string[] Todos =
        {
            SinTransporte, Asignado, EnTransito, Entregado, EntregadoRecibido, EntregadoConObservaciones
        };

        /// <summary>Estados en los que el pedido ya salió del flujo de transporte y entra al historial.</summary>
        public static readonly string[] Finalizados = { Entregado, EntregadoRecibido, EntregadoConObservaciones };

        /// <summary>Estados en los que ya se puede calificar a los demás participantes (CU-026).</summary>
        public static readonly string[] Calificables = { EntregadoRecibido, EntregadoConObservaciones };
    }

    /// <summary>Hitos del reporte de ubicación (RN02-CU021).</summary>
    public static class Hitos
    {
        public const string Salida = "Salida";
        public const string Avance = "Avance 2 horas";
        public const string PuntoAcopio = "Punto de Acopio";
        public const string DestinoFinal = "Destino Final";

        public static readonly string[] Todos = { Salida, Avance, PuntoAcopio, DestinoFinal };
    }

    public static class Regiones
    {
        public static readonly string[] Todas = { "Andina", "Caribe", "Pacífica", "Orinoquía", "Amazonía", "Insular" };
    }

    public static class TiposNotificacion
    {
        public const string Ubicacion = "Ubicacion";
        public const string Llegada = "Llegada";
        public const string EntregaCompletada = "EntregaCompletada";
        public const string Novedad = "Novedad";

        /// <summary>Tipos que además de la campana muestran una ventana emergente al destinatario (CU-025).</summary>
        public static readonly string[] ConPopup = { Llegada, EntregaCompletada };
    }

    public static class NivelesLog
    {
        public const string Info = "INFO";
        public const string Advertencia = "WARN";
        public const string Error = "ERROR";
    }
}
