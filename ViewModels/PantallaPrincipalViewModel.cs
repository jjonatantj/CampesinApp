using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CampesinApp.ViewModels
{
    public class PantallaPrincipalViewModel
    {
        public string RolUsuario { get; set; } = "Campesino";
        public string CedulaUsuario { get; set; } = string.Empty;

        // Búsqueda y Filtros
        public string TerminoBusqueda { get; set; } = string.Empty;
        public string CategoriaSeleccionada { get; set; } = string.Empty;
        public string VariedadSeleccionada { get; set; } = string.Empty;

        // Listas de texto (por si se usan en otras partes)
        public List<string> CategoriasDisponibles { get; set; } = new List<string>();
        public List<string> VariedadesDisponibles { get; set; } = new List<string>();

        // Listas desplegables oficiales tipo SelectListItem
        public List<SelectListItem> ListaTiposProducto { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> ListaVariedades { get; set; } = new List<SelectListItem>();

        // Paginación (5 resultados por página)
        public int PaginaActualPrecios { get; set; } = 1;
        public int TotalPaginasPrecios { get; set; } = 1;
        public int TotalRegistrosPrecios { get; set; } = 0;

        // Mensajes y Banderas
        public string MensajeExcepcionPrecios { get; set; } = string.Empty;
        public bool HaySugerenciasDisponibles { get; set; } = true;

        // Listados
        public List<PrecioSugeridoItem> TablaPrecios { get; set; } = new List<PrecioSugeridoItem>();
        public List<CamionSugerenciaItem> Camiones { get; set; } = new List<CamionSugerenciaItem>();
        public List<CampesinoDestacadoItem> CampesinosDestacados { get; set; } = new List<CampesinoDestacadoItem>();
        public List<CompradorMayoristaItem> CompradoresMayoristas { get; set; } = new List<CompradorMayoristaItem>();
        public List<ProductoOfertaItem> CatalogoProductos { get; set; } = new List<ProductoOfertaItem>();
    }

    public class PrecioSugeridoItem
    {
        public string Producto { get; set; } = string.Empty;
        public string Variedad { get; set; } = string.Empty;
        public string Categoria { get; set; } = string.Empty;
        public string UnidadMedida { get; set; } = "Kg";
        public decimal? PrecioVenta { get; set; }
        public decimal? PrecioCompra { get; set; }
        public DateOnly? FechaActualizacion { get; set; }
    }

    public class CamionSugerenciaItem
    {
        public int Id { get; set; }
        public string Capacidad { get; set; } = string.Empty;
        public string NombreComprador { get; set; } = string.Empty;
        public string PlacaCamion { get; set; } = string.Empty;
        public string RutaProyectada { get; set; } = string.Empty;
    }

    public class CampesinoDestacadoItem
    {
        public int Id { get; set; }
        public string NombreCompleto { get; set; } = string.Empty;
        public string Ubicacion { get; set; } = string.Empty;
        public string FotoUrl { get; set; } = string.Empty;
    }

    public class CompradorMayoristaItem
    {
        public int Id { get; set; }
        public string NombreComercial { get; set; } = string.Empty;
        public string LogoUrl { get; set; } = string.Empty;
    }

    public class ProductoOfertaItem
    {
        public int Id { get; set; }
        public int IdUsuario { get; set; }
        public string NombreProducto { get; set; } = string.Empty;
        public string Variedad { get; set; } = string.Empty;
        public string UbicacionCultivo { get; set; } = string.Empty;
        public string ImagenUrl { get; set; } = string.Empty;
    }
}