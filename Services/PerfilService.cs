using CampesinApp.Dtos;
using CampesinApp.Repositories.Models;
using Microsoft.EntityFrameworkCore;

namespace CampesinApp.Services
{
    /// <summary>
    /// Panel de configuración del perfil (CU-031). Dibuja únicamente los botones del rol autenticado (RN-01-CU031).
    /// Los botones de módulos que son de otros casos de uso quedan sin destino (UrlModal y UrlPagina nulos): cuando esos
    /// módulos existan, basta con asignarles aquí su URL.
    /// </summary>
    public class PerfilService
    {
        private const string CarpetaFotos = "images/perfiles";

        private readonly Campesinapp3Context _db;
        private readonly IWebHostEnvironment _entorno;

        public PerfilService(Campesinapp3Context db, IWebHostEnvironment entorno)
        {
            _db = db;
            _entorno = entorno;
        }

        /// <summary>Datos del usuario con sesión iniciada y sus botones. Lanza excepción si la base de datos falla (FE-01-CU031).</summary>
        public async Task<PerfilDto?> ObtenerAsync(int idUsuario)
        {
            var u = await _db.Usuarios
                .AsNoTracking()
                .Where(x => x.IdUsuario == idUsuario && x.Estado)
                .Select(x => new
                {
                    x.IdUsuario,
                    x.NombreCom,
                    x.ApellidoCom,
                    x.CorreoUsu,
                    x.TelelfonoUsu,
                    x.CedulaUsu,
                    x.Imagen,
                    TipoRol = x.IdRolNavigation.TipoRol,
                    Municipio = x.IdMunicipioNavigation.Municipio1
                })
                .FirstOrDefaultAsync();

            if (u == null) return null;

            var rol = RolUsuarioExtensions.Desde(u.TipoRol);
            if (rol == RolUsuario.Ninguno) return null;

            return new PerfilDto
            {
                Id = u.IdUsuario,
                Nombres = u.NombreCom,
                Apellidos = u.ApellidoCom,
                Correo = u.CorreoUsu,
                Telefono = u.TelelfonoUsu.ToString(),
                Cedula = u.CedulaUsu.ToString(),
                Rol = u.TipoRol,
                Municipio = u.Municipio,
                FotoUrl = ResolverFoto(u.Imagen),
                OpcionesGenerales = OpcionesGenerales(rol),
                OpcionesRol = OpcionesDelRol(rol, u.IdUsuario)
            };
        }

        /// <summary>Perfil que se consulta desde las tarjetas de la pantalla principal: datos básicos y calificaciones recibidas.</summary>
        public async Task<PerfilPublicoDto?> ObtenerPublicoAsync(int idUsuario, string rolEsperado)
        {
            var u = await _db.Usuarios
                .AsNoTracking()
                .Where(x => x.IdUsuario == idUsuario && x.Estado && x.IdRolNavigation.TipoRol == rolEsperado)
                .Select(x => new
                {
                    x.IdUsuario,
                    x.NombreCom,
                    x.ApellidoCom,
                    x.Imagen,
                    TipoRol = x.IdRolNavigation.TipoRol,
                    Municipio = x.IdMunicipioNavigation.Municipio1,
                    Departamento = x.IdMunicipioNavigation.IdDepartamentoNavigation.Departamento1
                })
                .FirstOrDefaultAsync();

            if (u == null) return null;

            var calificaciones = await _db.Calificacions
                .AsNoTracking()
                .Where(c => c.IdUsuarioEvaluado == idUsuario)
                .OrderByDescending(c => c.Fecha)
                .Select(c => new { c.Estrellas, c.Comentario })
                .ToListAsync();

            return new PerfilPublicoDto
            {
                Id = u.IdUsuario,
                NombreCompleto = $"{u.NombreCom} {u.ApellidoCom}".Trim(),
                Rol = u.TipoRol,
                Ubicacion = $"{u.Municipio} / {u.Departamento}",
                FotoUrl = ResolverFoto(u.Imagen),
                TotalCalificaciones = calificaciones.Count,
                PromedioEstrellas = calificaciones.Count == 0 ? null : Math.Round(calificaciones.Average(c => c.Estrellas), 1),
                Comentarios = calificaciones.Where(c => !string.IsNullOrWhiteSpace(c.Comentario)).Take(5).Select(c => c.Comentario).ToList()
            };
        }

        // ------------------------------------------------------------------------------------------ botones

