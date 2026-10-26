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
        /// Identificador de la demanda seleccionada en el filtro.
        /// </summary>
        public int? IdDemandaSeleccionada { get; set; }

        /// <summary>
        /// Identificador del municipio seleccionado en el filtro.
        /// </summary>
        public int? IdMunicipioSeleccionado { get; set; }

        /// <summary>
        /// Lista de demandas/ofertas activas para el select de filtros.
        /// </summary>
        public List<SelectListItem> DemandasDisponibles { get; set; } = new();

        /// <summary>
        /// Lista de municipios para el select de filtros.
        /// </summary>
        public List<SelectListItem> MunicipiosDisponibles { get; set; } = new();

        /// <summary>
        /// Mensaje de error para notificaciones de excepción en la interfaz.
        /// </summary>
        public string? MensajeError { get; set; }
    }
}

