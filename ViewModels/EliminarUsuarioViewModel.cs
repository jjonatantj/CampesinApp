using System.ComponentModel.DataAnnotations;

namespace CampesinApp.ViewModels
{
    public class EliminarUsuarioViewModel
    {
        [Display(Name = "Correo Electrónico")]
        public string Correo { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña")]
        public string Contrasena { get; set; } = string.Empty;

        [Display(Name = "Código de Verificación")]
        public string? CodigoVerificacion { get; set; }

        public bool? CodigoEnviado { get; set; } = false;
    }
}