        private static List<OpcionPerfilDto> OpcionesGenerales(RolUsuario rol)
        {
            var opciones = new List<OpcionPerfilDto>
            {
                new() { Texto = "Actualizar perfil", Icono = "bi-pencil-square" },
                new() { Texto = "Eliminar cuenta", Icono = "bi-person-x", Estilo = "peligro" },
                new() { Texto = "Puntos de acopio", Icono = "bi-geo-alt", UrlPagina = "/PuntoAcopio/Registrar" }
            };

            // CU-027: el transportador no consulta el historial de ventas.
            if (rol != RolUsuario.Transportador)
            {
                opciones.Add(new() { Texto = "Historial de transacciones", Icono = "bi-receipt", UrlModal = "/Historial/Transacciones" });
            }

            opciones.Add(new() { Texto = "Cerrar sesión", Icono = "bi-box-arrow-right", UrlModal = "/Sesion/Confirmar", Estilo = "principal" });
            return opciones;
        }

        private static List<OpcionPerfilDto> OpcionesDelRol(RolUsuario rol, int idUsuario) => rol switch
        {
            RolUsuario.Campesino => new List<OpcionPerfilDto>
            {
                new() { Texto = "Mi perfil", Icono = "bi-person-vcard", UrlPagina = $"/Perfil/ConsultaPerfilCampesino/{idUsuario}" },
                new() { Texto = "Perfil de pago", Icono = "bi-wallet2" },
                new() { Texto = "Registrar Punto de Acopio", Icono = "bi-pin-map", UrlPagina = "/PuntoAcopio/Registrar" },
                new() { Texto = "Historial ventas", Icono = "bi-receipt", UrlModal = "/Historial/Transacciones" },
                new() { Texto = "Mis pedidos", Icono = "bi-box-seam", UrlModal = "/Pedidos/Lista", Estilo = "principal" }
            },
            RolUsuario.Comprador => new List<OpcionPerfilDto>
            {
                new() { Texto = "Publicar demanda", Icono = "bi-megaphone" },
                new() { Texto = "Perfil de pago", Icono = "bi-wallet2" },
                new() { Texto = "Registrar Punto de Acopio", Icono = "bi-pin-map", UrlPagina = "/PuntoAcopio/Registrar" },
                new() { Texto = "Demandas", Icono = "bi-card-list", UrlPagina = "/catalogo/Index" },
                new() { Texto = "Historial de ventas", Icono = "bi-receipt", UrlModal = "/Historial/Transacciones" },
                new() { Texto = "Mis pedidos", Icono = "bi-box-seam", UrlModal = "/Pedidos/Lista", Estilo = "principal" }
            },
            RolUsuario.Transportador => new List<OpcionPerfilDto>
            {
                new() { Texto = "Generar Reporte de Ubicación", Icono = "bi-geo-alt-fill", UrlModal = "/Ubicacion/Reportar", Estilo = "principal" },
                new() { Texto = "Consultar Mis Viajes", Icono = "bi-truck", UrlModal = "/Transporte/MisViajes", Estilo = "principal" },
                new() { Texto = "Demandas", Icono = "bi-card-list", UrlPagina = "/catalogo/Index" }
            },
            RolUsuario.Administrador => new List<OpcionPerfilDto>
            {
                new() { Texto = "Registro Administrador", Icono = "bi-person-plus" },
                new() { Texto = "Cargar Datos DANE", Icono = "bi-cloud-upload" },
                new() { Texto = "Asignar Camión", Icono = "bi-truck-front", UrlPagina = "/AsignacionVehiculo/Index" },
                new() { Texto = "Gestión de Envíos", Icono = "bi-signpost-split", UrlModal = "/Transporte/Asignar", Estilo = "principal" },
                new() { Texto = "Generar Reporte de Ubicación", Icono = "bi-geo-alt-fill", UrlModal = "/Ubicacion/Reportar" },
                new() { Texto = "Registrar Punto de Acopio", Icono = "bi-pin-map", UrlPagina = "/PuntoAcopio/Registrar" },
                new() { Texto = "Historial Global del Aplicativo", Icono = "bi-journal-text", UrlModal = "/Historial/Global", Estilo = "principal" }
            },
            _ => new List<OpcionPerfilDto>()
        };

        /// <summary>La foto está en wwwroot/images/perfiles; si el archivo no existe se muestran las iniciales.</summary>
        private string ResolverFoto(string? nombreArchivo)
        {
            if (string.IsNullOrWhiteSpace(nombreArchivo)) return string.Empty;

            var nombre = Path.GetFileName(nombreArchivo);
            var ruta = Path.Combine(_entorno.WebRootPath ?? string.Empty, CarpetaFotos, nombre);
            return File.Exists(ruta) ? $"/{CarpetaFotos}/{nombre}" : string.Empty;
        }
    }
}
