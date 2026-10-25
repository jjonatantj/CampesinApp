using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace CampesinApp.ViewModels
{
    public class DetalleCrucePrecioViewModel
    {
        public string Producto { get; set; } = string.Empty;
        public string Variedad { get; set; } = string.Empty; // <-- Aquí estaba el error (tenía un punto)
        public decimal PrecioDane { get; set; }
        public decimal PrecioCorabastos { get; set; }
    }

    public class ImportarDatosViewModel
    {
        [Required(ErrorMessage = "Debe adjuntar el archivo oficial del DANE.")]
        public IFormFile? ArchivoDane { get; set; }

        [Required(ErrorMessage = "Debe adjuntar el archivo oficial de Corabastos.")]
        public IFormFile? ArchivoCorabastos { get; set; }

        public bool EsVistaPrevia { get; set; } = false;
        public int TotalFilasDane { get; set; }
        public int TotalFilasCorabastos { get; set; }
        public List<DetalleCrucePrecioViewModel> ResultadosCruce { get; set; } = new();
    }
}