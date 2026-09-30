namespace CampesinApp.Repositories.Models
{
    /// <summary>
    /// Modelo de vista (ViewModel) para la pantalla del Catálogo de Demanda (CU-011).
    /// Agrupa los datos que la vista necesita: filtros, listas desplegables y resultados.
    /// </summary>
    public class CatalogoDemandaViewModel
    {
        // ==========================================
        // 1. Valores seleccionados en los filtros
        // ==========================================
        public string? CategoriaSeleccionada { get; set; }
        public string? ProductoSeleccionado { get; set; }
        public string? RegionSeleccionada { get; set; }

        // ==========================================
        // 2. Opciones para las listas desplegables
        // ==========================================
        public List<string> CategoriasDisponibles { get; set; } = new();
        public List<string> ProductosDisponibles { get; set; } = new();
        public List<string> RegionesDisponibles { get; set; } = new();

        // ==========================================
        // 3. Resultados de demandas a mostrar
        // ==========================================
        public List<Demanda> Demandas { get; set; } = new();

        // ==========================================
        // 4. Manejo de error de conexión (FE-01-CU011)
        // ==========================================
        public bool TieneErrorConexion { get; set; } = false;
        public string? MensajeError { get; set; }
    }
}

