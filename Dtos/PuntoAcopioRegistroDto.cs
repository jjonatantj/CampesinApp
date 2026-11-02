namespace CampesinApp.Dtos
{
    /// <summary>
    /// DTO que transfiere la información del punto de acopio desde el controlador hacia el repositorio.
    /// Mapea los campos físicos de la tabla 'punto_Acopio' y el usuario logueado para 'usuario_Punto_Acopio'.
    /// </summary>
    public class PuntoAcopioRegistroDto
    {
        public int IdUsuario { get; set; }
        public int IdDepartamento { get; set; }
        public int IdMunicipio { get; set; }
        public string NombrePunto { get; set; } = string.Empty;
        public string Direccion { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public string HorarioAtencion { get; set; } = string.Empty;
        public decimal Latitud { get; set; }
        public decimal Longitud { get; set; }
        public decimal Capacidad { get; set; }
        public bool AccesoVehicular { get; set; }
        public bool TipoAcopio { get; set; } // 1 = Comprador, 0 = Campesino
        public string? Descripcion { get; set; }
    }
}
