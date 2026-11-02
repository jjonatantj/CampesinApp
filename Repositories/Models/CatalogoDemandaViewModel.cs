namespace CampesinApp.Repositories.Models
{
    /// <summary>
    /// Modelo de vista (ViewModel) para la pantalla del Catálogo de Demanda (CU-011).
    /// Agrupa los datos necesarios para la vista: los filtros jerárquicos (Tipo Producto -> Variedad, Departamento -> Municipio),
    /// opciones de desplegables, resultados, datos de sesión y control de excepciones (FE-01-CU011 y FE-02-CU011).
    /// </summary>
    public class CatalogoDemandaViewModel
    {
        // ==========================================
        // 1. Valores seleccionados en los filtros jerárquicos
        // ==========================================
        public int? IdTipoProductoSeleccionado { get; set; }
        public int? IdVariedadSeleccionada { get; set; }
        public int? IdDepartamentoSeleccionado { get; set; }
        public int? IdMunicipioSeleccionado { get; set; }

        // ==========================================
        // 2. Opciones para poblar las listas desplegables
        // ==========================================
        public List<TipoProducto> TiposProductoDisponibles { get; set; } = new();
        public List<Variedad> VariedadesDisponibles { get; set; } = new();
        public List<Departamento> DepartamentosDisponibles { get; set; } = new();
        public List<Municipio> MunicipiosDisponibles { get; set; } = new();

        // Propiedades de retrocompatibilidad
        public string? CategoriaSeleccionada { get; set; }
        public string? ProductoSeleccionado { get; set; }
        public string? RegionSeleccionada { get; set; }
        public List<string> CategoriasDisponibles { get; set; } = new();
        public List<string> ProductosDisponibles { get; set; } = new();
        public List<string> RegionesDisponibles { get; set; } = new();

        // ==========================================
        // 3. Resultados de demandas a mostrar en las tarjetas
        // ==========================================
        public List<Demandum> Demandas { get; set; } = new();

        // ==========================================
        // 4. Gestión de Sesión (RN01-CU011: Invitado vs Campesino)
        // ==========================================
        public string? RolUsuario { get; set; }
        public bool EsUsuarioAutenticado => !string.IsNullOrEmpty(RolUsuario);

        // ==========================================
        // 5. Manejo de Excepciones: FE-01-CU011 (Fallo en Base de Datos)
        // ==========================================
        public bool TieneErrorConexion { get; set; } = false;
        public string? MensajeError { get; set; }

        /// <summary>
        /// Determina si hay al menos un filtro activo para mostrar el botón de limpiar filtros.
        /// </summary>
        public bool TieneFiltrosActivos =>
            (IdTipoProductoSeleccionado.HasValue && IdTipoProductoSeleccionado.Value > 0) ||
            (IdVariedadSeleccionada.HasValue && IdVariedadSeleccionada.Value > 0) ||
            (IdDepartamentoSeleccionado.HasValue && IdDepartamentoSeleccionado.Value > 0) ||
            (IdMunicipioSeleccionado.HasValue && IdMunicipioSeleccionado.Value > 0);
    }
}
