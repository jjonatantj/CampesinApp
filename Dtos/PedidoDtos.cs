using CampesinApp.Repositories.Models;

namespace CampesinApp.Dtos
{
    public class PedidoResumenDto
    {
        public int IdDemanda { get; set; }

        public string Codigo => CodigoPedido.Formatear(IdDemanda);

        public string Producto { get; set; } = string.Empty;

        public int Cantidad { get; set; }

        public decimal PrecioUnitario { get; set; }

        public DateTime FechaEntrega { get; set; }

        public string DireccionFinal { get; set; } = string.Empty;

        public string EstadoEnvio { get; set; } = string.Empty;

        public bool TieneViaje { get; set; }

        public string NombreComprador { get; set; } = string.Empty;

        /// <summary>El comprador de este pedido puede validar la recepción (CU-025).</summary>
        public bool PuedeValidarEntrega { get; set; }

        /// <summary>El usuario puede calificar el pedido: el comprador con estrellas y comentario, el transportador con un comentario (CU-026).</summary>
        public bool PuedeCalificar { get; set; }

        /// <summary>El pedido ya está finalizado y el usuario solo puede consultar las observaciones y la calificación (CU-026).</summary>
        public bool PuedeVerObservaciones { get; set; }
    }

    public class ListaPedidosDto
    {
        public UsuarioActual Usuario { get; set; } = null!;

        public string? EstadoFiltro { get; set; }

        public List<PedidoResumenDto> Pedidos { get; set; } = new();

        public PaginacionDto Paginacion { get; set; } = new();
    }

    /// <summary>Un participante de un pedido: comprador, campesino o transportador.</summary>
    public sealed record ParticipantePedido(int IdUsuario, string Nombre, RolUsuario Rol);

    public static class CodigoPedido
    {
        public static string Formatear(int idDemanda) => $"PED-{idDemanda:D4}";

        /// <summary>Acepta "PED-0012", "ped12" o "12".</summary>
        public static bool TryParsear(string? texto, out int idDemanda)
        {
            idDemanda = 0;
            if (string.IsNullOrWhiteSpace(texto)) return false;
            var limpio = texto.Trim();
            if (limpio.StartsWith("PED", StringComparison.OrdinalIgnoreCase))
            {
                limpio = limpio[3..].TrimStart('-', ' ');
            }
            return int.TryParse(limpio, out idDemanda) && idDemanda > 0;
        }
    }
}
