using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using CampesinApp.Repositories;
using CampesinApp.Repositories.Models;

namespace CampesinApp.Controllers
{
    /// <summary>
    /// Controlador encargado de gestionar el Caso de Uso CU-011: Catálogo de Demanda.
    /// 
    /// REGLAS ARQUITECTÓNICAS CUMPLIDAS:
    /// 1. El controlador NO instancia ni llama directamente a DbContext.
    /// 2. Toda consulta a la BD se realiza exclusivamente a través de DemandaRepository.
    /// 3. Soporta tanto usuarios 'Invitados' (solo lectura) como usuarios 'Campesino' autenticados (RN01-CU011).
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
            // 1. Obtener el rol del usuario desde la sesión HTTP (si existe)
            string? rolUsuario = HttpContext.Session.GetString("RolUsuario");

            var modelo = new CatalogoDemandaViewModel
            {
                CategoriaSeleccionada = categoria,
                ProductoSeleccionado = producto,
                RegionSeleccionada = region,
                RolUsuario = rolUsuario
            };

            try
            {
                // 2. Consultar demandas aplicando los 3 filtros a través del repositorio
                modelo.Demandas = await _demandaRepository.ObtenerDemandasFiltradasAsync(categoria, producto, region);

                // 3. Obtener opciones para poblar los 3 desplegables
                modelo.CategoriasDisponibles = await _demandaRepository.ObtenerNombresCategoriasAsync();
                modelo.ProductosDisponibles = await _demandaRepository.ObtenerNombresProductosAsync();
                modelo.RegionesDisponibles = await _demandaRepository.ObtenerNombresRegionesAsync();
            }
            catch (Exception)
            {
                // FE-01-CU011: Error en base de datos
                // Si falla la conexión con el servidor de base de datos, el sistema muestra el mensaje de error
                modelo.TieneErrorConexion = true;
                modelo.MensajeError = "No fue posible cargar el catálogo de demanda. Intente más tarde";
                modelo.Demandas = new List<Demandum>();
            }

            // 4. Retornar la vista con el ViewModel completo
            return View(modelo);
        }
    }
}

