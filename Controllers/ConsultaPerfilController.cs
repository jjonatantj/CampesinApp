using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
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
        private readonly IWebHostEnvironment _entorno;

        public ConsultaPerfilController(Campesinapp3Context context, ILogService log, IWebHostEnvironment entorno)
        {
            _context = context;
            _log = log;
            _entorno = entorno;
        }

        /// <summary>
        /// Valida si el archivo de foto existe físicamente en el servidor. Si no existe, retorna null para dibujar iniciales.
        /// </summary>
        private string? ResolverFoto(string? nombreArchivo)
        {
            if (string.IsNullOrWhiteSpace(nombreArchivo) || nombreArchivo.Trim().Equals("null", StringComparison.OrdinalIgnoreCase))
                return null;

            var nombreLimpio = nombreArchivo.Trim();
            var nombreSoloArchivo = Path.GetFileName(nombreLimpio);

            // 1. Verificar en wwwroot/images/perfiles/{nombre}
            var rutaEnPerfiles = Path.Combine(_entorno.WebRootPath ?? string.Empty, "images", "perfiles", nombreSoloArchivo);
            if (System.IO.File.Exists(rutaEnPerfiles))
            {
                return $"/images/perfiles/{nombreSoloArchivo}";
            }

            // 2. Verificar en la ruta directa relativa a wwwroot
            var rutaDirecta = Path.Combine(_entorno.WebRootPath ?? string.Empty, nombreLimpio.TrimStart('/', '\\'));
            if (System.IO.File.Exists(rutaDirecta))
            {
                return "/" + nombreLimpio.TrimStart('/', '\\');
            }

            // Si el archivo físico no existe en disco, retornar null para que se muestren las iniciales
            return null;
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
                    .AsSplitQuery()
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

                // Obtener dirección a partir del punto de acopio registrado por el usuario
                var puntoAcopio = await _context.PuntoAcopios
                    .Where(p => p.IdUsuario == usuario.IdUsuario || p.IdUsuarios.Any(u => u.IdUsuario == usuario.IdUsuario))
                    .OrderByDescending(p => p.FechaRegistro)
                    .FirstOrDefaultAsync();

                string direccionVisualizar = (!string.IsNullOrWhiteSpace(puntoAcopio?.Direccion))
                    ? puntoAcopio.Direccion.Trim()
                    : "No registra";

                var viewModel = new ConsultaPerfilViewModel
                {
                    IdUsuario = usuario.IdUsuario,
                    NombreCompleto = nombreCompleto,
                    Rol = rolVisualizar,
                    FotoUrl = ResolverFoto(usuario.Imagen),
                    Iniciales = iniciales,
                    Correo = (esPerfilPropio || esAdmin) ? usuario.CorreoUsu : "***@***",
                    EsPerfilPropio = esPerfilPropio,
                    EsAdmin = esAdmin,
                    CedulaExacta = cedulaExacta,
                    CedulaEnmascarada = cedulaEnmascarada,
                    EstadoCuenta = usuario.Estado ? "Activo" : "Inactivo", // RN-05
                    Municipio = usuario.IdMunicipioNavigation?.Municipio1 ?? "Municipio no especificado",
                    Departamento = usuario.IdMunicipioNavigation?.IdDepartamentoNavigation?.Departamento1 ?? "Colombia",
                    Direccion = direccionVisualizar,
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
                            Total = precioRef != null ? precioRef.PrecioEsti : 0,
                            UnidadMedida = "Kg",
                            Descripcion = prod.DescripcionProduc,
                            ImagenUrl = !string.IsNullOrEmpty(prod.ImagenProduc) ? prod.ImagenProduc : null,
                            UbicacionCultivo = !string.IsNullOrWhiteSpace(prod.UbicacionCultivo) ? prod.UbicacionCultivo : "Predio Campesino"
                        });
                    }
                }
                else
                {
                    // Perfil del Comprador: Pestaña "Historial de ventas" (Solo demandas terminadas / culminadas)
                    viewModel.TituloPestana2 = "Historial de ventas";

                    var demandas = await _context.Demanda
                        .Include(d => d.IdTipoProductoNavigation)
                        .Include(d => d.IdVariedadNavigation)
                        .Include(d => d.IdEstadoDemanNavigation)
                        .Where(d => d.IdUsuario == usuario.IdUsuario)
                        .Where(d => d.IdEstadoDeman == 4 ||
                                    (d.IdEstadoDemanNavigation != null && (
                                        d.IdEstadoDemanNavigation.Estado.ToLower().Contains("complet") ||
                                        d.IdEstadoDemanNavigation.Estado.ToLower().Contains("finaliz") ||
                                        d.IdEstadoDemanNavigation.Estado.ToLower().Contains("culmin") ||
                                        d.IdEstadoDemanNavigation.Estado.ToLower().Contains("terminad")
                                    )))
                        .OrderByDescending(d => d.IdDemanda)
                        .ToListAsync();

                    foreach (var dem in demandas)
                    {
                        // En la base de datos, en PrecioDeman ya está el TOTAL pagado de la demanda
                        decimal totalCompra = (decimal)dem.PrecioDeman;
                        int cantidadKilos = dem.CantidadDeman;
                        decimal precioPorKg = cantidadKilos > 0 ? Math.Round(totalCompra / cantidadKilos, 0) : totalCompra;

                        viewModel.ItemsPestana2.Add(new ItemPerfilViewModel
                        {
                            Id = dem.IdDemanda,
                            Nombre = dem.IdTipoProductoNavigation?.Producto ?? "Solicitud de Compra",
                            Categoria = dem.IdTipoProductoNavigation?.Producto ?? "General",
                            Variedad = dem.IdVariedadNavigation?.VariedadProd ?? "Estándar",
                            PrecioEstimado = precioPorKg,
                            Cantidad = cantidadKilos,
                            Total = totalCompra,
                            UnidadMedida = "Kg",
                            Descripcion = dem.DescripcionDeman,
                            Fecha = dem.FechaEntrega != default ? dem.FechaEntrega : dem.LimTiemDeman,
                            Estado = dem.IdEstadoDemanNavigation?.Estado ?? "Completada"
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
                    viewModel.CalificacionPromedio = 0.0;
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

