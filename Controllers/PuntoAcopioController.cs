using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using CampesinApp.Repositories;
using CampesinApp.ViewModels;
using CampesinApp.Dtos;

namespace CampesinApp.Controllers
{
    /// <summary>
    /// Controlador para la gestión de Puntos de Acopio:
    /// - CU-018: Registro de Puntos de Acopio (Formulario y Georreferenciación).
    /// - CU-019: Revisar Puntos de Acopio (Mapas interactivos y filtros de demandas/municipios).
    /// 
    /// REGLAS ARQUITECTÓNICAS:
    /// 1. NO accede directamente al DbContext. Utiliza exclusivamente IPuntoAcopioRepository.
    /// 2. Aplica las reglas de negocio (RN-01 y RN-02) y flujos de excepción.
    /// 3. Aplica la seguridad de sesión en todos los métodos protegidos.
    /// </summary>
    public class PuntoAcopioController : Controller
    {
        private readonly IPuntoAcopioRepository _puntoAcopioRepo;
        private readonly IConfiguration _configuration;

        public PuntoAcopioController(IPuntoAcopioRepository puntoAcopioRepo, IConfiguration configuration)
        {
            _puntoAcopioRepo = puntoAcopioRepo;
            _configuration = configuration;
        }

        // =========================================================================
        // CASO DE USO CU-019: REVISAR PUNTOS DE ACOPIO (MAPAS)
        // Acción principal: Carga la vista del mapa con los filtros disponibles
        // =========================================================================
        [HttpGet]
        [Route("PuntoAcopio")]
        [Route("PuntoAcopio/Index")]
        public async Task<IActionResult> Index(int? idDemanda, int? idMunicipio)
        {
            // Verificación de Precondición: Usuario con sesión activa
            if (!EsUsuarioAutorizado(out int idUsuario, out string rol))
            {
                return RedirectToAction("Login", "InicioSesion");
            }

            var model = new RevisarPuntosAcopioViewModel
            {
                GoogleMapsApiKey = _configuration["GoogleMaps:ApiKey"] ?? string.Empty,
                IdDemandaSeleccionada = idDemanda,
                IdMunicipioSeleccionado = idMunicipio
            };

            try
            {
                await CargarFiltrosMapaAsync(model);
            }
            catch (Exception)
            {
                // FE03-CU019: Error al consultar datos para filtros
                model.MensajeError = "Error al obtener la información actualizada de los puntos de acopio. Intente nuevamente.";
            }

            return View(model);
        }

        // =========================================================================
        // ENDPOINT AJAX CU-019: Devolver puntos de acopio filtrados en formato JSON
        // =========================================================================
        [HttpGet]
        [Route("PuntoAcopio/ObtenerPuntosMapaJson")]
        public async Task<IActionResult> ObtenerPuntosMapaJson(int? idDemanda, int? idMunicipio)
        {
            if (!EsUsuarioAutorizado(out int idUsuario, out string rol))
            {
                return Unauthorized(new { mensaje = "Sesión no válida o expirada." });
            }

            try
            {
                var puntos = await _puntoAcopioRepo.ObtenerPuntosAcopioFiltradosAsync(idDemanda, idMunicipio);

                var resultado = puntos.Select(p => new
                {
                    id = p.IdPuntoAcopio,
                    nombrePunto = p.NombrePunto,
                    direccion = p.Direccion,
                    horarioAtencion = p.HorarioAtencion,
                    capacidad = p.Capacidad,
                    telefono = p.Telefono,
                    latitud = p.Latitud,
                    longitud = p.Longitud,
                    accesoVehicular = p.AccesoVehicular,
                    municipio = p.IdMunicipioNavigation?.Municipio1 ?? "Municipio",
                    departamento = p.IdMunicipioNavigation?.IdDepartamentoNavigation?.Departamento1 ?? "Colombia"
                });

                return Json(resultado);
            }
            catch (Exception)
            {
                // FE03-CU019: Error de conexión o base de datos
                return StatusCode(500, new { mensaje = "Error al obtener la información actualizada de los puntos de acopio. Intente nuevamente." });
            }
        }

