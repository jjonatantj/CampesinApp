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
                .Select(c => new
                {
                    c.Estrellas,
                    c.Comentario,
                    c.Fecha,
                    Autor = c.IdUsuarioEvaluaNavigation.NombreCom + " " + c.IdUsuarioEvaluaNavigation.ApellidoCom,
                    RolAutor = c.IdUsuarioEvaluaNavigation.IdRolNavigation.TipoRol
                })
                .ToListAsync();

            var perfil = new PerfilPublicoDto
            {
                Id = u.IdUsuario,
                NombreCompleto = $"{u.NombreCom} {u.ApellidoCom}".Trim(),
                Rol = u.TipoRol,
                Ubicacion = $"{u.Municipio} / {u.Departamento}",
                FotoUrl = ResolverFoto(u.Imagen),
                TotalCalificaciones = calificaciones.Count,
                PromedioEstrellas = calificaciones.Count == 0 ? null : Math.Round(calificaciones.Average(c => c.Estrellas), 1),
                Comentarios = calificaciones.Where(c => !string.IsNullOrWhiteSpace(c.Comentario)).Take(5).Select(c => c.Comentario).ToList(),
                Opiniones = calificaciones
                    .Where(c => !string.IsNullOrWhiteSpace(c.Comentario))
                    .Take(6)
                    .Select(c => new OpinionPerfilDto
                    {
                        Autor = c.Autor.Trim(),
                        RolAutor = c.RolAutor,
                        Estrellas = c.Estrellas,
                        Texto = c.Comentario,
                        Fecha = c.Fecha
                    }).ToList()
            };

            for (var estrellas = 1; estrellas <= 5; estrellas++)
            {
                perfil.Distribucion[estrellas - 1] = calificaciones.Count(c => c.Estrellas == estrellas);
            }

            // Cifras y secciones propias de cada rol (solo lectura de la base de datos).
            switch (RolUsuarioExtensions.Desde(rolEsperado))
            {
                case RolUsuario.Campesino:
                    await CompletarCampesinoAsync(perfil, idUsuario);
                    break;
                case RolUsuario.Comprador:
                    await CompletarCompradorAsync(perfil, idUsuario);
                    break;
                case RolUsuario.Transportador:
                    await CompletarTransportadorAsync(perfil, idUsuario);
                    break;
            }

            return perfil;
        }

        private async Task CompletarCampesinoAsync(PerfilPublicoDto perfil, int idUsuario)
        {
            var entregados = await _db.AporteCamps
                .AsNoTracking()
                .Where(a => a.IdUsuario == idUsuario && _db.Viajes.Any(v => v.IdDemanda == a.IdDemanda && EstadosEnvio.Finalizados.Contains(v.EstadoEnvio)))
                .Select(a => new { a.IdDemanda, a.CantAporte })
                .ToListAsync();

            var enCurso = await _db.AporteCamps
                .AsNoTracking()
                .Where(a => a.IdUsuario == idUsuario && !_db.Viajes.Any(v => v.IdDemanda == a.IdDemanda && EstadosEnvio.Finalizados.Contains(v.EstadoEnvio)))
                .Select(a => a.IdDemanda)
                .Distinct()
                .CountAsync();

            perfil.Productos = (await _db.Productos
                .AsNoTracking()
                .Where(p => p.IdUsuario == idUsuario)
                .OrderBy(p => p.IdTipoProductoNavigation.Producto)
                .Select(p => new
                {
                    Nombre = p.IdTipoProductoNavigation.Producto,
                    Variedad = p.IdVariedadNavigation.VariedadProd,
                    p.DescripcionProduc,
                    p.UbicacionCultivo
                })
                .ToListAsync())
                .Select(p => new ProductoPerfilDto { Nombre = p.Nombre, Variedad = p.Variedad, Descripcion = p.DescripcionProduc, Cultivo = p.UbicacionCultivo })
                .ToList();

            perfil.Puntos = (await _db.PuntoAcopios
                .AsNoTracking()
                .Where(p => p.IdUsuarios.Any(x => x.IdUsuario == idUsuario))
                .OrderBy(p => p.NombrePunto)
                .ToListAsync())
                .Select(p => new PuntoPerfilDto { Nombre = p.NombrePunto, Direccion = p.Direccion, Horario = p.HorarioAtencion, AccesoVehicular = p.AccesoVehicular })
                .ToList();

            perfil.Estadisticas = new List<DatoPerfilDto>
            {
                new() { Titulo = "Ventas completadas", Valor = Formato.Entero(entregados.Select(e => e.IdDemanda).Distinct().Count()), Icono = "bi-bag-check-fill" },
                new() { Titulo = "Producto entregado", Valor = $"{Formato.Entero(entregados.Sum(e => e.CantAporte))} kg", Icono = "bi-box-seam-fill" },
                new() { Titulo = "Pedidos en curso", Valor = Formato.Entero(enCurso), Icono = "bi-hourglass-split" },
                new() { Titulo = "Productos publicados", Valor = Formato.Entero(perfil.Productos.Count), Icono = "bi-basket2-fill" }
            };
        }

        private async Task CompletarCompradorAsync(PerfilPublicoDto perfil, int idUsuario)
        {
            var pedidos = await _db.Demanda
                .AsNoTracking()
                .Where(d => d.IdUsuario == idUsuario)
                .Select(d => new
                {
                    d.IdDemanda,
                    d.CantidadDeman,
                    Producto = d.IdTipoProductoNavigation.Producto + " " + d.IdVariedadNavigation.VariedadProd,
                    Finalizado = _db.Viajes.Any(v => v.IdDemanda == d.IdDemanda && EstadosEnvio.Finalizados.Contains(v.EstadoEnvio))
                })
                .ToListAsync();

            var completados = pedidos.Where(p => p.Finalizado).ToList();
            perfil.ProductosDeInteres = pedidos.Select(p => p.Producto.Trim()).Distinct().OrderBy(p => p).ToList();

            perfil.Estadisticas = new List<DatoPerfilDto>
            {
                new() { Titulo = "Pedidos realizados", Valor = Formato.Entero(pedidos.Count), Icono = "bi-cart-check-fill" },
                new() { Titulo = "Compras completadas", Valor = Formato.Entero(completados.Count), Icono = "bi-patch-check-fill" },
                new() { Titulo = "Volumen comprado", Valor = $"{Formato.Entero(completados.Sum(p => p.CantidadDeman))} kg", Icono = "bi-box-seam-fill" },
                new() { Titulo = "Pedidos en curso", Valor = Formato.Entero(pedidos.Count - completados.Count), Icono = "bi-hourglass-split" }
            };
        }

        private async Task CompletarTransportadorAsync(PerfilPublicoDto perfil, int idUsuario)
        {
            var viajes = await _db.Viajes
                .AsNoTracking()
                .Where(v => v.IdUsuarioTransportador == idUsuario)
                .Select(v => v.EstadoEnvio)
                .ToListAsync();

            perfil.Vehiculos = (await _db.Camions
                .AsNoTracking()
                .Where(c => c.IdUsuario == idUsuario)
                .OrderBy(c => c.PlacaCami)
                .ToListAsync())
                .Select(c => new VehiculoPerfilDto
                {
                    Placa = c.PlacaCami,
                    Modelo = c.ModeloCami,
                    CapacidadToneladas = c.CapacidadCami,
                    PaseAlDia = c.EstadoPase,
                    SeguroAlDia = c.EstadoSeguro,
                    TecnomecanicaAlDia = c.EstadoTecno
                }).ToList();

            var completados = viajes.Count(e => EstadosEnvio.Finalizados.Contains(e));
            perfil.Estadisticas = new List<DatoPerfilDto>
            {
                new() { Titulo = "Viajes completados", Valor = Formato.Entero(completados), Icono = "bi-truck" },
                new() { Titulo = "Viajes en curso", Valor = Formato.Entero(viajes.Count - completados), Icono = "bi-signpost-split-fill" },
                new() { Titulo = "Vehículos registrados", Valor = Formato.Entero(perfil.Vehiculos.Count), Icono = "bi-truck-front-fill" },
                new() { Titulo = "Capacidad total", Valor = $"{Formato.Entero(perfil.Vehiculos.Sum(v => v.CapacidadToneladas))} t", Icono = "bi-speedometer2" }
            };
        }

        // ------------------------------------------------------------------------------------------ botones

        private static List<OpcionPerfilDto> OpcionesGenerales(RolUsuario rol)
        {
            var opciones = new List<OpcionPerfilDto>
            {
                new() { Texto = "Actualizar perfil", Icono = "bi-pencil-square" },
                new() { Texto = "Eliminar cuenta", Icono = "bi-person-x", Estilo = "peligro", UrlPagina = "/EliminarUsuario" },
                new() { Texto = "Puntos de acopio", Icono = "bi-geo-alt", UrlPagina = "/PuntoAcopio/Registrar"  }
            };

            // CU-027: el transportador no consulta el historial de ventas.
            if (rol != RolUsuario.Transportador)
            {
                opciones.Add(new() { Texto = "Historial de transacciones", Icono = "bi-receipt", UrlPagina = "/Historial/Transacciones" });
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
                new() { Texto = "Historial ventas", Icono = "bi-receipt", UrlPagina = "/Historial/Transacciones" },
                new() { Texto = "Mis pedidos", Icono = "bi-box-seam", UrlModal = "/Pedidos/Lista", Estilo = "principal" }
            },
            RolUsuario.Comprador => new List<OpcionPerfilDto>
            {
                new() { Texto = "Publicar demanda", Icono = "bi-megaphone" },
                new() { Texto = "Perfil de pago", Icono = "bi-wallet2" },
                new() { Texto = "Registrar Punto de Acopio", Icono = "bi-pin-map", UrlPagina = "/PuntoAcopio/Registrar" },
                new() { Texto = "Demandas", Icono = "bi-card-list", UrlPagina = "/catalogo/Index" },
                new() { Texto = "Historial de ventas", Icono = "bi-receipt", UrlPagina = "/Historial/Transacciones" },
                new() { Texto = "Mis pedidos", Icono = "bi-box-seam", UrlModal = "/Pedidos/Lista", Estilo = "principal" }
            },
            RolUsuario.Transportador => new List<OpcionPerfilDto>
            {
                new() { Texto = "Generar Reporte de Ubicación", Icono = "bi-geo-alt-fill", UrlPagina = "/Ubicacion/Reportar", Estilo = "principal" },
                new() { Texto = "Consultar Mis Viajes", Icono = "bi-truck", UrlModal = "/Transporte/MisViajes", Estilo = "principal" },
                new() { Texto = "Demandas", Icono = "bi-card-list", UrlPagina = "/catalogo/Index" }
            },
            RolUsuario.Administrador => new List<OpcionPerfilDto>
            {
                new() { Texto = "Registro Administrador", Icono = "bi-person-plus", UrlPagina = "/RegistroAdmin" },
                new() { Texto = "Cargar Datos DANE", Icono = "bi-cloud-upload" },
                new() { Texto = "Asignar Camión", Icono = "bi-truck-front", UrlPagina = "/AsignacionVehiculo/Index" },
                new() { Texto = "Gestión de Envíos", Icono = "bi-signpost-split", UrlModal = "/Transporte/Asignar", Estilo = "principal" },
                new() { Texto = "Generar Reporte de Ubicación", Icono = "bi-geo-alt-fill", UrlPagina = "/Ubicacion/Reportar" },
                new() { Texto = "Registrar Punto de Acopio", Icono = "bi-pin-map", UrlPagina = "/PuntoAcopio/Registrar" },
                new() { Texto = "Historial Global del Aplicativo", Icono = "bi-journal-text", UrlPagina = "/Historial/Global", Estilo = "principal" }
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
