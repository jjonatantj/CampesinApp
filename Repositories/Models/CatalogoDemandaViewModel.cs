namespace CampesinApp.Repositories.Models
{
    /// <summary>
    /// Modelo de vista (ViewModel) para la pantalla del Catálogo de Demanda (CU-011).
    /// Agrupa los datos necesarios para la vista: los 3 filtros seleccionados, opciones de desplegables y resultados.
    /// </summary>
    public class CatalogoDemandaViewModel
    {
        // ==========================================
        // 1. Valores seleccionados en los 3 filtros desplegables
        // ==========================================
        public string? CategoriaSeleccionada { get; set; }
        public string? ProductoSeleccionado { get; set; }
        public string? RegionSeleccionada { get; set; }

        // ==========================================
        // 2. Opciones para poblar las listas desplegables (selects)
        // ==========================================
        public List<string> CategoriasDisponibles { get; set; } = new();
        public List<string> ProductosDisponibles { get; set; } = new();
        public List<string> RegionesDisponibles { get; set; } = new();

        // ==========================================
        // 3. Resultados de demandas a mostrar en las tarjetas
        // ==========================================
        public List<Demandum> Demandas { get; set; } = new();
    }
}

