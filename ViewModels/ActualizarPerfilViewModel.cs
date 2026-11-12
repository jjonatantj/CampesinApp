using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CampesinApp.ViewModels
{
    public class ActualizarPerfilViewModel
    {
        public string Nombres { get; set; } = string.Empty;
        public string Apellidos { get; set; } = string.Empty;
        public string Cedula { get; set; } = string.Empty;

        [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
        [EmailAddress(ErrorMessage = "Por favor, ingrese un formato de correo electrónico válido.")]
        public string Correo { get; set; } = string.Empty;

        [Required(ErrorMessage = "El número de teléfono es obligatorio.")]
        [RegularExpression(@"^\d{10}$", ErrorMessage = "El teléfono debe contener exactamente 10 dígitos numéricos.")]
        public string Telefono { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [StringLength(15, MinimumLength = 6, ErrorMessage = "La contraseña debe tener entre 6 y 15 caracteres.")]
        [RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d)(?=.*[\W_]).{6,15}$", ErrorMessage = "La contraseña debe incluir al menos una letra, un número y un carácter especial.")]
        public string? NuevaContrasena { get; set; }

        [Required(ErrorMessage = "Debe seleccionar un departamento.")]
        [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar un departamento válido.")]
        public int? IdDepartamento { get; set; }

        [Required(ErrorMessage = "Debe seleccionar un municipio.")]
        [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar un municipio válido.")]
        public int? IdMunicipio { get; set; }

        public List<SelectListItem> ListaDepartamentos { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> ListaMunicipios { get; set; } = new List<SelectListItem>();
    }
}