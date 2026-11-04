using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CampesinApp.ViewModels
{
    /// <summary>
    /// Modelo de Vista para el formulario de registro de Puntos de Acopio (CU-018).
    /// Contiene los campos del lugar físico y las propiedades auxiliares para la vista Razor.
    /// </summary>
    public class PuntoAcopioViewModel
    {
        [Required(ErrorMessage = "El nombre del punto de acopio es obligatorio.")]
        [StringLength(100, ErrorMessage = "El nombre no puede superar los 100 caracteres.")]
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
        [StringLength(150, ErrorMessage = "La dirección no puede superar los 150 caracteres.")]
        [Display(Name = "Dirección Física")]
        public string Direccion { get; set; } = string.Empty;

        [Required(ErrorMessage = "El teléfono de contacto es obligatorio.")]
        [StringLength(15, ErrorMessage = "El teléfono no puede superar los 15 dígitos.")]
        [Display(Name = "Teléfono de Contacto")]
        public string Telefono { get; set; } = string.Empty;

        [Required(ErrorMessage = "El horario de atención es obligatorio.")]
        [StringLength(80, ErrorMessage = "El horario no puede superar los 80 caracteres.")]
        [Display(Name = "Horario de Atención")]
        public string HorarioAtencion { get; set; } = "Lunes a Viernes: 6:00 AM - 5:00 PM";

        [Required(ErrorMessage = "La capacidad estimada es obligatoria.")]
        [Range(1, 10000000, ErrorMessage = "La capacidad debe ser un número positivo mayor a 0.")]
        [Display(Name = "Capacidad (Kg / Toneladas)")]
        public decimal Capacidad { get; set; }

        // Regla RN-02-CU018: Acceso vehicular para camiones
        [Display(Name = "¿Cuenta con acceso vehicular para camión?")]
        public bool AccesoVehicular { get; set; } = true;

        [StringLength(255, ErrorMessage = "La descripción no puede superar los 255 caracteres.")]
        [Display(Name = "Descripción o Detalles del Punto")]
        public string? Descripcion { get; set; }

        /// <summary>
        /// Indica si el usuario conectado es Administrador para permitirle seleccionar manualmente el tipo de acopio.
        /// </summary>
        public bool EsAdministrador { get; set; }

        /// <summary>
        /// Selección manual del tipo de punto por parte del Administrador:
        /// false (0) = Campesino, true (1) = Comprador
        /// </summary>
        [Display(Name = "Tipo de Punto de Acopio")]
        public bool TipoAcopioManual { get; set; }

        // Regla RN-01-CU018: Latitud y Longitud geográficas (llenadas automáticamente por el mapa en campos ocultos)
        [Required(ErrorMessage = "Debe hacer clic en el mapa para marcar la ubicación geográfica (Latitud requerida).")]
        [Display(Name = "Latitud")]
        public decimal? Latitud { get; set; }

        [Required(ErrorMessage = "Debe hacer clic en el mapa para marcar la ubicación geográfica (Longitud requerida).")]
        [Display(Name = "Longitud")]
        public decimal? Longitud { get; set; }

        // Listas desplegables para la vista Razor
        public List<SelectListItem> Departamentos { get; set; } = new();
        public List<SelectListItem> Municipios { get; set; } = new();

        // Clave de API de Google Maps para cargar el mapa interactivo
        public string GoogleMapsApiKey { get; set; } = string.Empty;
    }
}
