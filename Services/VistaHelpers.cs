using CampesinApp.Repositories.Models;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace CampesinApp.Services
{
    public static class VistaHelpers
    {
        private static readonly System.Text.Json.JsonSerializerOptions OpcionesJson = new()
        {
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
        };

        /// <summary>Puntos del mapa como JSON para el atributo data-puntos de mapas.js.</summary>
        public static string PuntosJson(IEnumerable<CampesinApp.Dtos.PuntoMapaDto> puntos) =>
            System.Text.Json.JsonSerializer.Serialize(puntos, OpcionesJson);

        /// <summary>Puntos de acopio como JSON para el mapa donde se elige el lugar del reporte.</summary>
        public static string PuntosAcopioJson(IEnumerable<CampesinApp.Dtos.PuntoAcopioOpcionDto> puntos) =>
            System.Text.Json.JsonSerializer.Serialize(puntos.Select(p => new { id = p.Id, titulo = p.Nombre, direccion = p.Direccion, lat = p.Lat, lng = p.Lng }), OpcionesJson);

        /// <summary>"4.628000, -74.064000" (punto decimal) o "No registradas" si el reporte no tiene coordenadas.</summary>
        public static string Coordenadas(double? latitud, double? longitud)
        {
            if (latitud == null || longitud == null) return "No registradas";
            var cultura = System.Globalization.CultureInfo.InvariantCulture;
            return $"{latitud.Value.ToString("0.000000", cultura)}, {longitud.Value.ToString("0.000000", cultura)}";
        }

        /// <summary>Devuelve "campo-error" (borde negro resaltado) si el campo tiene errores en el ModelState.</summary>
        public static string ClaseError(ViewDataDictionary viewData, string campo)
        {
            return viewData.ModelState.TryGetValue(campo, out var entrada) && entrada.Errors.Count > 0 ? "campo-error" : string.Empty;
        }

        public static string MensajeError(ViewDataDictionary viewData, string campo)
        {
            return viewData.ModelState.TryGetValue(campo, out var entrada)
                ? entrada.Errors.Select(e => e.ErrorMessage).FirstOrDefault(m => !string.IsNullOrWhiteSpace(m) && m != "Campo obligatorio") ?? string.Empty
                : string.Empty;
        }

        public static string Estrellas(int cantidad)
        {
            cantidad = Math.Clamp(cantidad, 0, 5);
            return new string('★', cantidad) + new string('☆', 5 - cantidad);
        }

        public static string ClaseEstado(string estado) => estado switch
        {
            EstadosEnvio.EntregadoRecibido => "bg-success",
            EstadosEnvio.EntregadoConObservaciones => "bg-warning text-dark",
            EstadosEnvio.Entregado => "bg-info text-dark",
            EstadosEnvio.EnTransito => "bg-primary",
            EstadosEnvio.Asignado => "bg-secondary",
            _ => "bg-light text-dark border"
        };
    }
}
