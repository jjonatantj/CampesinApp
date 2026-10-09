using System.ComponentModel.DataAnnotations;

namespace CampesinApp.ViewModels
{
    public class VerificacionCodigoViewModel
    {
        public string Correo { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;

        // Propiedad que usa la vista VerificacionCodigo.cshtml
        [Required(ErrorMessage = "El código enviado al correo electrónico es obligatorio.")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "El código de correo debe tener exactamente 6 dígitos.")]
        public string CodigoCorreo { get; set; } = string.Empty;

        // Alias para compatibilidad con el controlador
        public string Codigo
        {
            get => CodigoCorreo;
            set => CodigoCorreo = value;
        }

        [Required(ErrorMessage = "El código enviado al teléfono es obligatorio.")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "El código de teléfono debe tener exactamente 6 dígitos.")]
        public string CodigoTelefono { get; set; } = string.Empty;
    }
}
