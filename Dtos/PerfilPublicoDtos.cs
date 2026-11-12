namespace CampesinApp.Dtos
{
    /// <summary>Dato destacado del perfil público (tarjeta con icono, valor y título).</summary>
    public class DatoPerfilDto
    {
        public string Titulo { get; init; } = string.Empty;

        public string Valor { get; init; } = string.Empty;

        public string Icono { get; init; } = "bi-circle";
    }

    public class OpinionPerfilDto
    {
        public string Autor { get; init; } = string.Empty;

        public string RolAutor { get; init; } = string.Empty;

        public int Estrellas { get; init; }

        public string Texto { get; init; } = string.Empty;

        public DateTime Fecha { get; init; }
    }

    public class ProductoPerfilDto
    {
        public string Nombre { get; init; } = string.Empty;

        public string Variedad { get; init; } = string.Empty;

        public string Descripcion { get; init; } = string.Empty;

        public string Cultivo { get; init; } = string.Empty;
    }

    public class PuntoPerfilDto
    {
        public string Nombre { get; init; } = string.Empty;

        public string Direccion { get; init; } = string.Empty;

        public string Horario { get; init; } = string.Empty;

        public bool AccesoVehicular { get; init; }
    }

    public class VehiculoPerfilDto
    {
        public string Placa { get; init; } = string.Empty;

        public string Modelo { get; init; } = string.Empty;

        public int CapacidadToneladas { get; init; }

        public bool PaseAlDia { get; init; }

        public bool SeguroAlDia { get; init; }

        public bool TecnomecanicaAlDia { get; init; }

        public bool DocumentosAlDia => PaseAlDia && SeguroAlDia && TecnomecanicaAlDia;
    }
}
