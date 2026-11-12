namespace CampesinApp.Dtos
{
    /// <summary>Fila del "Historial de Transacciones" (CU-027) y del historial global de ventas (CU-030).</summary>
    public class TransaccionDto
    {
        public int IdDemanda { get; set; }

        public string Codigo => CodigoPedido.Formatear(IdDemanda);

        public string Producto { get; set; } = string.Empty;

        public int Cantidad { get; set; }

        public decimal Valor { get; set; }

        public DateTime Fecha { get; set; }

        public string Estado { get; set; } = string.Empty;

        public string Comprador { get; set; } = string.Empty;

        public string Vendedores { get; set; } = string.Empty;

        public string Transportador { get; set; } = string.Empty;
    }

    public class HistorialTransaccionesDto
    {
        public string? Busqueda { get; set; }

        public bool EsCompra { get; set; }

        public List<TransaccionDto> Transacciones { get; set; } = new();

        public PaginacionDto Paginacion { get; set; } = new();
    }

    public class DetalleTransaccionDto
    {
        public TransaccionDto Transaccion { get; set; } = null!;

        public string TelefonoTransportador { get; set; } = string.Empty;

        public string Placa { get; set; } = string.Empty;

        public List<CalificacionVistaDto> Calificaciones { get; set; } = new();
    }

    public class CalificacionVistaDto
    {
        public string Evaluador { get; set; } = string.Empty;

        public string Evaluado { get; set; } = string.Empty;

        public int Estrellas { get; set; }

        public string Comentario { get; set; } = string.Empty;
    }

    public class FiltroHistorialGlobalDto
    {
        /// <summary>"ventas" o "logs".</summary>
        public string Pestana { get; set; } = "ventas";

        public int? IdUsuario { get; set; }

        public string? Rol { get; set; }

        public DateTime? Desde { get; set; }

        public DateTime? Hasta { get; set; }

        /// <summary>Página que se muestra en pantalla (no se usa en el PDF).</summary>
        public int? Pagina { get; set; }
    }

    public class LogVistaDto
    {
        public DateTime Fecha { get; set; }

        public string Nivel { get; set; } = string.Empty;

        public string Usuario { get; set; } = string.Empty;

        public string Origen { get; set; } = string.Empty;

        public string Mensaje { get; set; } = string.Empty;
    }

    public class HistorialGlobalDto
    {
        public FiltroHistorialGlobalDto Filtro { get; set; } = new();

        public List<TransaccionDto> Ventas { get; set; } = new();

        public List<LogVistaDto> Logs { get; set; } = new();

        public PaginacionDto Paginacion { get; set; } = new();
    }

    /// <summary>Fila de "Precios Sugeridos" (CU-028); los valores se segmentan por rol (RN-01-CU029).</summary>
    public class PrecioVistaDto
    {
        public string Producto { get; set; } = string.Empty;

        public string Variedad { get; set; } = string.Empty;

        public decimal? PrecioComprador { get; set; }

        public decimal? PrecioCampesino { get; set; }

        public DateTime FechaCalculo { get; set; }
    }

    public class ListaPreciosDto
    {
        public UsuarioActual Usuario { get; set; } = null!;

        public List<PrecioVistaDto> Precios { get; set; } = new();

        public PaginacionDto Paginacion { get; set; } = new();
    }

    public class NotificacionDto
    {
        public int IdNotificacion { get; set; }

        public int? IdDemanda { get; set; }

        public string Tipo { get; set; } = string.Empty;

        public string Mensaje { get; set; } = string.Empty;

        public DateTime Fecha { get; set; }

        public bool Leida { get; set; }
    }

    /// <summary>Respuesta del sondeo periódico: contador de la campana, ventanas pendientes y avisos nuevos para mostrar como tarjeta.</summary>
    public class ResumenNotificacionesDto
    {
        public int NoLeidas { get; set; }

        public List<NotificacionDto> Popups { get; set; } = new();

        /// <summary>Notificaciones posteriores a la última que ya vio el navegador (se muestran como aviso flotante).</summary>
        public List<NotificacionDto> Nuevas { get; set; } = new();

        /// <summary>Id de la notificación más reciente del usuario; el navegador lo guarda para no repetir avisos.</summary>
        public int UltimoId { get; set; }
    }

    /// <summary>Una página de la lista de la campana.</summary>
    public class ListaNotificacionesDto
    {
        public List<NotificacionDto> Notificaciones { get; set; } = new();

        public int NoLeidas { get; set; }

        public PaginacionDto Paginacion { get; set; } = new();
    }
}
