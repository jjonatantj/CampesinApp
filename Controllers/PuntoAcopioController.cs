using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using CampesinApp.Repositories;
using CampesinApp.Repositories.Models;
using CampesinApp.ViewModels;
using CampesinApp.Dtos;
using CampesinApp.Services;

namespace CampesinApp.Controllers
{
    /// <summary>
    /// Controlador para la gestión de Puntos de Acopio:
    /// - CU-018: Registro de Puntos de Acopio (Formulario y Georreferenciación).
    /// - CU-019: Revisar Puntos de Acopio (Mapas interactivos y filtros por Tipo [Campesino/Comprador], Departamento y Municipio).
    /// </summary>
    public class PuntoAcopioController : Controller
    {
        private readonly IPuntoAcopioRepository _puntoAcopioRepo;
        private readonly IConfiguration _configuration;
        private readonly ILogService _logService;

        public PuntoAcopioController(
            IPuntoAcopioRepository puntoAcopioRepo, 
            IConfiguration configuration,
            ILogService logService)
        {
            _puntoAcopioRepo = puntoAcopioRepo;
            _configuration = configuration;
            _logService = logService;
        }

        // =========================================================================
        // CASO DE USO CU-019: REVISAR PUNTOS DE ACOPIO (MAPAS)
        // Acción principal: Carga la vista del mapa con los filtros disponibles
        // =========================================================================
        [HttpGet]
        [Route("PuntoAcopio")]
        [Route("PuntoAcopio/Index")]
        public async Task<IActionResult> Index(string? tipoAcopio, int? idDepartamento, int? idMunicipio, int? idDemanda)
        {
            // Verificación de Precondición: Usuario con sesión activa
            if (!EsUsuarioAutorizado(out int idUsuario, out string rol))
            {
                return RedirectToAction("Login", "InicioSesion");
            }

            // Conversión segura del bit de Tipo de Acopio ("1" o "true" = Comprador, "0" o "false" = Campesino)
            bool? filtroTipo = null;
            if (!string.IsNullOrWhiteSpace(tipoAcopio))
            {
                if (tipoAcopio == "1" || tipoAcopio.Equals("true", StringComparison.OrdinalIgnoreCase))
                {
                    filtroTipo = true;
                }
                else if (tipoAcopio == "0" || tipoAcopio.Equals("false", StringComparison.OrdinalIgnoreCase))
                {
                    filtroTipo = false;
                }
            }

            var model = new RevisarPuntosAcopioViewModel
            {
                GoogleMapsApiKey = _configuration["GoogleMaps:ApiKey"] ?? string.Empty,
                TipoAcopioSeleccionado = filtroTipo,
                IdDepartamentoSeleccionado = idDepartamento,
                IdMunicipioSeleccionado = idMunicipio,
                IdDemandaSeleccionada = idDemanda
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
        public async Task<IActionResult> ObtenerPuntosMapaJson(string? tipoAcopio, int? idDepartamento, int? idMunicipio, int? idDemanda)
        {
            if (!EsUsuarioAutorizado(out int idUsuario, out string rol))
            {
                return Unauthorized(new { mensaje = "Sesión no válida o expirada." });
            }

            // Conversión tolerante del bit: "1"/"true" => Comprador, "0"/"false" => Campesino
            bool? filtroTipo = null;
            if (!string.IsNullOrWhiteSpace(tipoAcopio))
            {
                if (tipoAcopio == "1" || tipoAcopio.Equals("true", StringComparison.OrdinalIgnoreCase))
                {
                    filtroTipo = true;
                }
                else if (tipoAcopio == "0" || tipoAcopio.Equals("false", StringComparison.OrdinalIgnoreCase))
                {
                    filtroTipo = false;
                }
            }

            try
            {
                var puntos = await _puntoAcopioRepo.ObtenerPuntosAcopioFiltradosAsync(filtroTipo, idDepartamento, idMunicipio, idDemanda);

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
                    tipoAcopio = p.TipoAcopio, // true = Comprador (1), false = Campesino (0)
                    tipoTexto = p.TipoAcopio ? "Comprador" : "Campesino",
                    descripcion = p.Descripcion ?? string.Empty,
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

            bool esAdmin = rol.Equals("Administrador", StringComparison.OrdinalIgnoreCase);

            var model = new PuntoAcopioViewModel
            {
                // Obtenemos la Google Maps API Key desde appsettings.json
                GoogleMapsApiKey = _configuration["GoogleMaps:ApiKey"] ?? string.Empty,
                EsAdministrador = esAdmin,
                TipoAcopioManual = false
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

            bool esAdmin = rol.Equals("Administrador", StringComparison.OrdinalIgnoreCase);
            model.EsAdministrador = esAdmin;
            model.GoogleMapsApiKey = _configuration["GoogleMaps:ApiKey"] ?? string.Empty;

            // 2. REGLA DE NEGOCIO RN-01-CU018: Ubicación geográfica obligatoria
            if (!model.Latitud.HasValue || !model.Longitud.HasValue || model.Latitud == 0 || model.Longitud == 0)
            {
                ModelState.AddModelError("Latitud", "Debe hacer clic en el mapa interactivo o usar 'Mi Ubicación Actual' para fijar las coordenadas geográficas.");
            }

            // 3. REGLA DE NEGOCIO RN-02-CU018: Acceso vehicular habilitado por defecto
            model.AccesoVehicular = true;

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
            catch (Exception ex)
            {
                // Registramos el detalle técnico en log_Sistema para auditoría técnica
                try
                {
                    await _logService.RegistrarAsync(
                        NivelesLog.Error,
                        "CU-018 Validación Duplicado",
                        $"Error al verificar duplicado de punto de acopio (Usuario ID #{idUsuario}): {ex.Message} | Detalle interno: {ex.InnerException?.Message}",
                        idUsuario > 0 ? idUsuario : (int?)null
                    );
                }
                catch { }

                ViewBag.MensajeErrorConexion = "No fue posible verificar la información del punto de acopio. Por favor intente nuevamente en unos momentos.";
                await CargarListasDesplegablesAsync(model);
                return View(model);
            }

            // 6. FLUJO PRINCIPAL DE GUARDADO (Paso 4):
            try
            {
                // Determinamos el tipo de acopio (1 = Comprador, 0 = Campesino):
                // Si es Administrador: usa la selección manual del formulario.
                // Si es otro rol: se determina según su rol autenticado.
                bool tipoAcopioFinal = esAdmin
                    ? model.TipoAcopioManual
                    : rol.Equals("Comprador", StringComparison.OrdinalIgnoreCase);

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
                    AccesoVehicular = true,
                    TipoAcopio = tipoAcopioFinal,
                    Descripcion = string.IsNullOrWhiteSpace(model.Descripcion) ? null : model.Descripcion.Trim()
                };

                int idPuntoGenerado = await _puntoAcopioRepo.GuardarPuntoAcopioAsync(nuevoPuntoDto);

                // Auditoría de registro exitoso en log_Sistema
                try
                {
                    await _logService.RegistrarAsync(
                        NivelesLog.Info,
                        "CU-018 Registro Punto de Acopio",
                        $"Punto de acopio '{model.NombrePunto}' (ID #{idPuntoGenerado}) registrado satisfactoriamente por el usuario ID #{idUsuario}.",
                        idUsuario > 0 ? idUsuario : (int?)null
                    );
                }
                catch { }

                ViewBag.RegistroExitoso = true;
                ViewBag.MensajeExito = $"Punto de acopio '{model.NombrePunto}' registrado satisfactoriamente (ID #{idPuntoGenerado}).";
                TempData["MensajeExito"] = "Punto de acopio registrado satisfactoriamente.";

                await CargarListasDesplegablesAsync(model);
                return View(model);
            }
            catch (Exception ex)
            {
                // Registramos el detalle técnico en log_Sistema para auditoría técnica
                try
                {
                    await _logService.RegistrarAsync(
                        NivelesLog.Error,
                        "CU-018 Registro Punto de Acopio",
                        $"Error al registrar punto de acopio (Usuario ID #{idUsuario}): {ex.Message} | Detalle interno: {ex.InnerException?.Message}",
                        idUsuario > 0 ? idUsuario : (int?)null
                    );
                }
                catch { }

                ViewBag.MensajeErrorConexion = "No fue posible registrar el punto de acopio debido a un problema interno del servicio. Por favor intente más tarde o comuníquese con el administrador.";
                await CargarListasDesplegablesAsync(model);
                return View(model);
            }
        }

        // =========================================================================
        // ENDPOINT AJAX: Obtener municipios dinámicos por departamento (CU-018 y CU-019)
        // =========================================================================
        [HttpGet]
        public async Task<IActionResult> ObtenerMunicipios(int idDepartamento)
        {
            var municipios = idDepartamento > 0
                ? await _puntoAcopioRepo.ObtenerMunicipiosPorDepartamentoAsync(idDepartamento)
                : await _puntoAcopioRepo.ObtenerTodosMunicipiosAsync();

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
            // 1. Cargar Tipos de Acopio (0: Campesino, 1: Comprador)
            model.TiposDisponibles = new System.Collections.Generic.List<SelectListItem>
            {
                new SelectListItem { Value = "0", Text = "Campesino", Selected = model.TipoAcopioSeleccionado.HasValue && !model.TipoAcopioSeleccionado.Value },
                new SelectListItem { Value = "1", Text = "Comprador", Selected = model.TipoAcopioSeleccionado.HasValue && model.TipoAcopioSeleccionado.Value }
            };

            // 2. Cargar Departamentos desde la base de datos
            var departamentos = await _puntoAcopioRepo.ObtenerDepartamentosAsync();
            model.DepartamentosDisponibles = departamentos.Select(d => new SelectListItem
            {
                Value = d.IdDepartamento.ToString(),
                Text = d.Departamento1,
                Selected = model.IdDepartamentoSeleccionado.HasValue && model.IdDepartamentoSeleccionado.Value == d.IdDepartamento
            }).ToList();

            // 3. Cargar Municipios (si hay departamento seleccionado se filtran por él, de lo contrario todos)
            if (model.IdDepartamentoSeleccionado.HasValue && model.IdDepartamentoSeleccionado.Value > 0)
            {
                var municipios = await _puntoAcopioRepo.ObtenerMunicipiosPorDepartamentoAsync(model.IdDepartamentoSeleccionado.Value);
                model.MunicipiosDisponibles = municipios.Select(m => new SelectListItem
                {
                    Value = m.IdMunicipio.ToString(),
                    Text = m.Municipio1,
                    Selected = model.IdMunicipioSeleccionado.HasValue && model.IdMunicipioSeleccionado.Value == m.IdMunicipio
                }).ToList();
            }
            else
            {
                var municipios = await _puntoAcopioRepo.ObtenerTodosMunicipiosAsync();
                model.MunicipiosDisponibles = municipios.Select(m => new SelectListItem
                {
                    Value = m.IdMunicipio.ToString(),
                    Text = $"{m.Municipio1} ({m.IdDepartamentoNavigation?.Departamento1 ?? "CO"})",
                    Selected = model.IdMunicipioSeleccionado.HasValue && model.IdMunicipioSeleccionado.Value == m.IdMunicipio
                }).ToList();
            }

            // 4. Cargar Demandas (opcional por compatibilidad)
            var demandas = await _puntoAcopioRepo.ObtenerDemandasParaFiltroAsync();
            model.DemandasDisponibles = demandas.Select(d => new SelectListItem
            {
                Value = d.IdDemanda.ToString(),
                Text = $"Demanda #{d.IdDemanda} - {d.IdTipoProductoNavigation?.Producto ?? "Producto"} ({d.CantidadDeman:N0} Kg)",
                Selected = model.IdDemandaSeleccionada.HasValue && model.IdDemandaSeleccionada.Value == d.IdDemanda
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
