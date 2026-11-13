using System.ComponentModel.DataAnnotations;

namespace CampesinApp.ViewModels
{
    public class DetalleCrucePrecioViewModel
    {
        public string Producto { get; set; } = string.Empty;
        public string Variedad { get; set; } = string.Empty;
        public decimal? PrecioDane { get; set; }
        public decimal? PrecioCorabastos { get; set; }
    }

    public class ImportarDatosViewModel
    {
        [Required(ErrorMessage = "Debe adjuntar el archivo oficial del DANE (.xlsx, .xls o .csv).")]
        public IFormFile? ArchivoDane { get; set; }

        [Required(ErrorMessage = "Debe adjuntar el boletín oficial de Corabastos en formato PDF.")]
        public IFormFile? ArchivoCorabastos { get; set; }

        public bool EsVistaPrevia { get; set; } = false;
        [Required]
        public int TotalFilasDane { get; set; }
        [Required]
        public int TotalFilasCorabastos { get; set; }
        public List<DetalleCrucePrecioViewModel> ResultadosCruce { get; set; } = new();
    }
}