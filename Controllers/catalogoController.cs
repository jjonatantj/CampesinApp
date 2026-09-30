using Microsoft.AspNetCore.Mvc;
using CampesinApp.Repositories.Models;

namespace CampesinApp.Controllers
{
    /// <summary>
    /// Controlador encargado de gestionar el Catálogo de Demanda (CU-011).
    /// Permite a campesinos e invitados ver las solicitudes de compra de los clientes,
    /// filtrar por categoría, producto o región, y responder a una demanda (CU-012).
    /// </summary>
    public class catalogoController : Controller
    {
        // =========================================================================
        // ACCIÓN PRINCIPAL: Cargar y filtrar el catálogo (Pasos 1 al 4 del CU-011)
        // =========================================================================
        // Recibe por parámetro lo que el usuario seleccione en los filtros desplegables.
        // Si no selecciona nada, los parámetros llegan vacíos (null o "").
        public IActionResult Index(string? categoria, string? producto, string? region, bool simularError = false)
        {
            var viewModel = new CatalogoDemandaViewModel
            {
                CategoriaSeleccionada = categoria,
                ProductoSeleccionado = producto,
                RegionSeleccionada = region
            };

            try
            {
                // Flujo de Excepción FE-01-CU011:
                // Si ocurre un error con la base de datos (o activamos simularError=true para probarlo),
                // se captura en el bloque 'catch' inferior.
                if (simularError)
                {
                    throw new Exception("Error simulado de conexión con el servidor de base de datos.");
                }

                // 1. Obtenemos la lista de demandas (por ahora datos simulados en memoria)
                var todasLasDemandas = ObtenerDemandasSimuladas();

                // 2. Extraemos valores únicos para llenar las listas desplegables (dropdowns)
                viewModel.CategoriasDisponibles = todasLasDemandas
                    .Select(d => d.Categoria)
                    .Distinct()
                    .OrderBy(c => c)
                    .ToList();

                viewModel.ProductosDisponibles = todasLasDemandas
                    .Select(d => d.Producto)
                    .Distinct()
                    .OrderBy(p => p)
                    .ToList();

                viewModel.RegionesDisponibles = todasLasDemandas
                    .Select(d => d.Region)
                    .Distinct()
                    .OrderBy(r => r)
                    .ToList();

                // 3. Aplicamos los filtros de búsqueda seleccionados por el usuario
                var consulta = todasLasDemandas.AsEnumerable();

                // Filtro por Categoría
                if (!string.IsNullOrWhiteSpace(categoria))
                {
                    consulta = consulta.Where(d => d.Categoria.Equals(categoria, StringComparison.OrdinalIgnoreCase));
                }

                // Filtro por Producto
                if (!string.IsNullOrWhiteSpace(producto))
                {
                    consulta = consulta.Where(d => d.Producto.Equals(producto, StringComparison.OrdinalIgnoreCase));
                }

                // Filtro por Región
                if (!string.IsNullOrWhiteSpace(region))
                {
                    consulta = consulta.Where(d => d.Region.Equals(region, StringComparison.OrdinalIgnoreCase));
                }

                // Asignamos la lista filtrada al modelo de la vista
                viewModel.Demandas = consulta.ToList();
            }
            catch (Exception)
            {
                // FE-01-CU011: Error en base de datos
                // Se marca el error para que la vista muestre el mensaje requerido con el botón "Aceptar"
                viewModel.TieneErrorConexion = true;
                viewModel.MensajeError = "No fue posible cargar el catálogo de demanda. Intente más tarde";
                viewModel.Demandas = new List<Demanda>();
            }

            // Enviamos el ViewModel a la vista Views/catalogo/Index.cshtml
            return View(viewModel);
        }

        // =========================================================================
        // PASO 5: Conexión con CU-012 "RESPONDER A DEMANDA DE PRODUCTO"
        // =========================================================================
        public IActionResult Responder(int id)
        {
            // RN01-CU011: Los usuarios invitados no pueden realizar reservas sin autenticarse.
            // Aquí puedes validar la sesión en el futuro cuando implementes CU-012.
            TempData["MensajeExito"] = $"Has seleccionado la demanda #{id}. Aquí se conectará el caso de uso CU-012 (Responder Demanda).";
            return RedirectToAction(nameof(Index));
        }

