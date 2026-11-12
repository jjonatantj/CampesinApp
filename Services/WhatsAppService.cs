using System.Net.Http.Headers;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace CampesinApp.Services
{
    public static class WhatsAppService
    {
        // Credenciales de Meta Developers
        private static readonly string PhoneNumberId = "1369880379540977";
        private static readonly string AccessToken = "EAAP7RFwGyeIBSiZCwcyy0O8jo6JUAaZCgZAO7LbtSy4ijh74aHKl8GnDOSgHgYQl8CIC00zd3SHOsRZADeK8z1ntnFs7ZBkAI5c0uUytCpG6EW2vgloYdpQYxbaeUgF6gjQQRqy2MoDa3ZA1TRdOTxv4qB7RFy4C3uzpP1yUI3uvwnx7ZAcGYp8VtFKGDlXgF5ZCNZA9rmoIADN8fkwaE4UmgUZC01wOBHqEWWeL4jbCj9ZA9xxfGzEzEW5VLN9mg5tFN9rZA4WNHK1jHOUhapOa9i5E7IZAE";

        public static async Task<bool> EnviarCodigoWhatsAppAsync(string numeroDestino, string codigoSms)
        {
            try
            {
                // Formatear el número para Colombia (57 + 10 dígitos)
                string numeroLimpio = numeroDestino.Replace(" ", "").Replace("+", "").Replace("-", "");
                if (!numeroLimpio.StartsWith("57") && numeroLimpio.Length == 10)
                {
                    numeroLimpio = "57" + numeroLimpio;
                }

                using (var httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AccessToken);

                    // Formato de payload enviando el mensaje directo con el código
                    var payload = new
                    {
                        messaging_product = "whatsapp",
                        recipient_type = "individual",
                        to = numeroLimpio,
                        type = "text",
                        text = new
                        {
                            preview_url = false,
                            body = $"🔐 Tu código de verificación para CampesinApp es: *{codigoSms}*\n\nEste código expira en 5 minutos."
                        }
                    };

                    string jsonPayload = JsonSerializer.Serialize(payload);
                    var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                    string url = $"https://graph.facebook.com/v18.0/{PhoneNumberId}/messages";
                    HttpResponseMessage response = await httpClient.PostAsync(url, content);

                    if (response.IsSuccessStatusCode)
                    {
                        Debug.WriteLine($"---> [ÉXITO WHATSAPP] Mensaje enviado a {numeroLimpio}");
                        return true;
                    }

                    string responseError = await response.Content.ReadAsStringAsync();
                    Debug.WriteLine($"---> [ERROR WHATSAPP]: {responseError}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"---> [EXCEPCIÓN WHATSAPP]: {ex.Message}");
                return false;
            }
        }
    }
}