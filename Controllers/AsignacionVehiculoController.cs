using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using CampesinApp.Repositories;
using CampesinApp.Services;
using CampesinApp.ViewModels;

namespace CampesinApp.Controllers
{
    /// <summary>
    /// Controlador para el Caso de Uso CU-020: ASIGNACIÓN DE VEHÍCULO.
    /// 
    /// REGLAS ARQUITECTÓNICAS:
    /// 1. NO accede directamente al DbContext. Utiliza exclusivamente IAsignacionVehiculoRepository.
    /// 2. Aplica las validaciones de negocio (RN-01-CU020 y RN-02-CU020).
    /// 3. Gestiona los flujos de excepción (FE01-CU020 y FE03-CU019).
    /// </summary>
    public class AsignacionVehiculoController : Controller
    {
        private readonly IAsignacionVehiculoRepository _repository;
        private readonly TransporteService _transporte;

        public AsignacionVehiculoController(IAsignacionVehiculoRepository repository, TransporteService transporte)
        {
            _repository = repository;
            _transporte = transporte;
        }

        // =========================================================================
        // PASO 2: LISTADO DE CARGAS PENDIENTES
        // =========================================================================
        /// <summary>
        /// Muestra la lista de cargas que requieren transporte (id_Camion == null).
        /// Cada elemento contiene hipervínculos con su ID y nombre de producto hacia el detalle.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // Validar precondición: Administrador autenticado
            if (!EsAdministradorAutenticado())
            {
                TempData["MensajeError"] = "Acceso restringido. Debe iniciar sesión como Administrador.";
                return RedirectToAction("Login", "InicioSesion");
            }

            try
            {
                var cargasPendientes = await _repository.ObtenerCargasPendientesAsync();

                var viewModel = new CargasPendientesViewModel
                {
                    Cargas = cargasPendientes.Select(d => new CargaPendienteItemViewModel
                    {
                        IdDemanda = d.IdDemanda,
                        NombreProducto = d.IdTipoProductoNavigation?.Producto ?? "Producto sin especificar",
                        Variedad = d.IdVariedadNavigation?.VariedadProd ?? "Estándar",
                        CantidadKg = d.CantidadDeman,
                        LugarDestino = $"{d.IdPuntoAcopioNavigation?.Direccion ?? "Punto de Acopio"} ({d.IdPuntoAcopioNavigation?.IdMunicipioNavigation?.Municipio1 ?? "Municipio"})",
                        FechaEntrega = d.FechaEntrega,
                        NombreComprador = $"{d.IdUsuarioNavigation?.NombreCom} {d.IdUsuarioNavigation?.ApellidoCom}".Trim()
                    }).ToList()
                };

                return View(viewModel);
            }
            catch (Exception)
            {
                // Flujo de excepción FE03-CU019: Fallo al consultar base de datos
                TempData["MensajeError"] = "Error al obtener la información actualizada de las cargas pendientes. Intente nuevamente.";
                return RedirectToAction("Index", "Perfil");
            }
        }

        // =========================================================================
        // PASO 4: DETALLE DEL PEDIDO Y SELECCIÓN DE CAMIÓN
        // =========================================================================
        /// <summary>
        /// Muestra el detalle centrado del pedido (origen, destino, cantidad, fecha, campesinos, comprador)
        /// y la tabla de camiones/conductores disponibles con documentos al día y capacidad suficiente.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Detalle(int id)
        {
            // Validar precondición: Administrador autenticado
            if (!EsAdministradorAutenticado())
            {
                TempData["MensajeError"] = "Acceso restringido. Debe iniciar sesión como Administrador.";
                return RedirectToAction("Login", "InicioSesion");
            }

            if (id <= 0)
            {
                TempData["MensajeError"] = "Identificador de carga no válido.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                // 1. Consultar demanda por ID
                var demanda = await _repository.ObtenerDemandaPorIdAsync(id);
                if (demanda == null)
                {
                    TempData["MensajeError"] = "El pedido seleccionado no fue encontrado o ya fue atendido.";
                    return RedirectToAction(nameof(Index));
                }

                if (demanda.IdCamion != null)
                {
                    TempData["MensajeError"] = $"El pedido #{id} ya tiene asignado un vehículo de transporte.";
                    return RedirectToAction(nameof(Index));
                }

                // 2. Consultar camiones disponibles que cumplen capacidad y documentos
                var camiones = await _repository.ObtenerCamionesDisponiblesAsync(demanda.CantidadDeman, id);

                // 3. Mapear al ViewModel
                var viewModel = new DetalleAsignacionViewModel
                {
                    IdDemanda = demanda.IdDemanda,
                    NombreProducto = demanda.IdTipoProductoNavigation?.Producto ?? "Producto sin especificar",
                    Variedad = demanda.IdVariedadNavigation?.VariedadProd ?? "Estándar",
                    CantidadKg = demanda.CantidadDeman,
                    PrecioUnitario = demanda.PrecioDeman,
                    LugarOrigen = !string.IsNullOrWhiteSpace(demanda.DireccionInicial) 
                        ? demanda.DireccionInicial 
                        : "Punto de Acopio Departamental",
                    LugarDestino = $"{demanda.IdPuntoAcopioNavigation?.Direccion ?? "Punto de Acopio"} - {demanda.IdPuntoAcopioNavigation?.IdMunicipioNavigation?.Municipio1 ?? ""}",
                    FechaEntrega = demanda.FechaEntrega,
                    NombreComprador = $"{demanda.IdUsuarioNavigation?.NombreCom} {demanda.IdUsuarioNavigation?.ApellidoCom}".Trim(),
                    TelefonoComprador = demanda.IdUsuarioNavigation != null && demanda.IdUsuarioNavigation.TelelfonoUsu > 0 
                        ? demanda.IdUsuarioNavigation.TelelfonoUsu.ToString() 
                        : "No disponible",
                    CampesinosParticipantes = demanda.AporteCamps.Select(a => new CampesinoAporteItemViewModel
                    {
                        IdUsuario = a.IdUsuario,
                        NombreCampesino = $"{a.IdUsuarioNavigation?.NombreCom} {a.IdUsuarioNavigation?.ApellidoCom}".Trim(),
                        KilosAportados = a.CantAporte,
                        Municipio = a.IdUsuarioNavigation?.IdMunicipioNavigation?.Municipio1 ?? "Región"
                    }).ToList(),
                    CamionesDisponibles = camiones.Select(c => new CamionDisponibleItemViewModel
                    {
                        IdCamion = c.IdCamion,
                        IdConductor = c.IdUsuario,
                        NombreConductor = $"{c.IdUsuarioNavigation?.NombreCom} {c.IdUsuarioNavigation?.ApellidoCom}".Trim(),
                        TelefonoConductor = c.IdUsuarioNavigation != null && c.IdUsuarioNavigation.TelelfonoUsu > 0 
                            ? c.IdUsuarioNavigation.TelelfonoUsu.ToString() 
                            : "No registrado",
                        Placa = c.PlacaCami,
                        Modelo = c.ModeloCami,
                        CapacidadKg = c.CapacidadCami,
                        EstadoPase = c.EstadoPase,
                        EstadoSeguro = c.EstadoSeguro,
                        EstadoTecno = c.EstadoTecno
                    }).ToList()
                };

                return View(viewModel);
            }
            catch (Exception)
            {
                TempData["MensajeError"] = "Error al consultar los datos del pedido y camiones disponibles.";
                return RedirectToAction(nameof(Index));
            }
        }

