namespace CampesinApp.ViewModels
{
    /// <summary>
    /// ViewModel para el Catálogo de Demanda (CU-011).
    /// Agrupa los datos que la vista necesita: filtros, listas desplegables y resultados.
    /// </summary>
    public class CatalogoDemandaViewModel
    {
        // 1. Valores seleccionados en los filtros
        public string? CategoriaSeleccionada { get; set; }
        public string? ProductoSeleccionado { get; set; }
        public string? RegionSeleccionada { get; set; }

        // 2. Opciones para las listas desplegables
        public List<string> CategoriasDisponibles { get; set; } = new();
        public List<string> ProductosDisponibles { get; set; } = new();
        public List<string> RegionesDisponibles { get; set; } = new();

        // 3. Resultados de demandas a mostrar
        public List<DemandaItem> Demandas { get; set; } = new();

        // 4. Manejo de error de conexión (FE-01-CU011)
        public bool TieneErrorConexion { get; set; } = false;
        public string? MensajeError { get; set; }
    }

    /// <summary>
    /// DTO que representa una fila en el catálogo de demanda.
    /// Mapeado desde la entidad <c>Demandum</c> generada por scaffold.
    /// Se llama DemandaItem para no colisionar con la clase parcial <c>Demandum</c>.
    /// </summary>
    public class DemandaItem
    {
        public int Id { get; set; }

        /// <summary>Título descriptivo de la demanda.</summary>
        public string Titulo { get; set; } = string.Empty;

        /// <summary>Nombre del comprador o empresa que solicita el producto.</summary>
        public string Comprador { get; set; } = string.Empty;

        /// <summary>Categoría agrícola (ej: Tubérculos, Frutas, Verduras, Hortalizas).</summary>
        public string Categoria { get; set; } = string.Empty;

        /// <summary>Producto específico (ej: Papa Pastusa, Tomate Chonto, Aguacate Hass).</summary>
        public string Producto { get; set; } = string.Empty;

        /// <summary>Región geográfica de entrega o requerimiento.</summary>
        public string Region { get; set; } = string.Empty;

        /// <summary>Cantidad requerida (en kg, canastillas o toneladas).</summary>
        public string CantidadRequerida { get; set; } = string.Empty;

        /// <summary>Tipo de camión de carga requerido.</summary>
        public string TipoCamion { get; set; } = string.Empty;

        /// <summary>Precio ofrecido o estimado por unidad.</summary>
        public string PrecioOfrecido { get; set; } = string.Empty;

        /// <summary>Fecha en que se publicó la demanda.</summary>
        public DateTime FechaPublicacion { get; set; } = DateTime.Now;

        /// <summary>Descripción o detalles adicionales.</summary>
        public string Descripcion { get; set; } = string.Empty;
    }
}

