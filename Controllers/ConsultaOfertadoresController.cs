using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CampesinApp.Repositories.Models;
using CampesinApp.Services;
using CampesinApp.ViewModels;

namespace CampesinApp.Controllers
{
    /// <summary>
    /// Controlador exclusivo e independiente para CU-016: Consulta de Ofertadores.
    /// Permite a Compradores, Transportadores y Administradores consultar las demandas
    /// y ver el detalle de los campesinos que han aportado productos a cada una.
    /// </summary>
    [Route("ConsultaOfertadores")]
    public class ConsultaOfertadoresController : Controller
    {
        private readonly Campesinapp3Context _context;
        private readonly ILogService _log;

        public ConsultaOfertadoresController(Campesinapp3Context context, ILogService log)
        {
            _context = context;
            _log = log;
        }

        /// <summary>
        /// Vista principal de CU-016: Lista de demandas y visualización de ofertadores.
        /// Rutas admitidas:
        /// - /ConsultaOfertadores
        /// - /ConsultaOfertadores/Index
        /// - /ConsultaOfertadores/Index?idDemanda=X
        /// </summary>
        [HttpGet("")]
        [HttpGet("Index")]
        public async Task<IActionResult> Index(int? idDemanda)
        {
            // Validación de sesión activa
            int? idSesion = HttpContext.Session.GetInt32("IdUsuario");
            string? rolSesion = HttpContext.Session.GetString("RolUsuario");

            if (idSesion == null)
            {
                TempData["MensajeError"] = "Debe iniciar sesión para acceder a la consulta de ofertadores.";
                return RedirectToAction("Login", "InicioSesion");
            }

            try
            {
                // ==============================================================
                // REGLAS DE NEGOCIO RN-01, RN-02 Y RN-03 (PERMISOS DE CONSULTA)
                // ==============================================================
                IQueryable<Demandum> query = _context.Demanda
                    .Include(d => d.IdTipoProductoNavigation)
                    .Include(d => d.IdVariedadNavigation)
                    .Include(d => d.AporteCamps)
                        .ThenInclude(a => a.IdUsuarioNavigation)
                            .ThenInclude(u => u.IdMunicipioNavigation)
                    .Include(d => d.AporteCamps)
                        .ThenInclude(a => a.IdPuntoAcopioNavigation)
                            .ThenInclude(p => p.IdMunicipioNavigation);

                if (string.Equals(rolSesion, "Comprador", StringComparison.OrdinalIgnoreCase))
                {
                    // RN-01-CU016: Comprador solo consulta demandas creadas bajo su cuenta
                    query = query.Where(d => d.IdUsuario == idSesion.Value);
                }
                else if (string.Equals(rolSesion, "Transportador", StringComparison.OrdinalIgnoreCase))
                {
                    // RN-03-CU016: Transportador solo visualiza demandas con su camión asignado
                    var camionesDelTransportador = await _context.Camions
                        .Where(c => c.IdUsuario == idSesion.Value)
                        .Select(c => c.IdCamion)
                        .ToListAsync();

                    query = query.Where(d => d.IdCamion.HasValue && camionesDelTransportador.Contains(d.IdCamion.Value));
                }
                else if (string.Equals(rolSesion, "Administrador", StringComparison.OrdinalIgnoreCase))
                {
                    // RN-02-CU016: Administrador tiene acceso global sin restricciones
                }
                else
                {
                    // El rol Campesino o Invitado no tiene precondición para consultar este módulo de administración
                    TempData["MensajeError"] = "Tu rol no tiene acceso a la consulta de ofertadores de demandas.";
                    return RedirectToAction("Index", "PantallaPrincipal");
                }

                var demandasDb = await query
                    .OrderByDescending(d => d.IdDemanda)
                    .ToListAsync();

                var viewModel = new ConsultaOfertadoresViewModel
                {
                    RolUsuario = rolSesion ?? "Usuario",
                    IdDemandaSeleccionada = idDemanda
                };

                foreach (var d in demandasDb)
                {
                    int totalAportado = d.AporteCamps.Sum(a => a.CantAporte);
                    int faltante = Math.Max(0, d.CantidadDeman - totalAportado);

                    var itemDemanda = new DemandaOfertadorItemViewModel
                    {
                        IdDemanda = d.IdDemanda,
                        NombreProducto = d.IdTipoProductoNavigation?.Producto ?? "Producto Agrícola",
                        Variedad = d.IdVariedadNavigation?.VariedadProd ?? "Estándar",
                        CantidadTotal = d.CantidadDeman,
                        CantidadAportada = totalAportado,
                        CantidadRestante = faltante,
                        FechaLimiteOfertar = d.LimTiemDeman,
                        FechaEntrega = d.FechaEntrega,
                        TotalOfertadores = d.AporteCamps.Count
                    };

                    // Mapeo de campesinos ofertadores
                    foreach (var ap in d.AporteCamps)
                    {
                        string ubicacion = "No especificada";
                        if (ap.IdPuntoAcopioNavigation != null)
                        {
                            string municipioPunto = ap.IdPuntoAcopioNavigation.IdMunicipioNavigation?.Municipio1 ?? "";
                            ubicacion = string.IsNullOrWhiteSpace(municipioPunto)
                                ? ap.IdPuntoAcopioNavigation.NombrePunto
                                : $"{ap.IdPuntoAcopioNavigation.NombrePunto} ({municipioPunto})";
                        }
                        else if (ap.IdUsuarioNavigation?.IdMunicipioNavigation != null)
                        {
                            ubicacion = ap.IdUsuarioNavigation.IdMunicipioNavigation.Municipio1;
                        }

                        itemDemanda.Ofertadores.Add(new OfertadorDetalleViewModel
                        {
                            IdAporte = ap.IdAporte,
                            IdCampesino = ap.IdUsuario,
                            NombreCampesino = $"{ap.IdUsuarioNavigation?.NombreCom} {ap.IdUsuarioNavigation?.ApellidoCom}".Trim(),
                            CantidadAportada = ap.CantAporte,
                            UbicacionOrigen = ubicacion
                        });
                    }

                    viewModel.Demandas.Add(itemDemanda);
                }

                return View("Index", viewModel);
            }
            catch (Exception ex)
            {
                // FE-02-CU016: Error de conexión a la base de datos
                await _log.RegistrarAsync(NivelesLog.Error, "CU-016 Consulta Ofertadores", $"{ex.GetType().Name}: {ex.Message}", idSesion);
                TempData["MensajeError"] = "No fue posible cargar la información. Intente nuevamente.";
                return RedirectToAction("Index", "PantallaPrincipal");
            }
        }
    }
}

