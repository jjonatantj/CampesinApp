using System.Collections.Generic;

namespace CampesinApp.ViewModels
{
    /// <summary>
    /// ViewModel para la Pantalla Principal (CU-009).
    /// Agrupa todos los datos que la vista necesita mostrar sin exponer
    /// directamente las entidades del DbContext.
    /// </summary>
    public class PantallaPrincipalViewModel
    {
        // Información de Sesión y Rol
        public string RolUsuario { get; set; } = "Campesino";
        public string CedulaUsuario { get; set; } = string.Empty;

        // Búsqueda y Filtro por Categoría / Tipo de Producto
        public string TerminoBusqueda { get; set; } = string.Empty;
        public string CategoriaSeleccionada { get; set; } = string.Empty;
        public List<string> CategoriasDisponibles { get; set; } = new List<string>();

        // Mensajes y Banderas de Excepción (FE01-CU009 / FE02-CU009)
        public string MensajeExcepcionPrecios { get; set; } = string.Empty;
        public bool HaySugerenciasDisponibles { get; set; } = true;

        // Listas de Datos para la Tabla de Precios y los 4 Bloques de Tarjetas
        public List<PrecioSugeridoItem> TablaPrecios { get; set; } = new List<PrecioSugeridoItem>();
        public List<CamionSugerenciaItem> Camiones { get; set; } = new List<CamionSugerenciaItem>();
        public List<CampesinoDestacadoItem> CampesinosDestacados { get; set; } = new List<CampesinoDestacadoItem>();
        public List<CompradorMayoristaItem> CompradoresMayoristas { get; set; } = new List<CompradorMayoristaItem>();
        public List<ProductoOfertaItem> CatalogoProductos { get; set; } = new List<ProductoOfertaItem>();
    }

    /// <summary>Fila de la tabla de precios sugeridos del día (datos DANE / precio_Estimado).</summary>
    public class PrecioSugeridoItem
    {
        public string Producto { get; set; } = string.Empty;
        public string Variedad { get; set; } = string.Empty;
        public string Categoria { get; set; } = string.Empty;
        public string UnidadMedida { get; set; } = "Kg";
        public decimal? PrecioVenta { get; set; }   // Visible para Campesino, Admin y Transportador (RN-01-CU009)
        public decimal? PrecioCompra { get; set; }  // Visible para Comprador, Admin y Transportador (RN-01-CU009)
        public DateOnly? FechaActualizacion { get; set; }
    }

    /// <summary>Tarjeta de camión/flete disponible (Bloque 1 de sugerencias FA01-CU009).</summary>
    public class CamionSugerenciaItem
    {
        public int Id { get; set; }
        public string Capacidad { get; set; } = string.Empty;
        public string NombreComprador { get; set; } = string.Empty;
        public string PlacaCamion { get; set; } = string.Empty;
        public string RutaProyectada { get; set; } = string.Empty;
    }

    /// <summary>Tarjeta de campesino destacado (Bloque 2 – RN-02-CU009).</summary>
    public class CampesinoDestacadoItem
    {
        public int Id { get; set; }
        public string NombreCompleto { get; set; } = string.Empty;
        public string Ubicacion { get; set; } = string.Empty;
        public string FotoUrl { get; set; } = string.Empty;
    }

    /// <summary>Tarjeta de comprador mayorista (Bloque 3 – RN-02-CU009).</summary>
    public class CompradorMayoristaItem
    {
        public int Id { get; set; }
        public string NombreComercial { get; set; } = string.Empty;
        public string LogoUrl { get; set; } = string.Empty;
    }

    /// <summary>Tarjeta de producto en catálogo (Bloque 4).</summary>
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

