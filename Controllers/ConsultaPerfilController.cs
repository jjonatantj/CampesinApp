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
    /// Controlador exclusivo e independiente para CU-015: Consulta Perfil Campesino/Comprador.
    /// Permite consultar perfiles públicos y propios con pestañas (tabs), modales de reputación,
    /// filtrado dinámico por categoría y control estricto de privacidad de datos sensibles.
    /// Soporta roles dinámicos: Campesino ("Productos Cultivados") y Comprador ("Historial de ventas").
    /// </summary>
    [Route("ConsultaPerfil")]
    public class ConsultaPerfilController : Controller
    {
        private readonly Campesinapp3Context _context;
        private readonly ILogService _log;

        public ConsultaPerfilController(Campesinapp3Context context, ILogService log)
        {
            _context = context;
            _log = log;
        }

        /// <summary>
        /// Acción principal para consultar el perfil de un usuario por su Id y Rol.
        /// Rutas admitidas:
        /// - /ConsultaPerfil
        /// - /ConsultaPerfil/Index
        /// - /ConsultaPerfil/Index/{id}
        /// - /ConsultaPerfil/{id}
        /// - /ConsultaPerfil/Comprador/{id}
        /// - /ConsultaPerfil/Campesino/{id}
        /// </summary>
        [HttpGet("")]
        [HttpGet("Index")]
        [HttpGet("Index/{id:int}")]
        [HttpGet("{id:int}")]
        [HttpGet("Comprador/{id:int}")]
        [HttpGet("Campesino/{id:int}")]
        public async Task<IActionResult> Index(int? id, string? rol = null)
        {
            // RN-01-CU015: Usuario debe tener sesión activa.
            int? idSesion = HttpContext.Session.GetInt32("IdUsuario");
            if (idSesion == null)
            {
                TempData["MensajeError"] = "Su sesión ha expirado. Inicie sesión para ver el perfil.";
                return RedirectToAction("Login", "InicioSesion");
            }

            // Si no se envía Id válido, se consulta el perfil propio del usuario autenticado
            int idUsuarioConsultar = (id.HasValue && id.Value > 0) ? id.Value : idSesion.Value;

            try
            {
                // Consulta del usuario con sus relaciones geográficas, rol y actividad registrada
                var usuario = await _context.Usuarios
                    .Include(u => u.IdRolNavigation)
                    .Include(u => u.IdMunicipioNavigation)
                        .ThenInclude(m => m.IdDepartamentoNavigation)
                    .Include(u => u.Demanda)
                    .Include(u => u.Productos)
                    .FirstOrDefaultAsync(u => u.IdUsuario == idUsuarioConsultar);

                if (usuario == null)
                {
                    TempData["MensajeError"] = "El perfil de usuario consultado no existe en el sistema.";
                    return RedirectToAction("Index", "PantallaPrincipal");
                }

                // Determinación de privilegios de visualización (RN-02, RN-03, RN-04, RN-05)
                string? rolSesion = HttpContext.Session.GetString("RolUsuario");
                bool esAdmin = string.Equals(rolSesion, "Administrador", StringComparison.OrdinalIgnoreCase);
                bool esPerfilPropio = (idSesion.Value == idUsuarioConsultar);

                // ==============================================================
                // RESOLUCIÓN ROBUSTA DEL ROL A MOSTRAR (RN-06-CU015)
                // - Si se especificó explícitamente en la petición (ej. rol=Comprador desde el catálogo)
                // - Si el usuario en la BD tiene rol Comprador o tiene demandas registradas
                // - Si el usuario en la BD tiene rol Campesino o tiene productos registrados
                // ==============================================================
                string rolDetectado = usuario.IdRolNavigation?.TipoRol?.Trim() ?? "";
                var rolEnum = RolUsuarioExtensions.Desde(rolDetectado);

                // Detección automática por ruta si se usó /ConsultaPerfil/Comprador/{id} o /Campesino/{id}
                string? rutaActual = Request.Path.Value?.ToLowerInvariant();
                if (string.IsNullOrEmpty(rol) && rutaActual != null)
                {
                    if (rutaActual.Contains("/comprador/")) rol = "Comprador";
                    else if (rutaActual.Contains("/campesino/")) rol = "Campesino";
                }

                string rolVisualizar;
                if (!string.IsNullOrWhiteSpace(rol))
                {
                    var rolParamEnum = RolUsuarioExtensions.Desde(rol);
                    rolVisualizar = rolParamEnum switch
                    {
                        RolUsuario.Comprador => "Comprador",
                        RolUsuario.Campesino => "Campesino",
                        RolUsuario.Transportador => "Transportador",
                        RolUsuario.Administrador => "Administrador",
                        _ => rol.Trim()
                    };
                }
                else if (rolEnum == RolUsuario.Comprador || (usuario.Demanda.Any() && !usuario.Productos.Any()))
                {
                    rolVisualizar = "Comprador";
                }
                else if (rolEnum == RolUsuario.Campesino || usuario.Productos.Any())
                {
                    rolVisualizar = "Campesino";
                }
                else if (rolEnum == RolUsuario.Transportador)
                {
                    rolVisualizar = "Transportador";
                }
                else if (rolEnum == RolUsuario.Administrador)
                {
                    rolVisualizar = "Administrador";
                }
                else
                {
                    rolVisualizar = !string.IsNullOrEmpty(rolDetectado) ? rolDetectado : "Campesino";
                }

                string nombreCompleto = $"{usuario.NombreCom} {usuario.ApellidoCom}".Trim();

                // Construcción de iniciales si no tiene foto
                string iniciales = "U";
                if (!string.IsNullOrWhiteSpace(usuario.NombreCom))
                {
                    iniciales = usuario.NombreCom.Substring(0, 1).ToUpper();
                    if (!string.IsNullOrWhiteSpace(usuario.ApellidoCom))
                    {
                        iniciales += usuario.ApellidoCom.Substring(0, 1).ToUpper();
                    }
                }

                // Protección de datos sensibles (RN-02 vs RN-03 / RN-05)
                string? cedulaExacta = null;
                string? cedulaEnmascarada = null;
                string cedulaStr = usuario.CedulaUsu.ToString();

                if (esPerfilPropio || esAdmin)
                {
                    cedulaExacta = cedulaStr;
                    cedulaEnmascarada = cedulaStr;
                }
                else
                {
                    // Enmascarar cédula para público general (muestra solo los últimos 4 dígitos)
                    cedulaEnmascarada = cedulaStr.Length > 4
                        ? new string('*', cedulaStr.Length - 4) + cedulaStr.Substring(cedulaStr.Length - 4)
                        : "****";
                }

                var viewModel = new ConsultaPerfilViewModel
                {
                    IdUsuario = usuario.IdUsuario,
                    NombreCompleto = nombreCompleto,
                    Rol = rolVisualizar,
                    FotoUrl = !string.IsNullOrEmpty(usuario.Imagen) ? usuario.Imagen : null,
                    Iniciales = iniciales,
                    Correo = (esPerfilPropio || esAdmin) ? usuario.CorreoUsu : "***@***",
                    EsPerfilPropio = esPerfilPropio,
                    EsAdmin = esAdmin,
                    CedulaExacta = cedulaExacta,
                    CedulaEnmascarada = cedulaEnmascarada,
                    EstadoCuenta = usuario.Estado ? "Activo" : "Inactivo", // RN-05
                    Municipio = usuario.IdMunicipioNavigation?.Municipio1 ?? "Municipio no especificado",
                    Departamento = usuario.IdMunicipioNavigation?.IdDepartamentoNavigation?.Departamento1 ?? "Colombia",
                    Direccion = "Centro Poblado / Vereda",
                    Telefono = (esPerfilPropio || esAdmin)
                        ? (usuario.TelelfonoUsu > 0 ? usuario.TelelfonoUsu.ToString() : "No registrado")
                        : (usuario.TelelfonoUsu > 0 ? "***" + usuario.TelelfonoUsu.ToString()[^4..] : "Privado")
                };

                // ==============================================================
                // PESTAÑA 2: PRODUCTOS CULTIVADOS (Campesino) O HISTORIAL (Comprador)
                // (RN-03, RN-04, RN-06 - Cumplimiento exacto de los Mockups)
                // ==============================================================
                if (string.Equals(rolVisualizar, "Campesino", StringComparison.OrdinalIgnoreCase))
                {
                    viewModel.TituloPestana2 = "Productos Cultivados";

                    var productos = await _context.Productos
                        .Include(p => p.IdTipoProductoNavigation)
                        .Include(p => p.IdVariedadNavigation)
                        .Where(p => p.IdUsuario == usuario.IdUsuario)
                        .ToListAsync();

                    var precios = await _context.PrecioEstimados.ToListAsync();

                    foreach (var prod in productos)
                    {
                        var precioRef = precios.FirstOrDefault(pr =>
                            pr.IdTipoProducto == prod.IdTipoProducto &&
                            pr.IdVariedad == prod.IdVariedad);

                        viewModel.ItemsPestana2.Add(new ItemPerfilViewModel
                        {
                            Id = prod.IdProducto,
                            Nombre = prod.IdTipoProductoNavigation?.Producto ?? "Producto Agrícola",
                            Categoria = prod.IdTipoProductoNavigation?.Producto ?? "General",
                            Variedad = prod.IdVariedadNavigation?.VariedadProd ?? "Estándar",
                            PrecioEstimado = precioRef != null ? precioRef.PrecioEsti : 0,
                            UnidadMedida = "Kg",
                            Descripcion = prod.DescripcionProduc,
                            ImagenUrl = !string.IsNullOrEmpty(prod.ImagenProduc) ? prod.ImagenProduc : null
                        });
                    }
                }
                else
                {
                    // Perfil del Comprador: Pestaña "Historial de ventas" (Mockup 3)
                    viewModel.TituloPestana2 = "Historial de ventas";

                    var demandas = await _context.Demanda
                        .Include(d => d.IdTipoProductoNavigation)
                        .Include(d => d.IdVariedadNavigation)
                        .Where(d => d.IdUsuario == usuario.IdUsuario)
                        .OrderByDescending(d => d.IdDemanda)
                        .ToListAsync();

                    foreach (var dem in demandas)
                    {
                        viewModel.ItemsPestana2.Add(new ItemPerfilViewModel
                        {
                            Id = dem.IdDemanda,
                            Nombre = dem.IdTipoProductoNavigation?.Producto ?? "Solicitud de Compra",
                            Categoria = dem.IdTipoProductoNavigation?.Producto ?? "General",
                            Variedad = dem.IdVariedadNavigation?.VariedadProd ?? "Estándar",
                            PrecioEstimado = (decimal)dem.PrecioDeman,
                            UnidadMedida = "Kg",
                            Descripcion = dem.DescripcionDeman,
                            Fecha = dem.LimTiemDeman
                        });
                    }
                }

                // Categorías únicas para el filtro desplegable
                viewModel.CategoriasDisponibles = viewModel.ItemsPestana2
                    .Select(i => i.Categoria)
                    .Where(c => !string.IsNullOrWhiteSpace(c))
                    .Distinct()
                    .OrderBy(c => c)
                    .ToList();

                // ==============================================================
                // REPUTACIÓN Y CALIFICACIONES (Ventana Emergente / Modal)
                // ==============================================================
                List<Observacion> observaciones = new List<Observacion>();

                if (string.Equals(rolVisualizar, "Campesino", StringComparison.OrdinalIgnoreCase))
                {
                    var demandasAportadas = await _context.AporteCamps
                        .Where(a => a.IdUsuario == usuario.IdUsuario)
                        .Select(a => a.IdDemanda)
                        .Distinct()
                        .ToListAsync();

                    observaciones = await _context.Observacions
                        .Include(o => o.IdDemandaNavigation)
                        .Where(o => demandasAportadas.Contains(o.IdDemanda))
                        .OrderByDescending(o => o.HoraObser)
                        .Take(10)
                        .ToListAsync();
                }
                else
                {
                    observaciones = await _context.Observacions
                        .Include(o => o.IdDemandaNavigation)
                        .Where(o => o.IdDemandaNavigation.IdUsuario == usuario.IdUsuario)
                        .OrderByDescending(o => o.HoraObser)
                        .Take(10)
                        .ToListAsync();
                }

                if (observaciones.Any())
                {
                    viewModel.TotalResenas = observaciones.Count;
                    viewModel.CalificacionPromedio = Math.Round(observaciones.Average(o => o.CalificacionEstre), 1);

                    foreach (var obs in observaciones)
                    {
                        viewModel.Calificaciones.Add(new CalificacionItemViewModel
                        {
                            Autor = "Usuario Verificado",
                            RolAutor = string.Equals(rolVisualizar, "Campesino", StringComparison.OrdinalIgnoreCase) ? "Comprador" : "Campesino",
                            Estrellas = obs.CalificacionEstre,
                            Comentario = !string.IsNullOrWhiteSpace(obs.ObserComprador)
                                ? obs.ObserComprador
                                : (!string.IsNullOrWhiteSpace(obs.ObserTransportador) ? obs.ObserTransportador : "Excelente entrega y cumplimiento de los acuerdos acordados."),
                            Fecha = obs.HoraObser
                        });
                    }
                }
                else
                {
                    // Valores iniciales si no hay observaciones registradas aún
                    viewModel.CalificacionPromedio = 5.0;
                    viewModel.TotalResenas = 0;
                }

                return View("Index", viewModel);
            }
            catch (Exception ex)
            {
                // FE-02-CU015: Error de conexión a la base de datos
                await _log.RegistrarAsync(NivelesLog.Error, "CU-015 Consulta Perfil", $"{ex.GetType().Name}: {ex.Message}", idSesion);
                TempData["MensajeError"] = "No fue posible cargar la información del perfil. Intente nuevamente.";
                return RedirectToAction("Index", "PantallaPrincipal");
            }
        }
    }
}

