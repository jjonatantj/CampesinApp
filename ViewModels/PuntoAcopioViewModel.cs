using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CampesinApp.ViewModels
{
    /// <summary>
    /// Modelo de Vista para el formulario de registro de Puntos de Acopio (CU-018).
    /// </summary>
    public class PuntoAcopioViewModel
    {
        [Required(ErrorMessage = "El nombre del punto de acopio es obligatorio.")]
        [StringLength(100, ErrorMessage = "El nombre no puede exceder los 100 caracteres.")]
        [Display(Name = "Nombre del Punto")]
        public string NombrePunto { get; set; } = string.Empty;

        [Required(ErrorMessage = "Debe seleccionar un departamento.")]
        [Range(1, int.MaxValue, ErrorMessage = "Seleccione un departamento válido.")]
        [Display(Name = "Departamento")]
        public int IdDepartamento { get; set; }

        [Required(ErrorMessage = "Debe seleccionar un municipio.")]
        [Range(1, int.MaxValue, ErrorMessage = "Seleccione un municipio válido.")]
        [Display(Name = "Municipio")]
        public int IdMunicipio { get; set; }

        [Required(ErrorMessage = "La dirección física es obligatoria.")]
        [StringLength(50, ErrorMessage = "La dirección no puede exceder los 50 caracteres.")]
        [Display(Name = "Dirección")]
        public string Direccion { get; set; } = string.Empty;

        // Regla RN-01-CU018: Latitud y Longitud obligatorias seleccionadas en el mapa
        [Required(ErrorMessage = "Debe seleccionar la ubicación geográfica en el mapa (Latitud requerida).")]
        [Display(Name = "Latitud")]
        public decimal? Latitud { get; set; }

        [Required(ErrorMessage = "Debe seleccionar la ubicación geográfica en el mapa (Longitud requerida).")]
        [Display(Name = "Longitud")]
        public decimal? Longitud { get; set; }

        [Required(ErrorMessage = "La capacidad de acopio es obligatoria.")]
        [Range(1, 10000000, ErrorMessage = "La capacidad debe ser un valor numérico positivo mayor a 0.")]
        [Display(Name = "Capacidad (Kg / Toneladas)")]
        public decimal Capacidad { get; set; }

        // Regla RN-02-CU018: Acceso vehicular para camiones
        [Display(Name = "¿Cuenta con acceso vehicular accesible para camión?")]
        public bool AccesoVehicular { get; set; } = true;

        // Elementos auxiliares para renderizar los desplegables en la vista
        public List<SelectListItem> Departamentos { get; set; } = new();
        public List<SelectListItem> Municipios { get; set; } = new();

        // Clave de API de Google Maps para inicializar el mapa en el cliente
        public string GoogleMapsApiKey { get; set; } = string.Empty;
    }
}

