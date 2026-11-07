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

        /// <summary>"ahora", "hace 5 min", "hace 2 h", "ayer" o la fecha, para las listas de avisos.</summary>
        public static string Relativo(DateTime fecha)
        {
            var pasado = DateTime.Now - fecha;
            if (pasado.TotalSeconds < 45) return "ahora";
            if (pasado.TotalMinutes < 60) return $"hace {Math.Max(1, (int)pasado.TotalMinutes)} min";
            if (pasado.TotalHours < 24) return $"hace {(int)pasado.TotalHours} h";
            if (pasado.TotalHours < 48) return "ayer";
            return FechaHora(fecha);
        }
    }
}
