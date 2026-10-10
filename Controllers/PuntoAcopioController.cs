using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Http;
using CampesinApp.Repositories;
using CampesinApp.Repositories.Models;
using CampesinApp.ViewModels;

namespace CampesinApp.Controllers
{
    /// <summary>
    /// Controlador para el Caso de Uso CU-018: Registro de Puntos de Acopio.
    /// 
    /// REGLAS ARQUITECTÓNICAS:
    /// 1. NO accede directamente al DbContext. Utiliza exclusivamente IPuntoAcopioRepository.
    /// 2. Aplica las reglas de negocio y excepciones según la especificación CU-018.
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
        // ACCIÓN GET: Mostrar el formulario de registro (Pasos 1 y 2 de CU-018)
        // =========================================================================
        [HttpGet]
        public async Task<IActionResult> Registrar()
        {
            // Verificación de Precondición: Usuario autenticado con rol autorizado
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
        // ACCIÓN POST: Procesar el registro del punto de acopio (Pasos 3 y 4)
        // =========================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Registrar(PuntoAcopioViewModel model)
        {
            // Validar sesión
            if (!EsUsuarioAutorizado(out int idUsuario, out string rol))
            {
                return RedirectToAction("Login", "InicioSesion");
            }

            model.GoogleMapsApiKey = _configuration["GoogleMaps:ApiKey"] ?? string.Empty;

            // REGLA DE NEGOCIO RN-01-CU018:
            // El registro del punto de acopio exige capturar la ubicación geográfica de forma obligatoria.
            if (!model.Latitud.HasValue || !model.Longitud.HasValue || model.Latitud == 0 || model.Longitud == 0)
            {
                ModelState.AddModelError("Latitud", "Debe capturar la ubicación geográfica obligatoria seleccionando el punto en el mapa interactivo.");
            }

            // REGLA DE NEGOCIO RN-02-CU018:
            // El punto de acopio debe tener acceso vehicular accesible para el camión.
            if (!model.AccesoVehicular)
            {
                ModelState.AddModelError("AccesoVehicular", "El punto de acopio debe contar obligatoriamente con acceso vehicular accesible para el camión.");
            }

            // Validar campos obligatorios del modelo
            if (!ModelState.IsValid)
            {
                await CargarListasDesplegablesAsync(model);
                return View(model);
            }

            // FLUJO DE EXCEPCIÓN FE-02-CU018:
            // Punto de acopio duplicado en la misma dirección o coordenadas.
            try
            {
                bool esDuplicado = await _puntoAcopioRepo.ExistePuntoDuplicadoAsync(
                    model.Direccion,
                    model.Latitud!.Value,
                    model.Longitud!.Value);

                if (esDuplicado)
                {
                    // Mensaje emergente de 6 segundos especificado en FE-02-CU018
                    ViewBag.MensajeErrorDuplicado = "Ya existe un punto de acopio registrado en la dirección ingresada.";
                    await CargarListasDesplegablesAsync(model);
                    return View(model);
                }
            }
            catch (Exception)
            {
                // Error al verificar duplicados por fallo de BD
                ViewBag.MensajeErrorConexion = "No fue posible conectar con el servicio de CampesinApp, por favor verifique su conexión a internet e intente de nuevo";
                await CargarListasDesplegablesAsync(model);
                return View(model);
            }

            // FLUJO DE EXCEPCIÓN FE-01-CU018:
            // Error en base de datos o fallo de conexión al intentar guardar.
            try
            {
                var nuevoPuntoDto = new CampesinApp.Dtos.PuntoAcopioRegistroDto
                {
                    IdUsuario = idUsuario,
                    NombrePunto = model.NombrePunto.Trim(),
                    IdDepartamento = model.IdDepartamento,
                    IdMunicipio = model.IdMunicipio,
                    Direccion = model.Direccion.Trim(),
                    Latitud = model.Latitud.Value,
                    Longitud = model.Longitud.Value,
                    Capacidad = model.Capacidad,
                    AccesoVehicular = model.AccesoVehicular
                };

                await _puntoAcopioRepo.GuardarPuntoAcopioAsync(nuevoPuntoDto);

                // ÉXITO (Paso 4 del CU-018):
                // Notifica en pantalla "Punto de acopio registrado satisfactoriamente" durante 3 segundos
                // y redirecciona al perfil de usuario (CU-031).
                ViewBag.RegistroExitoso = true;
                ViewBag.MensajeExito = "Punto de acopio registrado satisfactoriamente";
                TempData["MensajeExito"] = "Punto de acopio registrado satisfactoriamente";

                await CargarListasDesplegablesAsync(model);
                return View(model);
            }
            catch (Exception)
            {
                // FE-01-CU018: Error al guardar en base de datos
                ViewBag.MensajeErrorConexion = "No fue posible conectar con el servicio de CampesinApp, por favor verifique su conexión a internet e intente de nuevo";
                await CargarListasDesplegablesAsync(model);
                return View(model);
            }
        }

        // =========================================================================
        // ENDPOINT AJAX: Obtener municipios dinámicos por departamento
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

            // Precondición: El usuario debe contar con una sesión activa en la plataforma
            return idUsuario > 0;
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
                model.Municipios = new List<SelectListItem>();
            }
        }
    }
}