        // =========================================================================
        // PASO 8: VALIDACIÓN Y ASIGNACIÓN (POST)
        // =========================================================================
        /// <summary>
        /// Valida las reglas de negocio y persiste la asignación del camión:
        /// RN-01-CU020: Capacidad del camión >= Kilos demandados.
        /// RN-02-CU020: Documentos del conductor y vehículo al día (pase, seguro, tecnomecánica).
        /// Flujo FE01-CU020: Notifica si no cuenta con espacio suficiente o no está disponible.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Asignar(int idDemanda, int idCamion)
        {
            // Validar precondición: Administrador autenticado
            if (!EsAdministradorAutenticado())
            {
                TempData["MensajeError"] = "Acceso restringido. Debe iniciar sesión como Administrador.";
                return RedirectToAction("Login", "InicioSesion");
            }

            if (idDemanda <= 0 || idCamion <= 0)
            {
                TempData["MensajeError"] = "Por favor seleccione un conductor y camión válido.";
                return RedirectToAction(nameof(Detalle), new { id = idDemanda });
            }

            try
            {
                var demanda = await _repository.ObtenerDemandaPorIdAsync(idDemanda);
                if (demanda == null)
                {
                    TempData["MensajeError"] = "El pedido no existe o ya no está disponible.";
                    return RedirectToAction(nameof(Index));
                }

                var camion = await _repository.ObtenerCamionPorIdAsync(idCamion);
                if (camion == null)
                {
                    TempData["MensajeError"] = "El camión seleccionado no existe en el sistema.";
                    return RedirectToAction(nameof(Detalle), new { id = idDemanda });
                }

                // VALIDACIÓN RN-01-CU020: Espacio / Capacidad suficiente
                if (camion.CapacidadCami < demanda.CantidadDeman)
                {
                    // Flujo de excepción FE01-CU020
                    TempData["MensajeError"] = "El camión no tiene espacio suficiente o ya está ocupado en otro viaje.";
                    return RedirectToAction(nameof(Detalle), new { id = idDemanda });
                }

                // VALIDACIÓN RN-02-CU020: Documentos al día
                if (!camion.EstadoPase || !camion.EstadoSeguro || !camion.EstadoTecno)
                {
                    TempData["MensajeError"] = "El conductor o el vehículo no tienen los documentos vigentes al día.";
                    return RedirectToAction(nameof(Detalle), new { id = idDemanda });
                }

                // Persistir asignación en la base de datos
                bool exito = await _repository.AsignarCamionADemandaAsync(idDemanda, idCamion);
                if (!exito)
                {
                    TempData["MensajeError"] = "No fue posible guardar la asignación en la base de datos.";
                    return RedirectToAction(nameof(Detalle), new { id = idDemanda });
                }

                // Avisa al transportador, al comprador y a los campesinos del pedido (campana de notificaciones).
                try
                {
                    await _transporte.NotificarAsignacionAsync(idDemanda);
                }
                catch (Exception)
                {
                    // Un fallo al notificar no deshace la asignación que ya quedó guardada.
                }

                // Flujo básico Paso 8: Mensaje emergente durante 4 segundos "Asignación completa"
                TempData["MensajeExito"] = "Asignación completa";
                TempData["MostrarToast4Seg"] = true;

                return RedirectToAction(nameof(Index));
            }
            catch (Exception)
            {
                // Flujo de excepción de base de datos
                TempData["MensajeError"] = "Ocurrió un error inesperado al registrar la asignación. Intente de nuevo.";
                return RedirectToAction(nameof(Detalle), new { id = idDemanda });
            }
        }

        // =========================================================================
        // MÉTODOS PRIVADOS DE APOYO
        // =========================================================================
        private bool EsAdministradorAutenticado()
        {
            string? rol = HttpContext.Session.GetString("RolUsuario");
            // Permitimos acceso si el rol de sesión es Administrador
            return !string.IsNullOrEmpty(rol) && rol.Equals("Administrador", StringComparison.OrdinalIgnoreCase);
        }
    }
}

