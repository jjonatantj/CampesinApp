// Valores de texto que se guardan en la base de datos (estados, hitos, tipos). Archivo nuevo: no modifica ningún modelo existente.
namespace CampesinApp.Repositories.Models
{
    /// <summary>Estados del envío de un pedido (CU-021, CU-023, CU-024, CU-025).</summary>
    public static class EstadosEnvio
    {
        public const string SinTransporte = "Sin transporte";
        public const string Asignado = "Asignado";
        public const string EnTransito = "En tránsito";
        public const string Entregado = "Entregado";
        public const string EntregadoRecibido = "Entregado y Recibido Correctamente";
        public const string EntregadoConObservaciones = "Entregado con Observaciones";

        public static readonly string[] Todos =
        {
            SinTransporte, Asignado, EnTransito, Entregado, EntregadoRecibido, EntregadoConObservaciones
        };

        /// <summary>Estados en los que el pedido ya salió del flujo de transporte y entra al historial.</summary>
        public static readonly string[] Finalizados = { Entregado, EntregadoRecibido, EntregadoConObservaciones };

        /// <summary>Estados en los que ya se puede calificar a los demás participantes (CU-026).</summary>
        public static readonly string[] Calificables = { EntregadoRecibido, EntregadoConObservaciones };
    }

    /// <summary>Hitos del reporte de ubicación (RN02-CU021).</summary>
    public static class Hitos
    {
        public const string Salida = "Salida";
        public const string Avance = "Avance 2 horas";
        public const string PuntoAcopio = "Punto de Acopio";
        public const string DestinoFinal = "Destino Final";

        public static readonly string[] Todos = { Salida, Avance, PuntoAcopio, DestinoFinal };
    }

    /// <summary>
    /// Lugar "En carretera" del reporte: no es un punto de acopio, así que las coordenadas que toma el navegador se guardan
    /// en el mismo texto de ubicacion_Actual: "En carretera (4.628000, -74.064000)".
    /// </summary>
    public static class Carretera
    {
        public const string Nombre = "En carretera";

        private static readonly System.Text.RegularExpressions.Regex Patron =
            new(@"^En carretera \((-?\d+(?:\.\d+)?), (-?\d+(?:\.\d+)?)\)$", System.Text.RegularExpressions.RegexOptions.Compiled);

        public static string Formatear(double lat, double lng)
        {
            var c = System.Globalization.CultureInfo.InvariantCulture;
            return $"{Nombre} ({lat.ToString("0.000000", c)}, {lng.ToString("0.000000", c)})";
        }

        public static bool TryLeer(string? ubicacion, out double lat, out double lng)
        {
            lat = lng = 0;
            var m = Patron.Match(ubicacion ?? string.Empty);
            if (!m.Success) return false;
            var c = System.Globalization.CultureInfo.InvariantCulture;
            return double.TryParse(m.Groups[1].Value, System.Globalization.NumberStyles.Float, c, out lat)
                && double.TryParse(m.Groups[2].Value, System.Globalization.NumberStyles.Float, c, out lng);
        }
    }

    public static class Regiones
    {
        public static readonly string[] Todas = { "Andina", "Caribe", "Pacífica", "Orinoquía", "Amazonía", "Insular" };

        private static readonly Dictionary<string, string[]> DepartamentosPorRegion = new()
        {
            ["Andina"] = new[] { "antioquia", "boyaca", "caldas", "cundinamarca", "bogota", "huila", "norte de santander", "quindio", "risaralda", "santander", "tolima" },
            ["Caribe"] = new[] { "atlantico", "bolivar", "cesar", "cordoba", "la guajira", "magdalena", "sucre" },
            ["Pacífica"] = new[] { "cauca", "choco", "narino", "valle del cauca" },
            ["Orinoquía"] = new[] { "arauca", "casanare", "meta", "vichada" },
            ["Amazonía"] = new[] { "amazonas", "caqueta", "guainia", "guaviare", "putumayo", "vaupes" },
            ["Insular"] = new[] { "san andres" }
        };

        /// <summary>Región natural a la que pertenece un departamento (vacío si no se reconoce).</summary>
        public static string DeDepartamento(string? departamento)
        {
            var buscado = Normalizar(departamento);
            if (buscado.Length == 0) return string.Empty;
            foreach (var par in DepartamentosPorRegion)
            {
                if (par.Value.Any(d => buscado.StartsWith(d, StringComparison.Ordinal))) return par.Key;
            }
            return string.Empty;
        }

        private static string Normalizar(string? texto)
        {
            var descompuesto = (texto ?? string.Empty).Trim().ToLowerInvariant().Normalize(System.Text.NormalizationForm.FormD);
            return new string(descompuesto.Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark).ToArray());
        }
    }

    public static class TiposNotificacion
    {
        public const string Ubicacion = "Ubicacion";
        public const string Llegada = "Llegada";
        public const string EntregaCompletada = "EntregaCompletada";
        public const string Novedad = "Novedad";

        /// <summary>Tipos que además de la campana muestran una ventana emergente al destinatario (CU-025).</summary>
        public static readonly string[] ConPopup = { Llegada, EntregaCompletada };
    }

    public static class NivelesLog
    {
        public const string Info = "INFO";
        public const string Advertencia = "WARN";
        public const string Error = "ERROR";
    }
}
