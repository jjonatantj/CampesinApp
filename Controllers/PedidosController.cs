using CampesinApp.Dtos;
using CampesinApp.Services;
using CampesinApp.Repositories.Models;
using Microsoft.AspNetCore.Mvc;

namespace CampesinApp.Controllers
{
    /// <summary>"Lista de Pedidos" (CU-022, paso 2): ventana desde la que se accede a seguimiento, transporte, entrega y calificación.</summary>
    [RolesPermitidos(RolUsuario.Campesino, RolUsuario.Comprador, RolUsuario.Transportador, RolUsuario.Administrador)]
    public class PedidosController : AppController
    {
        private const int PedidosPorPagina = 4;

        private readonly PedidoService _pedidos;

        public PedidosController(IUsuarioActualService usuarioActual, PedidoService pedidos) : base(usuarioActual)
        {
            _pedidos = pedidos;
        }

        [HttpGet]
        public async Task<IActionResult> Lista(string? estado, int? pagina)
        {
            var usuario = (await ObtenerUsuarioAsync())!;

            // El total se necesita para saber en qué página caer; se calcula con la primera consulta y se repite si hay que corregirla.
            var solicitada = Math.Max(1, pagina ?? 1);
            var (pedidos, total) = await _pedidos.ListarAsync(usuario, estado, solicitada, PedidosPorPagina);
            var paginacion = PaginacionDto.Crear(solicitada, total, PedidosPorPagina, "pedidos",
                $"/Pedidos/Lista?estado={Uri.EscapeDataString(estado ?? string.Empty)}&pagina={{p}}", "principal");
            if (paginacion.Pagina != solicitada)
            {
                (pedidos, _) = await _pedidos.ListarAsync(usuario, estado, paginacion.Pagina, PedidosPorPagina);
            }

            return ModalPrincipal("_ListaPedidos", new ListaPedidosDto
            {
                Usuario = usuario,
                EstadoFiltro = estado,
                Pedidos = pedidos,
                Paginacion = paginacion
            });
        }
    }
}
