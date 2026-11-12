using CampesinApp.Repositories;
using CampesinApp.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;

namespace CampesinApp.Controllers
{
    public class PantallaPrincipalController : Controller
    {
        private readonly PantallaPrincipalRepository _repo;

        public PantallaPrincipalController(PantallaPrincipalRepository repo)
        {
            _repo = repo;
        }

        [HttpGet]
        public IActionResult Index(string categoria = "", string variedad = "", string buscar = "", int pagina = 1)
        {
            string? rolSesion = HttpContext.Session.GetString("RolUsuario");
            string? cedulaSesion = HttpContext.Session.GetString("CedulaUsuario");

            if (string.IsNullOrEmpty(rolSesion))
            {
                return RedirectToAction("Login", "InicioSesion");
            }

            var model = new PantallaPrincipalViewModel
            {
                RolUsuario = rolSesion,
                CedulaUsuario = cedulaSesion ?? "No identificada",
                TerminoBusqueda = buscar,
                CategoriaSeleccionada = categoria,
                VariedadSeleccionada = variedad,
                PaginaActualPrecios = pagina
            };

            // 1. Cargar desplegables a través del repositorio
            try
            {
                model.ListaTiposProducto = _repo.ObtenerListaTiposProducto(categoria);
                model.ListaVariedades = _repo.ObtenerListaVariedadesPorTipo(categoria, variedad);
            }
            catch
            {
                model.ListaTiposProducto = new List<SelectListItem>();
                model.ListaVariedades = new List<SelectListItem>();
            }

            // 2. Obtener precios paginados estrictamente de 5 en 5
            try
            {
                int pageSize = 5; // <--- Límite estricto de 5 elementos por página
                var resultado = _repo.ObtenerPreciosPaginados(buscar, categoria, variedad, pagina, pageSize);

                model.TotalRegistrosPrecios = resultado.TotalRegistros;
                model.TotalPaginasPrecios = (int)Math.Ceiling(model.TotalRegistrosPrecios / (double)pageSize);
                if (model.TotalPaginasPrecios < 1) model.TotalPaginasPrecios = 1;

                if (pagina > model.TotalPaginasPrecios) pagina = model.TotalPaginasPrecios;
                if (pagina < 1) pagina = 1;

                model.PaginaActualPrecios = pagina;
                model.TablaPrecios = resultado.ListaPrecios;
            }
            catch (Exception)
            {
                model.MensajeExcepcionPrecios = "Los precios sugeridos para la jornada se encuentran en proceso de actualización. Intente nuevamente en unos minutos.";
            }

            // 3. Cargar sugerencias del aplicativo
            try
            {
                model.HaySugerenciasDisponibles = _repo.ExistenSugerencias();

                if (model.HaySugerenciasDisponibles)
                {
                    model.Camiones = _repo.ObtenerCamionesDisponibles();
                    model.CampesinosDestacados = _repo.ObtenerCampesinosDestacados();
                    model.CompradoresMayoristas = _repo.ObtenerCompradoresMayoristas();
                    model.CatalogoProductos = _repo.ObtenerCatalogoProductos();
                }
            }
            catch (Exception)
            {
                model.HaySugerenciasDisponibles = false;
            }

            return View("PantallaPrincipal", model);
        }
    }
}
