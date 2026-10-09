using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using CampesinApp.Repositories;
using CampesinApp.ViewModels;
using System;
using System.Collections.Generic;

namespace CampesinApp.Controllers
{
    /// <summary>
    /// Controlador de la Pantalla Principal – CU-009.
    /// Funciona exclusivamente con los datos reales de la BD y la sesión activa del usuario.
    /// </summary>
    public class PantallaPrincipalController : Controller
    {
        private readonly PantallaPrincipalRepository _repo;

        public PantallaPrincipalController(PantallaPrincipalRepository repo)
        {
            _repo = repo;
        }

        [HttpGet]
        public IActionResult Index(string buscar = "", string categoria = "")
        {
            // 1. Obtener datos reales de la sesión activa iniciada en InicioSesionController
            string? rolSesion = HttpContext.Session.GetString("RolUsuario");
            string? cedulaSesion = HttpContext.Session.GetString("CedulaUsuario");

            // Si el usuario no ha iniciado sesión, se redirige al Login
            if (string.IsNullOrEmpty(rolSesion))
            {
                return RedirectToAction("Login", "InicioSesion");
            }

            var model = new PantallaPrincipalViewModel
            {
                RolUsuario = rolSesion,
                CedulaUsuario = cedulaSesion ?? "No identificada",
                TerminoBusqueda = buscar,
                CategoriaSeleccionada = categoria
            };

            // 2. Cargar categorías reales desde la base de datos (tabla tipo_Producto)
            try
            {
                model.CategoriasDisponibles = _repo.ObtenerCategorias();
            }
            catch
            {
                model.CategoriasDisponibles = new List<string>();
            }

            // 3. Consultar la tabla de precios sugeridos de la BD (precio_Estimado)
            try
            {
                model.TablaPrecios = _repo.ObtenerPreciosDelDia(buscar, categoria);
            }
            catch (Exception)
            {
                // FE02-CU009: Mensaje de excepción por falla de conexión a la BD
                model.MensajeExcepcionPrecios = "Los precios sugeridos para la jornada se encuentran en proceso de actualización. Intente nuevamente en unos minutos.";
            }

            // 4. Cargar sugerencias destacadas reales desde la BD
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
