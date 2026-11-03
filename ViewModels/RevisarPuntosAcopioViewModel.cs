using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CampesinApp.ViewModels
{
    /// <summary>
    /// Modelo de Vista para el Caso de Uso CU-019: Revisar Puntos de Acopio (Mapas).
    /// Provee los datos para inicializar el mapa y los desplegables de filtros.
    /// </summary>
    public class RevisarPuntosAcopioViewModel
    {
        /// <summary>
        /// Clave de API de Google Maps Javascript configurada en appsettings.json.
        /// </summary>
        public string GoogleMapsApiKey { get; set; } = string.Empty;

        /// <summary>
        /// Filtro por Tipo de Acopio en la base de datos (bit):
        /// null = Todos, false (0) = Campesino, true (1) = Comprador
        /// </summary>
        public bool? TipoAcopioSeleccionado { get; set; }

        /// <summary>
        /// Identificador del departamento seleccionado en el filtro.
        /// </summary>
        public int? IdDepartamentoSeleccionado { get; set; }

        /// <summary>
        /// Identificador del municipio seleccionado en el filtro.
        /// </summary>
        public int? IdMunicipioSeleccionado { get; set; }

        /// <summary>
        /// Identificador de demanda seleccionada (opcional).
        /// </summary>
        public int? IdDemandaSeleccionada { get; set; }

        /// <summary>
        /// Lista de tipos de punto de acopio para el select (Campesino / Comprador).
        /// </summary>
        public List<SelectListItem> TiposDisponibles { get; set; } = new();

        /// <summary>
        /// Lista de departamentos para el select de filtros.
        /// </summary>
        public List<SelectListItem> DepartamentosDisponibles { get; set; } = new();

        /// <summary>
        /// Lista de municipios para el select de filtros.
        /// </summary>
        public List<SelectListItem> MunicipiosDisponibles { get; set; } = new();

        /// <summary>
        /// Lista de demandas/ofertas activas para el select de filtros (opcional).
        /// </summary>
        public List<SelectListItem> DemandasDisponibles { get; set; } = new();

        /// <summary>
        /// Mensaje de error para notificaciones de excepción en la interfaz.
        /// </summary>
        public string? MensajeError { get; set; }
    }
}
