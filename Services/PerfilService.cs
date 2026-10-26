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

            var rolPerfil = RolUsuarioExtensions.Desde(rolEsperado);
            var calificaciones = await ObtenerCalificacionesAsync(idUsuario, rolPerfil);
            var conEstrellas = calificaciones.Where(c => c.Estrellas >= 1).ToList();

            var perfil = new PerfilPublicoDto
            {
                Id = u.IdUsuario,
                NombreCompleto = $"{u.NombreCom} {u.ApellidoCom}".Trim(),
                Rol = u.TipoRol,
                Ubicacion = $"{u.Municipio} / {u.Departamento}",
                FotoUrl = ResolverFoto(u.Imagen),
                TotalCalificaciones = conEstrellas.Count,
                PromedioEstrellas = conEstrellas.Count == 0 ? null : Math.Round(conEstrellas.Average(c => c.Estrellas), 1),
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
                perfil.Distribucion[estrellas - 1] = conEstrellas.Count(c => c.Estrellas == estrellas);
            }

            // Cifras y secciones propias de cada rol (solo lectura de la base de datos).
            switch (rolPerfil)
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

        private sealed record CalificacionRecibida(int Estrellas, string Comentario, DateTime Fecha, string Autor, string RolAutor);

        /// <summary>
        /// Calificaciones y comentarios que recibió el usuario, tomados de la tabla observacion: el campesino y el transportador
        /// reciben las estrellas y el comentario del comprador de sus pedidos; el comprador recibe los comentarios de los transportadores.
        /// </summary>
        private async Task<List<CalificacionRecibida>> ObtenerCalificacionesAsync(int idUsuario, RolUsuario rol)
        {
            IQueryable<int> pedidos = rol switch
            {
                RolUsuario.Campesino => _db.AporteCamps.Where(a => a.IdUsuario == idUsuario).Select(a => a.IdDemanda),
                RolUsuario.Transportador => _db.Demanda.Where(d => d.IdCamion != null && _db.Camions.Any(c => c.IdCamion == d.IdCamion && c.IdUsuario == idUsuario)).Select(d => d.IdDemanda),
                RolUsuario.Comprador => _db.Demanda.Where(d => d.IdUsuario == idUsuario).Select(d => d.IdDemanda),
                _ => _db.Demanda.Where(d => false).Select(d => d.IdDemanda)
            };

            var filas = await (
                    from o in _db.Observacions
                    where pedidos.Contains(o.IdDemanda)
                    join d in _db.Demanda on o.IdDemanda equals d.IdDemanda
                    orderby o.HoraObser descending
                    select new
                    {
                        o.CalificacionEstre,
                        o.ObserComprador,
                        o.ObserTransportador,
                        o.HoraObser,
                        Comprador = d.IdUsuarioNavigation.NombreCom + " " + d.IdUsuarioNavigation.ApellidoCom,
                        d.IdCamion
                    })
                .AsNoTracking()
                .ToListAsync();

            var idsCamion = filas.Where(f => f.IdCamion != null).Select(f => f.IdCamion!.Value).Distinct().ToList();
            var duenos = (await _db.Camions
                    .AsNoTracking()
                    .Where(c => idsCamion.Contains(c.IdCamion))
                    .Select(c => new { c.IdCamion, Nombre = c.IdUsuarioNavigation.NombreCom + " " + c.IdUsuarioNavigation.ApellidoCom })
                    .ToListAsync())
                .ToDictionary(c => c.IdCamion, c => c.Nombre);

            var lista = new List<CalificacionRecibida>();
            foreach (var f in filas)
            {
                if (rol == RolUsuario.Comprador)
                {
                    var comentario = CalificacionService.ParsearTransportador(f.ObserTransportador);
                    if (!string.IsNullOrWhiteSpace(comentario))
                    {
                        var autor = f.IdCamion != null && duenos.TryGetValue(f.IdCamion.Value, out var nombre) ? nombre : "Transportador";
                        lista.Add(new CalificacionRecibida(0, comentario, f.HoraObser, autor, "Transportador"));
                    }
                }
                else if (f.CalificacionEstre >= 1)
                {
                    var comentario = CalificacionService.ParsearComprador(f.ObserComprador).Comentario ?? string.Empty;
                    lista.Add(new CalificacionRecibida(f.CalificacionEstre, comentario, f.HoraObser, f.Comprador, "Comprador"));
                }
            }

            return lista;
        }

        /// <summary>Pedidos cuyo envío ya terminó (entregados, con o sin observaciones).</summary>
        private async Task<HashSet<int>> IdsPedidosFinalizadosAsync()
        {
            var ids = await _db.Viajes()
                .AsNoTracking()
                .Where(v => EstadosEnvio.Finalizados.Contains(v.EstadoEnvio))
                .Select(v => v.IdDemanda)
                .ToListAsync();
            return ids.ToHashSet();
        }

        private async Task CompletarCampesinoAsync(PerfilPublicoDto perfil, int idUsuario)
        {
            var finalizados = await IdsPedidosFinalizadosAsync();
            var aportes = await _db.AporteCamps
                .AsNoTracking()
                .Where(a => a.IdUsuario == idUsuario)
                .Select(a => new { a.IdDemanda, a.CantAporte })
                .ToListAsync();
            var entregados = aportes.Where(a => finalizados.Contains(a.IdDemanda)).ToList();
            var enCurso = aportes.Where(a => !finalizados.Contains(a.IdDemanda)).Select(a => a.IdDemanda).Distinct().Count();

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
            var finalizados = await IdsPedidosFinalizadosAsync();
            var pedidos = await _db.Demanda
                .AsNoTracking()
                .Where(d => d.IdUsuario == idUsuario)
                .Select(d => new
                {
                    d.IdDemanda,
                    d.CantidadDeman,
                    Producto = d.IdTipoProductoNavigation.Producto + " " + d.IdVariedadNavigation.VariedadProd
                })
                .ToListAsync();

            var completados = pedidos.Where(p => finalizados.Contains(p.IdDemanda)).ToList();
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
            var viajes = await _db.Viajes()
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
                new() { Texto = "Actualizar perfil", Icono = "bi-pencil-square", UrlPagina = "/ActualizarPerfil" },
                new() { Texto = "Eliminar cuenta", Icono = "bi-person-x", Estilo = "peligro", UrlPagina = "/EliminarUsuario" },
                new() { Texto = "Puntos de acopio", Icono = "bi-geo-alt", UrlPagina = "/PuntoAcopio/Index" }
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
                new() { Texto = "Perfil de pago", Icono = "bi-wallet2", UrlPagina = "/PerfilPago" },
                new() { Texto = "Registrar Punto de Acopio", Icono = "bi-pin-map", UrlPagina = "/PuntoAcopio/Registrar" },
                new() { Texto = "Historial ventas", Icono = "bi-receipt", UrlPagina = "/Historial/Transacciones" },
                new() { Texto = "Mis pedidos", Icono = "bi-box-seam", UrlModal = "/Pedidos/Lista", Estilo = "principal" }
            },
            RolUsuario.Comprador => new List<OpcionPerfilDto>
            {
                new() { Texto = "Publicar demanda", Icono = "bi-megaphone", UrlPagina = "/PublicarDemanda" },
                new() { Texto = "Perfil de pago", Icono = "bi-wallet2", UrlPagina = "/PerfilPago" },
                new() { Texto = "Registrar Punto de Acopio", Icono = "bi-pin-map", UrlPagina = "/PuntoAcopio/Registrar" },
                new() { Texto = "Demandas", Icono = "bi-card-list", UrlPagina = "/ConsultaOfertadores" },
                new() { Texto = "Historial de ventas", Icono = "bi-receipt", UrlPagina = "/Historial/Transacciones" },
                new() { Texto = "Mis pedidos", Icono = "bi-box-seam", UrlModal = "/Pedidos/Lista", Estilo = "principal" }
            },
            RolUsuario.Transportador => new List<OpcionPerfilDto>
            {
                new() { Texto = "Generar Reporte de Ubicación", Icono = "bi-geo-alt-fill", UrlPagina = "/Ubicacion/Reportar", Estilo = "principal" },
                new() { Texto = "Consultar Mis Viajes", Icono = "bi-truck", UrlModal = "/Transporte/MisViajes", Estilo = "principal" },
                new() { Texto = "Demandas", Icono = "bi-card-list", UrlPagina = "/ConsultaOfertadores" }
            },
            RolUsuario.Administrador => new List<OpcionPerfilDto>
            {
                new() { Texto = "Registro Administrador", Icono = "bi-person-plus", UrlPagina = "/RegistroAdmin" },
                new() { Texto = "Cargar Datos DANE", Icono = "bi-cloud-upload", UrlPagina = "/ImportarDatos" },
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
