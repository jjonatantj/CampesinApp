using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CampesinApp.ViewModels
{
    /// <summary>
    /// Modelo de Vista para el Caso de Uso CU-017: "PROCESO DE PAGOS".
    /// Representa los datos requeridos para que el comprador pague y retenga los fondos del pedido.
    /// </summary>
    public class ProcesoPagoViewModel
    {
        // -------------------------------------------------------------
        // DATOS DE LA DEMANDA / PEDIDO A PAGAR
        // -------------------------------------------------------------
        [Required]
        public int? IdDemanda { get; set; }

        public string NombreProducto { get; set; } = string.Empty;

        public string NombreVariedad { get; set; } = string.Empty;

        [Required]
        public int? CantidadKilos { get; set; }

        [Required]
        public double? PrecioPorKilo { get; set; }

        // El comprador paga el 100% del valor estimado del pedido
        public double ValorTotalPagar => (CantidadKilos ?? 0) * (PrecioPorKilo ?? 0);

        [Required]
        public DateTime? FechaEntrega { get; set; }

        public string DireccionEntrega { get; set; } = string.Empty;

        // -------------------------------------------------------------
        // MÉTODO DE PAGO REGISTRADO POR EL COMPRADOR
        // -------------------------------------------------------------
        [Required(ErrorMessage = "Debe seleccionar un método de pago registrado.")]
        [Range(1, int.MaxValue, ErrorMessage = "Por favor elija un método de pago válido.")]
        [Display(Name = "Método de Pago")]
        public int? IdPerfilPagoSeleccionado { get; set; }

        // Lista de opciones para el menú desplegable con las tarjetas/cuentas guardadas
        public List<SelectListItem> ListaMetodosPago { get; set; } = new();

        // Identificador antifraude de sesión para Openpay
        public string? DeviceSessionId { get; set; }
    }

    /// <summary>
    /// Estructura temporal en sesión para retener los datos de la demanda antes de que el pago sea aprobado.
    /// Garantiza que ninguna demanda sea insertada o publicada en la base de datos si el pago es rechazado.
    /// </summary>
    public class BorradorDemandaDto
    {
        public int IdUsuario { get; set; }
        public int IdTipoProducto { get; set; }
        public int IdVariedad { get; set; }
        public int Cantidad { get; set; }
        public double PrecioEstimado { get; set; }
        public DateTime FechaEntrega { get; set; }
        public DateTime LimTiemDeman { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        public int IdPuntoAcopio { get; set; }
        public string DireccionInicial { get; set; } = string.Empty;
    }
}

