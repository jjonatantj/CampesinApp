namespace CampesinApp.Dtos
{
    /// <summary>
    /// DTO que transfiere la información del punto de acopio desde el controlador hacia el repositorio.
    /// Permite persistir los datos en la tabla existente 'demanda' (direccion_Inicial, direccion_Final, etc.).
    /// </summary>
    public class PuntoAcopioRegistroDto
    {
        public int IdUsuario { get; set; }
        public string NombrePunto { get; set; } = string.Empty;
        public int IdDepartamento { get; set; }
        public int IdMunicipio { get; set; }
        public string Direccion { get; set; } = string.Empty;
        public decimal Latitud { get; set; }
        public decimal Longitud { get; set; }
        public decimal Capacidad { get; set; }
        public bool AccesoVehicular { get; set; }
    }
}

