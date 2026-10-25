using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

namespace CampesinApp.ViewModels
{
    public class RegistroUsuarioViewModel
    {
        // Control de Pasos (1: Formulario principal, 2: Seleccionar Método, 3: Ingresar Código)
        public int Paso { get; set; } = 1;

        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [RegularExpression(@"^[a-zA-ZáéíóúÁÉÍÓÚñÑ\s]+$", ErrorMessage = "Los nombres solo deben contener letras y espacios.")]
        [Display(Name = "Nombres")]
        public string Nombres { get; set; } = string.Empty;

        [Required(ErrorMessage = "El apellido es obligatorio.")]
        [RegularExpression(@"^[a-zA-ZáéíóúÁÉÍÓÚñÑ\s]+$", ErrorMessage = "Los apellidos solo deben contener letras y espacios.")]
        [Display(Name = "Apellidos")]
        public string Apellidos { get; set; } = string.Empty;

        [Required(ErrorMessage = "La cédula es obligatoria.")]
        [RegularExpression(@"^\d{6,10}$", ErrorMessage = "La cédula debe contener solo números (entre 6 y 10 dígitos).")]
        [Display(Name = "Cédula")]
        public string Cedula { get; set; } = string.Empty;

        [Required(ErrorMessage = "El correo es obligatorio.")]
        [EmailAddress(ErrorMessage = "Formato de correo no válido.")]
        [Display(Name = "Correo Electrónico")]
        public string Correo { get; set; } = string.Empty;

        [Required(ErrorMessage = "El teléfono es obligatorio.")]
        [RegularExpression(@"^\d{7,10}$", ErrorMessage = "El teléfono debe contener solo números (entre 7 y 10 dígitos).")]
        [Display(Name = "Teléfono")]
        public string Telefono { get; set; } = string.Empty;

        [Required(ErrorMessage = "Debe seleccionar un rol.")]
        [Display(Name = "Rol")]
        public int IdRol { get; set; }

        [Required(ErrorMessage = "El departamento es obligatorio.")]
        [Display(Name = "Departamento")]
        public int IdDepartamento { get; set; }

        [Required(ErrorMessage = "El municipio es obligatorio.")]
        [Display(Name = "Municipio")]
        public int IdMunicipio { get; set; }

        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        [StringLength(15, MinimumLength = 6, ErrorMessage = "Debe tener entre 6 y 15 caracteres.")]
        [RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d)(?=.*[\W_]).{6,15}$", ErrorMessage = "Debe incluir al menos una letra, un número y un carácter especial.")]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña")]
        public string Contrasena { get; set; } = string.Empty;

        [Required(ErrorMessage = "Debe confirmar su contraseña.")]
        [DataType(DataType.Password)]
        [Compare("Contrasena", ErrorMessage = "Las contraseñas no coinciden.")]
        [Display(Name = "Confirmar Contraseña")]
        public string ConfirmarContrasena { get; set; } = string.Empty;

        [Display(Name = "Foto de Perfil")]
        public IFormFile? FotoPerfil { get; set; }

        // Propiedades para los Pasos 2 y 3 (Selección de método y verificación)
        public string? MetodoSeleccionado { get; set; } // "correo" o "telefono"
        public string? CorreoEnmascarado { get; set; }
        public string? TelefonoEnmascarado { get; set; }

        [Display(Name = "Código de Verificación")]
        public string? CodigoVerificacion { get; set; }

        // Listas dinámicas
        public List<SelectListItem> ListaRoles { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> ListaDepartamentos { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> ListaMunicipios { get; set; } = new List<SelectListItem>();
    }
}