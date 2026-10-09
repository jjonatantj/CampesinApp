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
        private readonly PedidoService _pedidos;

        public PedidosController(IUsuarioActualService usuarioActual, PedidoService pedidos) : base(usuarioActual)
        {
            _pedidos = pedidos;
        }

        [HttpGet]
        public async Task<IActionResult> Lista(string? estado)
        {
            var usuario = (await ObtenerUsuarioAsync())!;
            var modelo = new ListaPedidosDto
            {
                Usuario = usuario,
                EstadoFiltro = estado,
                Pedidos = await _pedidos.ListarAsync(usuario, estado)
            };
            return ModalPrincipal("_ListaPedidos", modelo);
        }
    }
}
