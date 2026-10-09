using CampesinApp.Dtos;
using CampesinApp.Services;
using CampesinApp.Repositories.Models;
using Microsoft.AspNetCore.Mvc;

namespace CampesinApp.Controllers
{
    /// <summary>Consulta de datos del transportador (CU-023) y registro de datos logísticos / mis viajes (CU-024).</summary>
    public class TransporteController : AppController
    {
        private readonly TransporteService _transporte;

        public TransporteController(IUsuarioActualService usuarioActual, TransporteService transporte) : base(usuarioActual)
        {
            _transporte = transporte;
        }

        // ---------------------------------------------------------------- CU-023

        [HttpGet]
        [RolesPermitidos(RolUsuario.Campesino, RolUsuario.Comprador, RolUsuario.Transportador, RolUsuario.Administrador)]
        public async Task<IActionResult> Detalle(int idDemanda)
        {
            var usuario = (await ObtenerUsuarioAsync())!;
            var resultado = await _transporte.ObtenerDetalleAsync(usuario, idDemanda);
            if (!resultado.Exitoso)
            {
                return AvisoDeError(resultado); // FE-01-CU023 / FE-2-CU023
            }
            return ModalSuperpuesto("_DetalleTransporte", resultado.Datos);
        }

        /// <summary>
        /// Detalle logístico de un camión, desde "Ver detalles del flete" de la pantalla principal (página completa con "Regresar").
        /// </summary>
        [HttpGet("Transporte/ConsultaDatosTransporte/{id:int}")]
        public async Task<IActionResult> ConsultaDatosTransporte(int id)
        {
            var usuario = await ObtenerUsuarioAsync();
            if (usuario == null)
            {
                return RedirectToAction("Login", "InicioSesion");
            }

            // CU-023: los actores son campesino, comprador y administrador.
            if (usuario.Rol == RolUsuario.Transportador)
            {
                return View("SinPermisoFlete");
            }

            var camion = await _transporte.ObtenerCamionAsync(id);
            return camion == null ? View("SinPermisoFlete") : View(camion);
        }

        /// <summary>FA-01-CU023: búsqueda del administrador por placa o código de pedido, desde su pantalla principal.</summary>
        [HttpGet]
        [RolesPermitidos(RolUsuario.Administrador)]
        public async Task<IActionResult> Buscar(string? texto)
        {
            var usuario = (await ObtenerUsuarioAsync())!;
            var idDemanda = await _transporte.BuscarPedidoAsync(texto);
            if (idDemanda == null)
            {
                return Aviso("No se encontró ningún transporte con esa placa o código de pedido.", TipoAviso.Advertencia);
            }

            var resultado = await _transporte.ObtenerDetalleAsync(usuario, idDemanda.Value);
            if (!resultado.Exitoso)
            {
                return AvisoDeError(resultado);
            }

            resultado.Datos!.DesdeBusqueda = true;
            return ModalPrincipal("_DetalleTransporte", resultado.Datos);
        }

        // ---------------------------------------------------------------- CU-024 (administrador)

        [HttpGet]
        [RolesPermitidos(RolUsuario.Administrador)]
        public async Task<IActionResult> Asignar()
        {
            var dto = new AsignacionTransporteDto();
            await _transporte.CargarOpcionesAsignacionAsync(dto);
            return ModalPrincipal("_AsignacionForm", dto);
        }

        [HttpPost]
        [RolesPermitidos(RolUsuario.Administrador)]
        public async Task<IActionResult> Asignar(AsignacionTransporteDto dto)
        {
            var usuario = (await ObtenerUsuarioAsync())!;
            await _transporte.CargarOpcionesAsignacionAsync(dto);

            // FE-01-CU024: se resaltan las casillas vacías.
            ModelState.Clear();
            if (dto.IdDemanda == null) ModelState.AddModelError(nameof(dto.IdDemanda), "Campo obligatorio");
            if (dto.IdTransportador == null) ModelState.AddModelError(nameof(dto.IdTransportador), "Campo obligatorio");
            if (string.IsNullOrWhiteSpace(dto.Telefono)) ModelState.AddModelError(nameof(dto.Telefono), "Campo obligatorio");
            if (string.IsNullOrWhiteSpace(dto.Placa)) ModelState.AddModelError(nameof(dto.Placa), "Campo obligatorio");
            if (string.IsNullOrWhiteSpace(dto.Modelo)) ModelState.AddModelError(nameof(dto.Modelo), "Campo obligatorio");
            if (dto.Capacidad == null) ModelState.AddModelError(nameof(dto.Capacidad), "Campo obligatorio");

            if (!ModelState.IsValid)
            {
                ViewData["FaltanCampos"] = true;
                return ModalPrincipal("_AsignacionForm", dto);
            }

            var resultado = await _transporte.AsignarAsync(usuario, dto);
            if (!resultado.Exitoso)
            {
                ViewData["ErrorNegocio"] = resultado.Mensaje;
                return ModalPrincipal("_AsignacionForm", dto);
            }

            return Aviso(resultado.Mensaje!, TipoAviso.Exito, cerrar: "todas");
        }

        // ---------------------------------------------------------------- CU-024 (transportador)

        [HttpGet]
        [RolesPermitidos(RolUsuario.Transportador)]
        public async Task<IActionResult> MisViajes()
        {
            var usuario = (await ObtenerUsuarioAsync())!;
            return ModalPrincipal("_MisViajes", await _transporte.ListarMisViajesAsync(usuario));
        }
    }
}
