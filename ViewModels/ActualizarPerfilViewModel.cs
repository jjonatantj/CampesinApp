using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CampesinApp.ViewModels
{
    public class ActualizarPerfilViewModel
    {
        public string Nombres { get; set; } = string.Empty;
        public string Apellidos { get; set; } = string.Empty;
        public string Cedula { get; set; } = string.Empty;

        [Required(ErrorMessage = "El correo es obligatorio.")]
        [EmailAddress(ErrorMessage = "Formato de correo no válido.")]
        public string Correo { get; set; } = string.Empty;

        [Required(ErrorMessage = "El teléfono es obligatorio.")]
        [RegularExpression(@"^\d{7,10}$", ErrorMessage = "Ingrese un número de teléfono válido.")]
        public string Telefono { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [StringLength(15, MinimumLength = 6, ErrorMessage = "Debe tener entre 6 y 15 caracteres.")]
        [RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d)(?=.*[\W_]).{6,15}$", ErrorMessage = "Debe incluir letra, número y carácter especial.")]
        public string? NuevaContrasena { get; set; }

        [Required(ErrorMessage = "El departamento es obligatorio.")]
        public int IdDepartamento { get; set; }

        [Required(ErrorMessage = "El municipio es obligatorio.")]
        public int IdMunicipio { get; set; }

        public List<SelectListItem> ListaDepartamentos { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> ListaMunicipios { get; set; } = new List<SelectListItem>();
    }
}