using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CampesinApp.ViewModels
{
    /// <summary>
    /// Modelo de Vista para el Caso de Uso CU-014: "COMPLETAR PERFIL DE PAGO".
    /// Transporta los datos del formulario entre la Vista y el Controlador MVC.
    /// </summary>
    public class PerfilPagoViewModel
    {
        // Identificador del usuario que tiene la sesión activa en CampesinApp
        public int IdUsuario { get; set; }

        // Documento de identidad del titular de la cuenta (debe coincidir con la sesión activa)
        [Required(ErrorMessage = "El documento del titular es obligatorio.")]
        [Display(Name = "Titular (Número de Documento)")]
        public string DocumentoTitular { get; set; } = string.Empty;

        // Identificador de la entidad financiera seleccionada (FK a entidad_Financiera)
        [Required(ErrorMessage = "Debe seleccionar una entidad financiera.")]
        [Range(1, int.MaxValue, ErrorMessage = "Por favor elija una entidad financiera válida.")]
        [Display(Name = "Entidad Financiera")]
        public int IdEntiFinanc { get; set; }

        // Identificador del tipo de cuenta bancaria (FK a tipo_Cuenta: Ahorros, Corriente, etc.)
        [Required(ErrorMessage = "Debe seleccionar el tipo de cuenta.")]
        [Range(1, int.MaxValue, ErrorMessage = "Por favor elija un tipo de cuenta válido.")]
        [Display(Name = "Tipo de Cuenta")]
        public int IdTipoCuenta { get; set; }

        // Identificador del tipo/modalidad de pago (FK a tipo_Pago)
        [Required(ErrorMessage = "Debe seleccionar el tipo de pago.")]
        [Range(1, int.MaxValue, ErrorMessage = "Por favor elija una modalidad de pago válida.")]
        [Display(Name = "Tipo de Pago")]
        public int IdTipoPago { get; set; }

        // Número de cuenta o tarjeta ingresado por el usuario
        [Required(ErrorMessage = "El número de cuenta o tarjeta es obligatorio.")]
        [StringLength(19, MinimumLength = 4, ErrorMessage = "El número de cuenta debe tener entre 4 y 19 dígitos.")]
        [Display(Name = "Número de Cuenta")]
        public string NumCuenta { get; set; } = string.Empty;

        // Código de seguridad CVC (ingresado en frontend para tokenización en Openpay)
        [Required(ErrorMessage = "El código CVC es obligatorio.")]
        [StringLength(4, MinimumLength = 3, ErrorMessage = "El CVC debe contener 3 o 4 dígitos.")]
        [Display(Name = "CVC")]
        public string Cvc { get; set; } = string.Empty;

        // Token de seguridad devuelto por la pasarela Openpay tras la tokenización en JavaScript
        public string? TokenOpenpay { get; set; }

        // Identificador del dispositivo (Device Session ID) que genera Openpay para antifraude
        public string? DeviceSessionId { get; set; }

        // Listas para poblar los elementos de selección (<select>) en la Vista
        public List<SelectListItem> ListaEntidadesFinancieras { get; set; } = new();
        public List<SelectListItem> ListaTiposCuenta { get; set; } = new();
        public List<SelectListItem> ListaTiposPago { get; set; } = new();
    }
}

