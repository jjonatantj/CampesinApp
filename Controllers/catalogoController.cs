using Microsoft.AspNetCore.Mvc;
using CampesinApp.Repositories;
using CampesinApp.Repositories.Models;

namespace CampesinApp.Controllers
{
    /// <summary>
    /// Controlador encargado de gestionar el Caso de Uso CU-011: Catálogo de Demanda.
    /// 
    /// REGLA ARQUITECTÓNICA CUMPLIDA:
    /// - Este controlador NO instancia ni llama directamente a DbContext.
    /// - Toda consulta a la BD se delega al repositorio (DemandaRepository).
    /// </summary>
    public class catalogoController : Controller
    {
        private readonly DemandaRepository _demandaRepository;

        // Inyección de dependencias por constructor
        public catalogoController(DemandaRepository demandaRepository)
        {
            _demandaRepository = demandaRepository;
        }

        /// <summary>
        /// Acción principal Index:
        /// Recibe por parámetros GET los 3 filtros especificados en CU-011:
        /// categoría, producto y región.
        /// </summary>
        public async Task<IActionResult> Index(string? categoria, string? producto, string? region)
        {
            // 1. Consultamos las demandas aplicando los 3 filtros
            var demandas = await _demandaRepository.ObtenerDemandasFiltradasAsync(categoria, producto, region);

            // 2. Obtenemos las opciones para poblar los 3 desplegables
            var categoriasDisponibles = await _demandaRepository.ObtenerNombresCategoriasAsync();
            var productosDisponibles = await _demandaRepository.ObtenerNombresProductosAsync();
            var regionesDisponibles = await _demandaRepository.ObtenerNombresRegionesAsync();

            // 3. Construimos el ViewModel para la vista
            var modelo = new CatalogoDemandaViewModel
            {
                CategoriaSeleccionada = categoria,
                ProductoSeleccionado = producto,
                RegionSeleccionada = region,
                CategoriasDisponibles = categoriasDisponibles,
                ProductosDisponibles = productosDisponibles,
                RegionesDisponibles = regionesDisponibles,
                Demandas = demandas
            };

            // 4. Retornamos la vista con el modelo completo
            return View(modelo);
                }
            };
        }
    }
}

