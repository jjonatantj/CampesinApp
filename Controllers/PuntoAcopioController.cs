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
    /// Controlador para el Caso de Uso CU-018: Registro de Puntos de Acopio.
    /// 
    /// REGLAS ARQUITECTÓNICAS:
    /// 1. NO accede directamente al DbContext. Utiliza exclusivamente IPuntoAcopioRepository.
    /// 2. Aplica las reglas de negocio (RN-01 y RN-02) y flujos de excepción (FE-01 y FE-02).
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
        // ACCIÓN POST: Procesar el registro del punto de acopio (Pasos 3 y 4)
        // =========================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Registrar(PuntoAcopioViewModel model)
        {
            // 1. Validar sesión de usuario
            if (!EsUsuarioAutorizado(out int idUsuario, out string rol))
            {
                return RedirectToAction("Login", "InicioSesion");
            }

            model.GoogleMapsApiKey = _configuration["GoogleMaps:ApiKey"] ?? string.Empty;

            // 2. REGLA DE NEGOCIO RN-01-CU018:
            // El registro exige capturar la ubicación geográfica obligatoria mediante el mapa.
            if (!model.Latitud.HasValue || !model.Longitud.HasValue || model.Latitud == 0 || model.Longitud == 0)
            {
                ModelState.AddModelError("Latitud", "Debe hacer clic en el mapa interactivo para fijar las coordenadas geográficas (Latitud y Longitud).");
            }

            // 3. REGLA DE NEGOCIO RN-02-CU018:
            // El punto de acopio debe contar con acceso vehicular accesible para el camión.
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

            // 5. FLUJO DE EXCEPCIÓN FE-02-CU018:
            // Validar punto de acopio duplicado en la misma dirección o coordenadas.
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
                // Fallo de conexión o tiempo de espera con la BD
                ViewBag.MensajeErrorConexion = "No fue posible conectar con el servicio de CampesinApp, por favor verifique su conexión a internet e intente de nuevo.";
                await CargarListasDesplegablesAsync(model);
                return View(model);
            }

            // 6. FLUJO PRINCIPAL DE GUARDADO (Paso 4):
            // Persistir en 'punto_Acopio' y vincular en 'usuario_Punto_Acopio'
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

                // El repositorio inserta en 'punto_Acopio', obtiene el ID generado y lo relaciona en 'usuario_Punto_Acopio'
                int idPuntoGenerado = await _puntoAcopioRepo.GuardarPuntoAcopioAsync(nuevoPuntoDto);

                // Mensaje de éxito temporal de 3 segundos antes de redireccionar al perfil
                ViewBag.RegistroExitoso = true;
                ViewBag.MensajeExito = $"Punto de acopio '{model.NombrePunto}' registrado satisfactoriamente (ID #{idPuntoGenerado}).";
                TempData["MensajeExito"] = "Punto de acopio registrado satisfactoriamente.";

                await CargarListasDesplegablesAsync(model);
                return View(model);
            }
            catch (Exception)
            {
                // FE-01-CU018: Error al guardar en base de datos
                ViewBag.MensajeErrorConexion = "No fue posible conectar con el servicio de CampesinApp, por favor verifique su conexión a internet e intente de nuevo.";
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

            // Precondición: El usuario debe tener sesión activa
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
                model.Municipios = new System.Collections.Generic.List<SelectListItem>();
            }
        }
    }
}

