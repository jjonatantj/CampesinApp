using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CampesinApp.ViewModels
{
    public class PublicarDemandaViewModel
    {
        [Required(ErrorMessage = "El tipo de producto es obligatorio.")]
        [Display(Name = "Tipo de Producto")]
        public int IdTipoProducto { get; set; }

        [Required(ErrorMessage = "La variedad es obligatoria.")]
        [Display(Name = "Variedad")]
        public int IdVariedad { get; set; }

        [Required(ErrorMessage = "La cantidad requerida es obligatoria.")]
        [Range(20, 1000000, ErrorMessage = "Para compradores mayoristas, la cantidad mínima permitida es de 20 kg.")]
        [Display(Name = "Cantidad Requerida (Kilos)")]
        public decimal Cantidad { get; set; }

        [Required(ErrorMessage = "El precio propuesto es obligatorio.")]
        [Range(100, 100000000, ErrorMessage = "Ingrese un precio real y válido (mayor a 100 COP).")]
        [Display(Name = "Precio Propuesto (COP por Kilo)")]
        public decimal PrecioEstimado { get; set; }

        [Required(ErrorMessage = "La fecha de entrega es obligatoria.")]
        [DataType(DataType.Date)]
        [Display(Name = "Fecha de Entrega")]
        public DateTime FechaEntrega { get; set; }

        [Required(ErrorMessage = "La hora de entrega es obligatoria.")]
        [Display(Name = "Hora de Entrega")]
        public string HoraEntrega { get; set; } = "08:00";

        [DataType(DataType.Date)]
        [Display(Name = "Fecha Límite")]
        public DateTime? LimTiemDeman { get; set; }

        [Required(ErrorMessage = "La hora límite es obligatoria.")]
        [Display(Name = "Hora Límite")]
        public string HoraLimite { get; set; } = "23:59";

        [Required(ErrorMessage = "La descripción y requerimientos son obligatorios.")]
        [StringLength(500, ErrorMessage = "La descripción no puede superar los 500 caracteres.")]
        [Display(Name = "Descripción y Requerimientos")]
        public string Descripcion { get; set; } = string.Empty;

        [Required(ErrorMessage = "Debe seleccionar un punto de acopio autorizado.")]
        [Display(Name = "Punto de Acopio Autorizado")]
        public int IdPuntoAcopio { get; set; }
        public bool TienePuntosAcopio { get; set; } = true;

        // Listas para desplegables
        public List<SelectListItem> ListaTiposProducto { get; set; } = new();
        public List<SelectListItem> ListaVariedades { get; set; } = new();
        public List<SelectListItem> ListaPuntosAcopio { get; set; } = new();
    }
}