        // =========================================================================
        // ACCIÓN GET: Mostrar el formulario de registro (Pasos 1 y 2 de CU-018)
        // =========================================================================
        [HttpGet]
        [Route("PuntoAcopio/Registrar")]
        public async Task<IActionResult> Registrar()
        {
            // Verificación de Precondición: Usuario autenticado con sesión activa
            if (!EsUsuarioAutorizado(out int idUsuario, out string rol))
            {
                return RedirectToAction("Login", "InicioSesion");
            }

            var model = new PuntoAcopioViewModel
            {
                // Obtenemos la Google Maps API Key desde appsettings.json
                GoogleMapsApiKey = _configuration["GoogleMaps:ApiKey"] ?? string.Empty
            };

            await CargarListasDesplegablesAsync(model);

            return View(model);
        }

        // =========================================================================
        // ACCIÓN POST: Procesar el registro del punto de acopio (Pasos 3 y 4 de CU-018)
        // =========================================================================
        [HttpPost]
        [Route("PuntoAcopio/Registrar")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Registrar(PuntoAcopioViewModel model)
        {
            // 1. Validar sesión de usuario
            if (!EsUsuarioAutorizado(out int idUsuario, out string rol))
            {
                return RedirectToAction("Login", "InicioSesion");
            }

            model.GoogleMapsApiKey = _configuration["GoogleMaps:ApiKey"] ?? string.Empty;

            // 2. REGLA DE NEGOCIO RN-01-CU018: Ubicación geográfica obligatoria
            if (!model.Latitud.HasValue || !model.Longitud.HasValue || model.Latitud == 0 || model.Longitud == 0)
            {
                ModelState.AddModelError("Latitud", "Debe hacer clic en el mapa interactivo o usar 'Mi Ubicación Actual' para fijar las coordenadas geográficas.");
            }

            // 3. REGLA DE NEGOCIO RN-02-CU018: Acceso vehicular accesible para camión
            if (!model.AccesoVehicular)
            {
                ModelState.AddModelError("AccesoVehicular", "El punto de acopio debe contar obligatoriamente con acceso vehicular accesible para camiones.");
            }

            // 4. Validar integridad del formulario
            if (!ModelState.IsValid)
            {
                await CargarListasDesplegablesAsync(model);
                return View(model);
            }

            // 5. FLUJO DE EXCEPCIÓN FE-02-CU018: Validar duplicado
            try
            {
                bool esDuplicado = await _puntoAcopioRepo.ExistePuntoDuplicadoAsync(
                    model.Direccion,
                    model.Latitud!.Value,
                    model.Longitud!.Value);

                if (esDuplicado)
                {
                    ViewBag.MensajeErrorDuplicado = "Ya existe un punto de acopio registrado en la misma dirección o coordenadas geográficas.";
                    await CargarListasDesplegablesAsync(model);
                    return View(model);
                }
            }
            catch (Exception)
            {
                ViewBag.MensajeErrorConexion = "No fue posible conectar con el servicio de CampesinApp, por favor verifique su conexión a internet e intente de nuevo.";
                await CargarListasDesplegablesAsync(model);
                return View(model);
            }

            // 6. FLUJO PRINCIPAL DE GUARDADO (Paso 4):
            try
            {
                var nuevoPuntoDto = new PuntoAcopioRegistroDto
                {
                    IdUsuario = idUsuario,
                    IdDepartamento = model.IdDepartamento,
                    IdMunicipio = model.IdMunicipio,
                    NombrePunto = model.NombrePunto.Trim(),
                    Direccion = model.Direccion.Trim(),
                    Telefono = model.Telefono.Trim(),
                    HorarioAtencion = model.HorarioAtencion.Trim(),
                    Latitud = model.Latitud.Value,
                    Longitud = model.Longitud.Value,
                    Capacidad = model.Capacidad,
                    AccesoVehicular = model.AccesoVehicular
                };

                int idPuntoGenerado = await _puntoAcopioRepo.GuardarPuntoAcopioAsync(nuevoPuntoDto);

                ViewBag.RegistroExitoso = true;
                ViewBag.MensajeExito = $"Punto de acopio '{model.NombrePunto}' registrado satisfactoriamente (ID #{idPuntoGenerado}).";
                TempData["MensajeExito"] = "Punto de acopio registrado satisfactoriamente.";

                await CargarListasDesplegablesAsync(model);
                return View(model);
            }
            catch (Exception)
            {
                ViewBag.MensajeErrorConexion = "No fue posible conectar con el servicio de CampesinApp, por favor verifique su conexión a internet e intente de nuevo.";
                await CargarListasDesplegablesAsync(model);
                return View(model);
            }
        }

        // =========================================================================
        // ENDPOINT AJAX: Obtener municipios dinámicos por departamento (CU-018)
        // =========================================================================
        [HttpGet]
        public async Task<IActionResult> ObtenerMunicipios(int idDepartamento)
        {
            var municipios = await _puntoAcopioRepo.ObtenerMunicipiosPorDepartamentoAsync(idDepartamento);
            var resultado = municipios.Select(m => new
            {
                id = m.IdMunicipio,
                nombre = m.Municipio1
            });

            return Json(resultado);
        }

        // =========================================================================
        // MÉTODOS PRIVADOS AUXILIARES
        // =========================================================================
        private bool EsUsuarioAutorizado(out int idUsuario, out string rol)
        {
            idUsuario = HttpContext.Session.GetInt32("IdUsuario") ?? 0;
            rol = HttpContext.Session.GetString("RolUsuario") ?? string.Empty;

            // Precondición: El usuario debe tener sesión activa
            return idUsuario > 0;
        }

        private async Task CargarFiltrosMapaAsync(RevisarPuntosAcopioViewModel model)
        {
            // Cargar Demandas
            var demandas = await _puntoAcopioRepo.ObtenerDemandasParaFiltroAsync();
            model.DemandasDisponibles = demandas.Select(d => new SelectListItem
            {
                Value = d.IdDemanda.ToString(),
                Text = $"Demanda #{d.IdDemanda} - {d.IdTipoProductoNavigation?.Producto ?? "Producto"} ({d.CantidadDeman:N0} Kg)",
                Selected = model.IdDemandaSeleccionada.HasValue && model.IdDemandaSeleccionada.Value == d.IdDemanda
            }).ToList();

            // Cargar Municipios
            var municipios = await _puntoAcopioRepo.ObtenerTodosMunicipiosAsync();
            model.MunicipiosDisponibles = municipios.Select(m => new SelectListItem
            {
                Value = m.IdMunicipio.ToString(),
                Text = $"{m.Municipio1} ({m.IdDepartamentoNavigation?.Departamento1 ?? "CO"})",
                Selected = model.IdMunicipioSeleccionado.HasValue && model.IdMunicipioSeleccionado.Value == m.IdMunicipio
            }).ToList();
        }

        private async Task CargarListasDesplegablesAsync(PuntoAcopioViewModel model)
        {
            var departamentos = await _puntoAcopioRepo.ObtenerDepartamentosAsync();
            model.Departamentos = departamentos.Select(d => new SelectListItem
            {
                Value = d.IdDepartamento.ToString(),
                Text = d.Departamento1,
                Selected = d.IdDepartamento == model.IdDepartamento
            }).ToList();

            if (model.IdDepartamento > 0)
            {
                var municipios = await _puntoAcopioRepo.ObtenerMunicipiosPorDepartamentoAsync(model.IdDepartamento);
                model.Municipios = municipios.Select(m => new SelectListItem
                {
                    Value = m.IdMunicipio.ToString(),
                    Text = m.Municipio1,
                    Selected = m.IdMunicipio == model.IdMunicipio
                }).ToList();
            }
            else
            {
                model.Municipios = new System.Collections.Generic.List<SelectListItem>();
            }
        }
    }
}

