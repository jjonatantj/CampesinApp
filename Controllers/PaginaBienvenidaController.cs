using Microsoft.AspNetCore.Mvc;
using CampesinApp.Repositories;

namespace CampesinApp.Controllers
{
    /// <summary>
    /// Controlador para la Página de Bienvenida y Pantalla Principal (CU-001).
    /// Permite a cualquier visitante (campesino, comprador, transportador o invitado)
    /// conocer el propósito de CampesinApp y consultar el catálogo preliminar desde la BD real.
    /// </summary>
    public class PaginaBienvenidaController : Controller
    {
        private readonly PaginaBienvenidaRepository _bienvenidaRepo;

        public PaginaBienvenidaController(PaginaBienvenidaRepository bienvenidaRepo)
        {
            _bienvenidaRepo = bienvenidaRepo;
        }

        // =========================================================================
        // ACCIÓN PRINCIPAL: Cargar la pantalla de bienvenida (CU-001)
        // =========================================================================
        public IActionResult Bienvenida()
        {
            // Consultar el catálogo de demandas desde la base de datos SQL Server
            var catalogoPreliminar = _bienvenidaRepo.ObtenerCatalogoPreliminar();

            return View(catalogoPreliminar);
        }
    }
}

