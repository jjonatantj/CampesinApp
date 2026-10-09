namespace CampesinApp.Repositories.Models
{
    /// <summary>
    /// Representa un producto agrícola con su precio sugerido y demanda preliminar
    /// para mostrarlo en la página de bienvenida (CU-001) a usuarios e invitados.
    /// </summary>
    public class ProductoPreliminar
    {
        // Identificador único del producto
        public int Id { get; set; }

        // Nombre del producto (ej: Papa Pastusa, Aguacate Hass)
        public string Nombre { get; set; } = string.Empty;

        // Categoría agrícola (ej: Tubérculos, Frutas, Hortalizas)
        public string Categoria { get; set; } = string.Empty;

        // Precio sugerido por kilo calculado para la jornada
        public string PrecioSugerido { get; set; } = string.Empty;

        // Cantidad requerida en la demanda activa
        public string CantidadDemanda { get; set; } = string.Empty;

        // Nombre de la empresa o comprador que solicita la cosecha
        public string Comprador { get; set; } = string.Empty;

        // Departamento o región de destino/origen
        public string Ubicacion { get; set; } = string.Empty;

        // Tipo de transporte o camión requerido
        public string TipoCamion { get; set; } = string.Empty;

        // Ruta de la imagen local guardada en la carpeta wwwroot/images
        public string ImagenUrl { get; set; } = string.Empty;
    }
}

