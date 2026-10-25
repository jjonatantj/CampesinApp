using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CampesinApp.ViewModels
{
    public class PublicarDemandaViewModel
    {
        [Required(ErrorMessage = "El tipo de producto es obligatorio.")]
        public int IdTipoProducto { get; set; }

        [Required(ErrorMessage = "La variedad es obligatoria.")]
        public int IdVariedad { get; set; }

        [Required(ErrorMessage = "La cantidad es obligatoria.")]
        [Range(20, 1000000, ErrorMessage = "Para compradores mayoristas, la cantidad mínima es de 20 kg.")]
        public decimal Cantidad { get; set; }

        [Required(ErrorMessage = "El precio estimado es obligatorio.")]
        [Range(100, 100000000, ErrorMessage = "Ingrese un precio real y válido.")]
        public decimal PrecioEstimado { get; set; }

        [Required(ErrorMessage = "La fecha de entrega es obligatoria.")]
        [DataType(DataType.Date)]
        public DateTime FechaEntrega { get; set; }

        [Required(ErrorMessage = "La hora de entrega es obligatoria.")]
        public string HoraEntrega { get; set; } = "08:00";

        [Required(ErrorMessage = "El límite de tiempo es obligatorio.")]
        [DataType(DataType.Date)]
        public DateTime LimTiemDeman { get; set; }

        [Required(ErrorMessage = "La hora límite es obligatoria.")]
        public string HoraLimite { get; set; } = "17:00";

        [Required(ErrorMessage = "La descripción es obligatoria.")]
        [StringLength(500, ErrorMessage = "La descripción no puede superar los 500 caracteres.")]
        public string Descripcion { get; set; } = string.Empty;

        [Required(ErrorMessage = "El punto de acopio es obligatorio.")]
        public int IdPuntoAcopio { get; set; }

        // --- CONSTRUCTOR DE DIRECCIÓN URBANA ---
        [Required(ErrorMessage = "Seleccione el tipo de vía.")]
        public string TipoVia { get; set; } = "Calle";

        [Required(ErrorMessage = "El número de vía es obligatorio.")]
        public string NumeroVia { get; set; } = string.Empty;

        public string? LetraBis { get; set; }

        [Required(ErrorMessage = "El cruce es obligatorio.")]
        public string Cruce { get; set; } = string.Empty;

        [Required(ErrorMessage = "La placa es obligatoria.")]
        public string PlacaVia { get; set; } = string.Empty;

        // Campo opcional solo visual (no se almacena en la BD)
        public string? IndicacionExtra { get; set; }

        public string? DetalleDireccion { get; set; }
        public string DireccionEstructurada { get; set; } = string.Empty;

        // Listas para desplegables
        public List<SelectListItem> ListaTiposProducto { get; set; } = new();
        public List<SelectListItem> ListaVariedades { get; set; } = new();
        public List<SelectListItem> ListaPuntosAcopio { get; set; } = new();
        public List<SelectListItem> ListaTiposVia { get; set; } = new()
        {
            new() { Value = "Calle", Text = "Calle" },
            new() { Value = "Carrera", Text = "Carrera" },
            new() { Value = "Transversal", Text = "Transversal" },
            new() { Value = "Diagonal", Text = "Diagonal" },
            new() { Value = "Avenida", Text = "Avenida" }
        };
    }
}