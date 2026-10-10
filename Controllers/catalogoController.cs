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

        // =========================================================================
        // ACCIÓN DETALLE (CU-012: RESPONDER A DEMANDA DE PRODUCTO)
        // =========================================================================
        /// <summary>
        /// Muestra el detalle específico de una demanda seleccionada en el catálogo CU-011.
        /// Flujo de excepción FE01-CU012: En caso de error o si la demanda no existe,
        /// redirige al catálogo mostrando una notificación temporal.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Detalle(int id)
        {
            if (id <= 0)
            {
                TempData["MensajeError"] = "Identificador de demanda no válido.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                // Consultamos la demanda a través del repositorio con todas sus relaciones
                var demanda = await _demandaRepository.ObtenerDemandaPorIdAsync(id);

                if (demanda == null)
                {
                    // FE01-CU012: Demanda inexistente
                    TempData["MensajeError"] = "La demanda seleccionada no fue encontrada o ya no está disponible.";
                    return RedirectToAction(nameof(Index));
                }

                // Consultamos el total aportado previamente por otros campesinos
                int totalAportado = await _demandaRepository.ObtenerTotalAportadoAsync(id);
                int cantidadFaltante = Math.Max(0, demanda.CantidadDeman - totalAportado);

                string? rolUsuario = HttpContext.Session.GetString("RolUsuario");

                var viewModel = new CampesinApp.ViewModels.DetalleDemandaViewModel
                {
                    IdDemanda = demanda.IdDemanda,
                    NombreProducto = demanda.IdTipoProductoNavigation?.Producto ?? "Producto sin especificar",
                    VariedadProducto = demanda.IdVariedadNavigation?.VariedadProd ?? "Variedad estándar",
                    Descripcion = string.IsNullOrWhiteSpace(demanda.DescripcionDeman) ? "Sin observaciones adicionales." : demanda.DescripcionDeman,
                    EstadoDemanda = demanda.IdEstadoDemanNavigation?.Estado ?? "Activa",
                    CantidadTotal = demanda.CantidadDeman,
                    CantidadAportada = totalAportado,
                    CantidadFaltante = cantidadFaltante,
                    PrecioPropuesto = demanda.PrecioDeman,
                    DireccionEntrega = demanda.DireccionFinal,
                    MunicipioEntrega = demanda.IdMunicipioNavigation?.Municipio1 ?? "Municipio no registrado",
                    DepartamentoEntrega = demanda.IdMunicipioNavigation?.IdDepartamentoNavigation?.Departamento1 ?? "",
                    FechaLimite = demanda.LimTiemDeman,
                    FechaEntrega = demanda.FechaEntrega,
                    CompradorId = demanda.IdUsuario,
                    CompradorNombreCompleto = $"{demanda.IdUsuarioNavigation?.NombreCom} {demanda.IdUsuarioNavigation?.ApellidoCom}".Trim(),
                    CompradorTelefono = demanda.IdUsuarioNavigation != null && demanda.IdUsuarioNavigation.TelelfonoUsu > 0
                        ? demanda.IdUsuarioNavigation.TelelfonoUsu.ToString()
                        : "No disponible",
                    CompradorCorreo = demanda.IdUsuarioNavigation?.CorreoUsu ?? "No disponible",
                    CompradorMunicipio = demanda.IdUsuarioNavigation?.IdMunicipioNavigation?.Municipio1 ?? "Colombia",
                    EsUsuarioAutenticado = !string.IsNullOrEmpty(rolUsuario),
                    RolUsuario = rolUsuario
                };

                return View(viewModel);
            }
            catch (Exception)
            {
                // FE01-CU012: Error al conectar con la base de datos
                TempData["MensajeError"] = "No fue posible conectar con el servidor para consultar el detalle de la demanda. Intente más tarde.";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// Alias de enrutamiento: permite ingresar también a través de /catalogo/Responder/{id}
        /// </summary>
        [HttpGet]
        public IActionResult Responder(int id)
        {
            return RedirectToAction(nameof(Detalle), new { id });
        }
    }
}

