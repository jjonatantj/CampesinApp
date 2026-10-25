using System.ComponentModel.DataAnnotations;

namespace CampesinApp.ViewModels
{
    public class ForzarCambioViewModel
    {
        [Required(ErrorMessage = "La nueva contraseña es obligatoria.")]
        [StringLength(15, MinimumLength = 6, ErrorMessage = "Debe tener entre 6 y 15 caracteres.")]
        [RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d)(?=.*[\W_]).{6,15}$", ErrorMessage = "Debe incluir al menos una letra, un número y un carácter especial.")]
        [DataType(DataType.Password)]
        [Display(Name = "Nueva Contraseña")]
        public string NuevaContrasena { get; set; } = string.Empty;

        [Required(ErrorMessage = "Debe confirmar su contraseña.")]
        [DataType(DataType.Password)]
        [Compare("NuevaContrasena", ErrorMessage = "Las contraseñas no coinciden.")]
        [Display(Name = "Confirmar Contraseña")]
        public string ConfirmarContrasena { get; set; } = string.Empty;
    }
}
