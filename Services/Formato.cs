using System.Globalization;

namespace CampesinApp.Services
{
    /// <summary>Formatos de presentación en español de Colombia para las vistas.</summary>
    public static class Formato
    {
        private static readonly CultureInfo Colombia = new("es-CO");

        public static string Moneda(decimal valor) => valor.ToString("C0", Colombia);

        public static string MonedaConDecimales(decimal valor) => valor.ToString("C2", Colombia);

        public static string Entero(int valor) => valor.ToString("N0", Colombia);

        public static string Promedio(double valor) => valor.ToString("0.0", Colombia);

        public static string Fecha(DateTime fecha) => fecha.ToString("dd/MM/yyyy", Colombia);

        public static string Hora(DateTime fecha) => fecha.ToString("hh:mm tt", Colombia);

        public static string FechaHora(DateTime fecha) => fecha.ToString("dd/MM/yyyy hh:mm tt", Colombia);
    }
}