        // =========================================================================
        // DATOS DE PRUEBA SIMULADOS
        // =========================================================================
        // Como aún estás iniciando y aprendiendo, estos datos en memoria te permiten
        // ver la interfaz en funcionamiento de inmediato sin depender de una BD externa.
        private List<Demanda> ObtenerDemandasSimuladas()
        {
            return new List<Demanda>
            {
                new Demanda
                {
                    Id = 1,
                    Titulo = "Demanda urgente de Papa Pastusa lavada",
                    Comprador = "Distribuidora Central de Abastos",
                    Categoria = "Tubérculos",
                    Producto = "Papa Pastusa",
                    Region = "Cundinamarca",
                    CantidadRequerida = "4.500 Kilos",
                    TipoCamion = "Camión Turbo (Capacidad 4.5 Ton)",
                    PrecioOfrecido = "$ 2.200 / Kg",
                    FechaPublicacion = DateTime.Now.AddDays(-1),
                    Descripcion = "Requerimos papa de primera calidad, empacada en bultos de 50 kg para entrega inmediata."
                },
                new Demanda
                {
                    Id = 2,
                    Titulo = "Compra de Tomate Chonto maduro",
                    Comprador = "Supermercados El Ahorro",
                    Categoria = "Hortalizas",
                    Producto = "Tomate Chonto",
                    Region = "Boyacá",
                    CantidadRequerida = "350 Canastillas",
                    TipoCamion = "Camión Sencillo (Capacidad 8 Ton)",
                    PrecioOfrecido = "$ 38.000 / Canastilla",
                    FechaPublicacion = DateTime.Now.AddDays(-2),
                    Descripcion = "Tomate pintón o maduro, en canastillas plásticas estándar limpias."
                },
                new Demanda
                {
                    Id = 3,
                    Titulo = "Lote de Aguacate Hass para exportación",
                    Comprador = "Exportadora Frutas de Colombia",
                    Categoria = "Frutas",
                    Producto = "Aguacate Hass",
                    Region = "Antioquia",
                    CantidadRequerida = "10 Toneladas",
                    TipoCamion = "Camión Doble Troque (Refrigerado)",
                    PrecioOfrecido = "$ 5.500 / Kg",
                    FechaPublicacion = DateTime.Now.AddHours(-10),
                    Descripcion = "Fruta con certificado BPA o registro predio exportador ICA. Calibre 14 a 22."
                },
                new Demanda
                {
                    Id = 4,
                    Titulo = "Requerimiento de Cebolla Junca fresca",
                    Comprador = "Agrocomercial La Sabana",
                    Categoria = "Hortalizas",
                    Producto = "Cebolla Junca",
                    Region = "Boyacá",
                    CantidadRequerida = "150 Cargas",
                    TipoCamion = "Camión Turbo",
                    PrecioOfrecido = "$ 85.000 / Carga",
                    FechaPublicacion = DateTime.Now.AddDays(-3),
                    Descripcion = "Cebolla descolada y lavada, atada según norma tradicional."
                },
                new Demanda
                {
                    Id = 5,
                    Titulo = "Plátano Hartón Verde por toneladas",
                    Comprador = "Procesadora de Alimentos del Valle",
                    Categoria = "Frutas",
                    Producto = "Plátano Hartón",
                    Region = "Valle del Cauca",
                    CantidadRequerida = "12 Toneladas",
                    TipoCamion = "Tractomula (Capacidad 30 Ton)",
                    PrecioOfrecido = "$ 1.800 / Kg",
                    FechaPublicacion = DateTime.Now.AddDays(-4),
                    Descripcion = "Plátano verde de buen grosor para elaboración de snacks y congelados."
                },
                new Demanda
                {
                    Id = 6,
                    Titulo = "Zanahoria seleccionada tamaño estándar",
                    Comprador = "Verduras y Frutas El Campesino",
                    Categoria = "Tubérculos",
                    Producto = "Zanahoria",
                    Region = "Cundinamarca",
                    CantidadRequerida = "200 Bultos",
                    TipoCamion = "Camión Turbo",
                    PrecioOfrecido = "$ 65.000 / Bulto",
                    FechaPublicacion = DateTime.Now.AddHours(-5),
                    Descripcion = "Zanahoria lavada de primera, empacada en costales limpios."
                }
            };
        }
    }
}

