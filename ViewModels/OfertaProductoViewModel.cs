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

        [Required]
        public int? IdDemanda { get; set; }

        public string NombreProducto { get; set; } = string.Empty;

        public string VariedadProducto { get; set; } = string.Empty;

        public string? Descripcion { get; set; }

        // Cantidad total solicitada originalmente por el comprador
        public int? CantidadTotal { get; set; }

        // Cantidad que aún falta por aportar para completar el pedido
        public int? CantidadFaltante { get; set; }

        // Suma de aportes realizados previamente por TODOS los campesinos a esta demanda
        public int? CantidadAportadaPrevia { get; set; }

        // Tope general del 10% del total demandado (RN-04-CU013)
        public int? TopeDiezPorciento { get; set; }

        // Cantidad aportada previamente por ESTE mismo campesino a esta demanda
        public int? AportePrevioCampesino { get; set; }

        // Indica si el campesino ya cumplió con el requisito de haber aportado >= 10%
        public bool YaCumplioMinimoDiez => ((AportePrevioCampesino ?? 0) >= (TopeDiezPorciento ?? 0));

        // Mínimo permitido para la transacción actual (1 si ya cumplió el 10% o le falta poco)
        public int? MinimoPermitido { get; set; }

        public double? PrecioPropuesto { get; set; }

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

        /// <summary>
        /// Punto de acopio seleccionado donde el campesino dejará el producto para su recogida.
        /// </summary>
        [Required(ErrorMessage = "Debe seleccionar un punto de acopio donde entregará su producto.")]
        [Display(Name = "Punto de Acopio de Entrega")]
        public int? IdPuntoAcopio { get; set; }

        // =========================================================================
        // DATOS PARA SELECCIÓN DE PUNTOS DE ACOPIO Y MAPA
        // =========================================================================

        public string GoogleMapsApiKey { get; set; } = string.Empty;

        public List<PuntoAcopioItemViewModel> PuntosAcopioDisponibles { get; set; } = new();

        /// <summary>
        /// Si el campesino ya aportó previamente a esta demanda, el punto de acopio queda fijado
        /// al mismo del primer aporte para consolidar la logística de recogida.
        /// </summary>
        public int? IdPuntoAcopioFijo { get; set; }

        public string? NombrePuntoAcopioFijo { get; set; }

        public string? DetallePuntoAcopioFijo { get; set; }

        public bool TienePuntoAcopioFijo => IdPuntoAcopioFijo.HasValue && IdPuntoAcopioFijo.Value > 0;
    }

    /// <summary>
    /// Elemento simplificado para listar y mapear puntos de acopio en la vista de oferta.
    /// </summary>
    public class PuntoAcopioItemViewModel
    {
        public int IdPuntoAcopio { get; set; }
        public string NombrePunto { get; set; } = string.Empty;
        public string Direccion { get; set; } = string.Empty;
        public string Municipio { get; set; } = string.Empty;
        public string Departamento { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public string HorarioAtencion { get; set; } = string.Empty;
        public decimal Latitud { get; set; }
        public decimal Longitud { get; set; }
        public decimal Capacidad { get; set; }
        public bool AccesoVehicular { get; set; }
        public string? Descripcion { get; set; }
        public bool TipoAcopio { get; set; }
        public bool EsPropio { get; set; }
    }
}

