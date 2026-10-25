using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace CampesinApp.ViewModels
{
    /// <summary>
    /// Modelo de Vista para el Caso de Uso CU-013: Registro de Producto (Oferta del campesino a una demanda).
    /// Contiene los datos requeridos para mostrar el resumen de la solicitud en la parte superior
    /// y captura los datos de entrada del campesino con sus validaciones.
    /// </summary>
    public class OfertaProductoViewModel
    {
        // =========================================================================
        // DATOS INFORMATIVOS DE LA DEMANDA (Solo lectura en la parte superior)
        // =========================================================================

        public int IdDemanda { get; set; }

        public string NombreProducto { get; set; } = string.Empty;

        public string VariedadProducto { get; set; } = string.Empty;

        public string? Descripcion { get; set; }

        // Cantidad total solicitada originalmente por el comprador
        public int CantidadTotal { get; set; }

        // Cantidad que aún falta por aportar para completar el pedido
        public int CantidadFaltante { get; set; }

        // Suma de aportes realizados previamente por otros campesinos
        public int CantidadAportadaPrevia { get; set; }

        // Tope mínimo permitido por el sistema: 10% del total demandado (RN-04-CU013)
        public int MinimoPermitido { get; set; }

        public double PrecioPropuesto { get; set; }

        public string MunicipioEntrega { get; set; } = string.Empty;

        public string DepartamentoEntrega { get; set; } = string.Empty;

        public string? DireccionEntrega { get; set; }

        public string CompradorNombre { get; set; } = string.Empty;

        // =========================================================================
        // CAMPOS DE ENTRADA DEL FORMULARIO (Diligenciados por el Campesino)
        // =========================================================================

        /// <summary>
        /// Cantidad de producto que el campesino aportará a la demanda.
        /// RN-05-CU013: Debe ser un número entero mayor a cero.
        /// </summary>
        [Required(ErrorMessage = "Debe ingresar la cantidad que aportará.")]
        [Range(1, int.MaxValue, ErrorMessage = "Por favor ingrese solo números enteros mayores a cero en el campo de cantidad ofertada.")]
        [Display(Name = "Cantidad a aportar")]
        public int? CantidadAporte { get; set; }

        /// <summary>
        /// Imagen fotográfica del producto ofertado.
        /// RN-06-CU013: Formatos permitidos: JPG, JPEG, PNG y WebP.
        /// </summary>
        [Required(ErrorMessage = "Debe adjuntar una imagen del producto que va a ofertar.")]
        [Display(Name = "Imagen del producto")]
        public IFormFile? ImagenProducto { get; set; }
    }
}

