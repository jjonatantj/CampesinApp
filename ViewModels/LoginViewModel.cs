using System.ComponentModel.DataAnnotations;

namespace CampesinApp.ViewModels
{
    /// <summary>ViewModel para el formulario de inicio de sesión (CU-004).</summary>
    public class LoginViewModel
    {
        [Required(ErrorMessage = "La cédula es obligatoria.")]
        [Display(Name = "Número de Cédula")]
        public string Cedula { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña")]
        public string Contrasena { get; set; } = string.Empty;
    }
}

