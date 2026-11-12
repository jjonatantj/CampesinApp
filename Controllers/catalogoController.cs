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
        /// Recibe por parámetros GET los filtros jerárquicos y la página actual:
        /// 1. idTipoProducto (Tipo de Producto)
        /// 2. idVariedad (Variedad dependiente)
        /// 3. idDepartamento (Departamento)
        /// 4. idMunicipio (Municipio dependiente)
        /// 5. pagina (Página actual para mostrar de a 8 demandas = 2 filas de 4)
        /// </summary>
        public async Task<IActionResult> Index(int? idTipoProducto, int? idVariedad, int? idDepartamento, int? idMunicipio, int pagina = 1)
        {
            // 1. Obtener el rol del usuario desde la sesión HTTP (si existe)
            string? rolUsuario = HttpContext.Session.GetString("RolUsuario");

            if (pagina < 1) pagina = 1;

            var modelo = new CatalogoDemandaViewModel
            {
                IdTipoProductoSeleccionado = idTipoProducto,
                IdVariedadSeleccionada = idVariedad,
                IdDepartamentoSeleccionado = idDepartamento,
                IdMunicipioSeleccionado = idMunicipio,
                RolUsuario = rolUsuario,
                PaginaActual = pagina,
                TamanoPagina = 8 // 8 productos por página (2 filas de 4 tarjetas)
            };

            try
            {
                // 2. Consultar demandas aplicando los filtros jerárquicos a través del repositorio
                var todasDemandas = await _demandaRepository.ObtenerDemandasFiltradasAsync(
                    idTipoProducto, idVariedad, idDepartamento, idMunicipio);

                modelo.TotalDemandas = todasDemandas.Count;

                // Si la página excede el total, ubicamos en la última página disponible
                if (modelo.TotalPaginas > 0 && modelo.PaginaActual > modelo.TotalPaginas)
                {
                    modelo.PaginaActual = modelo.TotalPaginas;
                }

                // Aplicar paginación (Skip y Take) para obtener únicamente los 8 de la página
                modelo.Demandas = todasDemandas
                    .Skip((modelo.PaginaActual - 1) * modelo.TamanoPagina)
                    .Take(modelo.TamanoPagina)
                    .ToList();

                // 3. Poblar desplegables principales
                modelo.TiposProductoDisponibles = await _demandaRepository.ObtenerTiposProductoAsync();
                modelo.DepartamentosDisponibles = await _demandaRepository.ObtenerDepartamentosAsync();

                // 4. Si hay producto seleccionado, poblar sus variedades dependientes
                if (idTipoProducto.HasValue && idTipoProducto.Value > 0)
                {
                    modelo.VariedadesDisponibles = await _demandaRepository.ObtenerVariedadesPorTipoProductoAsync(idTipoProducto.Value);
                }

                // 5. Si hay departamento seleccionado, poblar sus municipios dependientes
                if (idDepartamento.HasValue && idDepartamento.Value > 0)
                {
                    modelo.MunicipiosDisponibles = await _demandaRepository.ObtenerMunicipiosPorDepartamentoAsync(idDepartamento.Value);
                }
            }
            catch (Exception)
            {
                // FE-01-CU011: Error en base de datos
                modelo.TieneErrorConexion = true;
                modelo.MensajeError = "No fue posible cargar el catálogo de demanda. Intente más tarde";
                modelo.Demandas = new List<Demandum>();
            }

            // 6. Retornar la vista con el ViewModel completo
            return View(modelo);
        }

        /// <summary>
        /// Endpoint AJAX: Retorna en formato JSON las variedades asociadas al tipo de producto seleccionado (cascada).
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ObtenerVariedades(int idTipoProducto)
        {
            if (idTipoProducto <= 0)
            {
                return Json(new List<object>());
            }

            var variedades = await _demandaRepository.ObtenerVariedadesPorTipoProductoAsync(idTipoProducto);
            var resultado = variedades.Select(v => new
            {
                id = v.IdVariedad,
                nombre = v.VariedadProd
            });

            return Json(resultado);
        }

        /// <summary>
        /// Endpoint AJAX: Retorna en formato JSON los municipios asociados al departamento seleccionado (cascada).
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ObtenerMunicipios(int idDepartamento)
        {
            if (idDepartamento <= 0)
            {
                return Json(new List<object>());
            }

            var municipios = await _demandaRepository.ObtenerMunicipiosPorDepartamentoAsync(idDepartamento);
            var resultado = municipios.Select(m => new
            {
                id = m.IdMunicipio,
                nombre = m.Municipio1
            });

            return Json(resultado);
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
                    PrecioPropuesto = demanda.PrecioCampesino ?? demanda.PrecioDeman,
                    DireccionEntrega = demanda.IdPuntoAcopioNavigation?.Direccion ?? demanda.DireccionInicial ?? "Punto de Acopio",
                    MunicipioEntrega = demanda.IdPuntoAcopioNavigation?.IdMunicipioNavigation?.Municipio1 ?? "Municipio no registrado",
                    DepartamentoEntrega = demanda.IdPuntoAcopioNavigation?.IdMunicipioNavigation?.IdDepartamentoNavigation?.Departamento1 ?? "",
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

