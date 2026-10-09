using System.ComponentModel.DataAnnotations;

namespace CampesinApp.ViewModels
{
    /// <summary>ViewModel para el flujo de recuperación de contraseña (CU-005).</summary>
    public class RecuperaciónContraseñaViewModel
    {
        public int Paso { get; set; } = 1; // 1: Cédula, 2: Método, 3: Código, 4: Nueva Contraseña

        [Required(ErrorMessage = "La cédula es obligatoria.")]
        [Display(Name = "Número de Cédula")]
        public string Cedula { get; set; }

        public string MetodoSeleccionado { get; set; } // "correo" o "telefono"
        public string CorreoEnmascarado { get; set; }
        public string TelefonoEnmascarado { get; set; }

        [StringLength(6, MinimumLength = 6, ErrorMessage = "El código debe tener exactamente 6 dígitos.")]
        [Display(Name = "Código de Verificación")]
        public string CodigoVerificacion { get; set; }

        [Required(ErrorMessage = "El campo Nueva Contraseña es obligatorio.")]
        [StringLength(15, MinimumLength = 6, ErrorMessage = "Debe tener entre 6 y 15 caracteres.")]
        [RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d)(?=.*[\W_]).{6,15}$", ErrorMessage = "Debe incluir al menos una letra, un número y un carácter especial.")]
        [DataType(DataType.Password)]
        [Display(Name = "Nueva Contraseña")]
        public string NuevaContrasena { get; set; }

        [Required(ErrorMessage = "El campo Confirmar Nueva Contraseña es obligatorio.")]
        [DataType(DataType.Password)]
        [Compare("NuevaContrasena", ErrorMessage = "Las contraseñas no coinciden.")]
        [Display(Name = "Confirmar Nueva Contraseña")]
        public string ConfirmarContrasena { get; set; }
    }
}

