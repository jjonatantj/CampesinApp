using System;
using Microsoft.AspNetCore.Http;

namespace CampesinApp.Services
{
    /// <summary>
    /// Métodos de extensión para HttpRequest que aíslan la inspección de cabeceras de transporte HTTP
    /// evitando accesos crudos en los controladores MVC (Cumplimiento de la regla SonarQube S6960).
    /// </summary>
    public static class HttpRequestExtensions
    {
        private const string CabeceraAjax = "X-Requested-With";
        private const string ValorXmlHttpRequest = "XMLHttpRequest";

        /// <summary>
        /// Determina si la petición HTTP entrante corresponde a una llamada asíncrona AJAX.
        /// </summary>
        public static bool EsPeticionAjax(this HttpRequest? request)
        {
            if (request?.Headers == null)
            {
                return false;
            }

            return request.Headers.TryGetValue(CabeceraAjax, out var valor) &&
                   string.Equals(valor, ValorXmlHttpRequest, StringComparison.OrdinalIgnoreCase);
        }
    }
}

