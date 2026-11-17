using System.ComponentModel.DataAnnotations;

// ARCHIVO LEGACY - Migrado a ViewModels/LoginViewModel.cs
namespace CampesinApp.Repositories.Models.Legacy
{
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
