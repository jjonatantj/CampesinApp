using System.ComponentModel.DataAnnotations;

namespace CampesinApp.ViewModels
{
    public class RegistroAdminViewModel
    {
        [Required(ErrorMessage = "El número de documento es obligatorio.")]
        [RegularExpression(@"^[0-9]{6,10}$", ErrorMessage = "Ingrese un número de documento válido (6 a 10 dígitos).")]
        [Display(Name = "Número de Documento")]
        public string Cedula { get; set; } = string.Empty;

        [Required(ErrorMessage = "Los nombres son obligatorios.")]
        [StringLength(30, ErrorMessage = "Máximo 30 caracteres.")]
        [Display(Name = "Nombres")]
        public string Nombres { get; set; } = string.Empty;

        [Required(ErrorMessage = "Los apellidos son obligatorios.")]
        [StringLength(30, ErrorMessage = "Máximo 30 caracteres.")]
        [Display(Name = "Apellidos")]
        public string Apellidos { get; set; } = string.Empty;

        [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
        [EmailAddress(ErrorMessage = "Ingrese un correo electrónico válido.")]
        [StringLength(50, ErrorMessage = "Máximo 50 caracteres.")]
        [Display(Name = "Correo Electrónico")]
        public string Correo { get; set; } = string.Empty;

        [Required(ErrorMessage = "El teléfono es obligatorio.")]
        [RegularExpression(@"^[0-9]{10}$", ErrorMessage = "El teléfono debe tener 10 dígitos.")]
        [Display(Name = "Teléfono")]
        public string Telefono { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        [StringLength(15, MinimumLength = 6, ErrorMessage = "La contraseña debe tener entre 6 y 15 caracteres.")]
        [RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d)(?=.*[@$!%*#?&])[A-Za-z\d@$!%*#?&]{6,15}$",
            ErrorMessage = "Debe incluir al menos una letra, un número y un carácter especial.")]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña")]
        public string Contrasena { get; set; } = string.Empty;

        [Required(ErrorMessage = "La confirmación de contraseña es obligatoria.")]
        [DataType(DataType.Password)]
        [Compare("Contrasena", ErrorMessage = "Las contraseñas no coinciden.")]
        [Display(Name = "Confirmar Contraseña")]
        public string ConfirmarContrasena { get; set; } = string.Empty;
    }
}
