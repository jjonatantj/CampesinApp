namespace CampesinApp.Repositories.Models
{
    /// <summary>
    /// Representa una solicitud de compra (Demanda) publicada por un comprador.
    /// Según CU-011: Demanda de productos que necesita el comprador por camión de carga.
    /// </summary>
    public class Demanda
    {
        public int Id { get; set; }

        // Título descriptivo de la demanda
        public string Titulo { get; set; } = string.Empty;

        // Nombre del comprador o empresa que solicita el producto
        public string Comprador { get; set; } = string.Empty;

        // Categoría agrícola (ej: Tubérculos, Frutas, Verduras, Hortalizas)
        public string Categoria { get; set; } = string.Empty;

        // Producto específico (ej: Papa Pastusa, Tomate Chonto, Aguacate Hass)
        public string Producto { get; set; } = string.Empty;

        // Región geográfica de entrega o requerimiento (ej: Cundinamarca, Boyacá, Antioquia)
        public string Region { get; set; } = string.Empty;

        // Cantidad requerida especificada en kilos, canastillas o toneladas
        public string CantidadRequerida { get; set; } = string.Empty;

        // Tipo de camión de carga requerido (ej: Camión Turbo, Sencillo, Doble Troque, Tractomula)
        public string TipoCamion { get; set; } = string.Empty;

        // Precio ofrecido o estimado por unidad o kilo
        public string PrecioOfrecido { get; set; } = string.Empty;

        // Fecha en que se publicó la demanda
        public DateTime FechaPublicacion { get; set; } = DateTime.Now;

        // Descripción o detalles adicionales para el campesino
        public string Descripcion { get; set; } = string.Empty;
    }
}

