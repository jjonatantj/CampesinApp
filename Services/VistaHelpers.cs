using CampesinApp.Repositories.Models;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace CampesinApp.Services
{
    public static class VistaHelpers
    {
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
