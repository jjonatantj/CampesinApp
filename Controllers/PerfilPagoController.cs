// Importamos las librerías necesarias de ASP.NET Core y Entity Framework
using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using CampesinApp.Repositories.Models;
using CampesinApp.ViewModels;

namespace CampesinApp.Controllers
{
    /// <summary>
    /// Controlador MVC para el caso de uso CU-014: "COMPLETAR PERFIL DE PAGO".
    /// Permite a Compradores y Campesinos registrar su cuenta bancaria o medio de pago con pasarela Openpay.
    /// </summary>
    [Route("PerfilPago")]
    public class PerfilPagoController : Controller
    {
        // Contexto de base de datos para interactuar con las tablas de CAMPESINAPP_3
        private readonly Campesinapp3Context _context;

        // Acceso al archivo appsettings.json para leer la configuración de Openpay
        private readonly IConfiguration _configuration;

        // Constructor con inyección de dependencias clásica
        public PerfilPagoController(Campesinapp3Context context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // =========================================================================
        // ACCIÓN 1: GET /PerfilPago o /PerfilPago/Index
        // Muestra el formulario con los campos solicitados por el CU-014
        // =========================================================================
        [HttpGet("")]
        [HttpGet("Index")]
        public async Task<IActionResult> Index([FromQuery] string? returnUrl = null)
        {
            // 1. Verificamos si existe una sesión activa en la aplicación
            var idUsuario = HttpContext.Session.GetInt32("IdUsuario");
            if (idUsuario == null)
            {
                // Si el usuario no ha iniciado sesión, se le redirige al inicio de sesión (CU-004)
                return RedirectToAction("Login", "InicioSesion");
            }

            // 2. Buscamos el usuario en la base de datos para obtener su información
            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.IdUsuario == idUsuario.Value && u.Estado);

            if (usuario == null)
            {
                // Si el usuario no existe o está inactivo, cerramos la sesión por seguridad
                HttpContext.Session.Clear();
                return RedirectToAction("Login", "InicioSesion");
            }

            // Aseguramos que la sesión mantenga el rol para la barra privada (_NavPrivada)
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("RolUsuario")))
            {
                var rol = await _context.Rols.FirstOrDefaultAsync(r => r.IdRol == usuario.IdRol);
                if (rol != null)
                {
                    HttpContext.Session.SetString("RolUsuario", rol.TipoRol);
                }
            }

            // Detectamos si proviene del flujo de publicación de demanda (CU-010 / CU-017)
            bool vieneDeDemanda = returnUrl == "ProcesoPago" || !string.IsNullOrWhiteSpace(HttpContext.Session.GetString("DemandaBorrador"));
            ViewBag.VieneDeDemanda = vieneDeDemanda;

            // 3. Preparamos el ViewModel con los datos iniciales
            var model = new PerfilPagoViewModel
            {
                IdUsuario = usuario.IdUsuario,
                // RN-01-CU014: El número de documento validado debe coincidir con la sesión activa
                DocumentoTitular = usuario.CedulaUsu.ToString(),
                ReturnUrl = returnUrl
            };

            // 4. Llenamos las listas desplegables desde la base de datos (tipo_Cuenta, entidad_Financiera, tipo_Pago)
            await PoblarCatalogosAsync(model);

            // 5. Pasamos las llaves públicas de Openpay a la vista mediante ViewBag para facilitar el modo Sandbox
            ViewBag.OpenpayMerchantId = _configuration["Openpay:MerchantId"] ?? "m9xxxxxxsandboxid";
            ViewBag.OpenpayPublicKey = _configuration["Openpay:PublicApiKey"] ?? "pk_xxxxxxxxxxxxxxxxxxxxxxxx";
            ViewBag.OpenpaySandbox = _configuration.GetValue<bool>("Openpay:SandboxMode", true);
            ViewBag.OpenpayPais = _configuration["Openpay:Pais"] ?? "CO";

            // 6. Retornamos la vista con el modelo cargado
            return View(model);
        }

        // =========================================================================
        // ACCIÓN 2: POST /PerfilPago/Guardar
        // Recibe los datos diligenciados, valida la sesión y el token de Openpay, y guarda en BD
        // =========================================================================
        [HttpPost("")]
        [HttpPost("Guardar")]
        [ValidateAntiForgeryToken] // Evita peticiones maliciosas externas (CSRF)
        public async Task<IActionResult> Guardar(
            PerfilPagoViewModel model,
            [FromHeader(Name = "X-Forwarded-For")] string? xForwardedFor = null)
        {
            // 1. Validamos que el usuario mantenga su sesión activa
            var idUsuario = HttpContext.Session.GetInt32("IdUsuario");
            if (idUsuario == null)
            {
                return RedirectToAction("Login", "InicioSesion");
            }

            // 2. Obtenemos el usuario de la sesión desde la BD
            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.IdUsuario == idUsuario.Value && u.Estado);

            if (usuario == null)
            {
                HttpContext.Session.Clear();
                return RedirectToAction("Login", "InicioSesion");
            }

            // 3. REGLA DE NEGOCIO RN-01-CU014:
            // Validamos que el número de documento ingresado sea estrictamente igual al de la sesión
            if (model.DocumentoTitular?.Trim() != usuario.CedulaUsu.ToString())
            {
                ModelState.AddModelError("DocumentoTitular", "El número de documento no coincide con el registrado en su cuenta activa.");
            }

            // 4. VALIDACIÓN DE PASARELA (Openpay):
            // Verificamos que el script de Openpay.js haya generado y enviado el Token de seguridad
            if (string.IsNullOrWhiteSpace(model.TokenOpenpay))
            {
                // Flujo de excepción FE03-CU014: Error en pasarela de pagos
                ModelState.AddModelError("", "La pasarela de pagos Openpay no pudo validar la tarjeta/cuenta. Verifique sus datos o intente nuevamente.");
            }

            // 5. Si alguna validación de formulario o de negocio falló, recargamos la vista con los errores
            if (!ModelState.IsValid)
            {
                await PoblarCatalogosAsync(model);
                ViewBag.OpenpayMerchantId = _configuration["Openpay:MerchantId"] ?? "m9xxxxxxsandboxid";
                ViewBag.OpenpayPublicKey = _configuration["Openpay:PublicApiKey"] ?? "pk_xxxxxxxxxxxxxxxxxxxxxxxx";
                ViewBag.OpenpaySandbox = _configuration.GetValue<bool>("Openpay:SandboxMode", true);
                ViewBag.OpenpayPais = _configuration["Openpay:Pais"] ?? "CO";
                return View("Index", model);
            }

            // 5. REGISTRO PERMANENTE DE CLIENTE Y TARJETA EN OPENPAY (Customer & Saved Card)
            string ipCliente = ObtenerIpCliente(xForwardedFor);
            var resultadoOpenpay = await RegistrarTarjetaEnOpenpayAsync(usuario, model.TokenOpenpay!, model.DeviceSessionId, ipCliente);
            if (!resultadoOpenpay.Exito)
            {
                ModelState.AddModelError("", resultadoOpenpay.ErrorMensaje ?? "No fue posible registrar la tarjeta con la pasarela de pagos.");
                await PoblarCatalogosAsync(model);
                ViewBag.OpenpayMerchantId = _configuration["Openpay:MerchantId"] ?? "m9xxxxxxsandboxid";
                ViewBag.OpenpayPublicKey = _configuration["Openpay:PublicApiKey"] ?? "pk_xxxxxxxxxxxxxxxxxxxxxxxx";
                ViewBag.OpenpaySandbox = _configuration.GetValue<bool>("Openpay:SandboxMode", true);
                ViewBag.OpenpayPais = _configuration["Openpay:Pais"] ?? "CO";
                return View("Index", model);
            }

            try
            {
                // 6. Preparación de la entidad perfil_Pago:
                // Usamos exclusivamente las columnas exactas de la tabla perfil_Pago en CAMPESINAPP_3:
                // id_Usuario, id_Tipo_Cuenta, id_Tipo_Pago, id_Enti_Financ, num_Cuenta, cvc
                string numLimpio = (model.NumCuenta ?? "").Replace(" ", "").Replace("-", "");
                var perfilPago = new PerfilPago
                {
                    // Llave foránea del usuario activo
                    IdUsuario = usuario.IdUsuario,

                    // Llave foránea de tipo_Cuenta (Ahorros / Corriente)
                    IdTipoCuenta = model.IdTipoCuenta!.Value,

                    // Llave foránea de tipo_Pago (Tarjeta, Débito, etc.)
                    IdTipoPago = model.IdTipoPago!.Value,

                    // Llave foránea de entidad_Financiera (Banco Agrario, Bancolombia, etc.)
                    IdEntiFinanc = model.IdEntiFinanc!.Value,

                    // Para num_Cuenta (varchar(12) en BD): guardamos los últimos dígitos limpios
                    NumCuenta = numLimpio.Length > 12 
                        ? numLimpio.Substring(numLimpio.Length - 12) 
                        : numLimpio,

                    // BUENA PRÁCTICA DE SEGURIDAD (PCI-DSS):
                    // En el campo cvc (varchar(100) en BD) almacenamos el identificador compuesto permanente
                    // en formato customerId|cardId para permitir cargos recurrentes sin quemar el token temporal.
                    Cvc = (resultadoOpenpay.IdentificadorCompuesto ?? "").Length > 100 
                        ? (resultadoOpenpay.IdentificadorCompuesto ?? "").Substring(0, 100) 
                        : (resultadoOpenpay.IdentificadorCompuesto ?? "")
                };

                // 7. Insertamos el nuevo registro en la tabla perfil_Pago
                _context.PerfilPagos.Add(perfilPago);

                // 8. Confirmamos los cambios en MySQL
                await _context.SaveChangesAsync();

                // 9. Mostramos la ventana emergente de proceso exitoso con temporizador de 4s
                bool vieneDeDemanda = model.ReturnUrl == "ProcesoPago" || !string.IsNullOrWhiteSpace(HttpContext.Session.GetString("DemandaBorrador"));
                ViewBag.RegistroExitoso = true;
                ViewBag.VieneDeDemanda = vieneDeDemanda;
                if (vieneDeDemanda)
                {
                    ViewBag.MensajeExito = "Perfil de pago registrado con éxito. Redirigiendo al pago de tu pedido...";
                    ViewBag.UrlDestinoExito = Url.Action("Index", "ProcesoPago");
                }
                else
                {
                    ViewBag.MensajeExito = "Perfil de pago registrado con éxito.";
                    ViewBag.UrlDestinoExito = Url.Action("Index", "Perfil");
                }

                await PoblarCatalogosAsync(model);
                ViewBag.OpenpayMerchantId = _configuration["Openpay:MerchantId"] ?? "m9xxxxxxsandboxid";
                ViewBag.OpenpayPublicKey = _configuration["Openpay:PublicApiKey"] ?? "pk_xxxxxxxxxxxxxxxxxxxxxxxx";
                ViewBag.OpenpaySandbox = _configuration.GetValue<bool>("Openpay:SandboxMode", true);
                ViewBag.OpenpayPais = _configuration["Openpay:Pais"] ?? "CO";
                return View("Index", model);
            }
            catch (Exception ex)
            {
                // Flujo de excepción FE02-CU014: Error en Base de Datos
                ModelState.AddModelError("", "No fue posible guardar los datos bancarios. Intente nuevamente. Error: " + ex.Message);
                await PoblarCatalogosAsync(model);
                ViewBag.OpenpayMerchantId = _configuration["Openpay:MerchantId"] ?? "m9xxxxxxsandboxid";
                ViewBag.OpenpayPublicKey = _configuration["Openpay:PublicApiKey"] ?? "pk_xxxxxxxxxxxxxxxxxxxxxxxx";
                ViewBag.OpenpaySandbox = _configuration.GetValue<bool>("Openpay:SandboxMode", true);
                ViewBag.OpenpayPais = _configuration["Openpay:Pais"] ?? "CO";
                return View("Index", model);
            }
        }

        // =========================================================================
        // MÉTODO AUXILIAR: Carga las listas desplegables desde la base de datos
        // =========================================================================
        private async Task PoblarCatalogosAsync(PerfilPagoViewModel model)
        {
            // Entidades financieras (Bancos registrados en entidad_Financiera)
            model.ListaEntidadesFinancieras = await _context.EntidadFinancieras
                .OrderBy(e => e.EntiFinanc)
                .Select(e => new SelectListItem
                {
                    Value = e.IdEntiFinanc.ToString(),
                    Text = e.EntiFinanc
                })
                .ToListAsync();

            // Tipos de cuenta (tipo_Cuenta: Ahorros, Corriente, etc.)
            model.ListaTiposCuenta = await _context.TipoCuenta
                .OrderBy(tc => tc.TipoCuenta)
                .Select(tc => new SelectListItem
                {
                    Value = tc.IdTipoCuenta.ToString(),
                    Text = tc.TipoCuenta
                })
                .ToListAsync();

            // Tipos de pago (tipo_Pago)
            model.ListaTiposPago = await _context.TipoPagos
                .OrderBy(tp => tp.TipoPago1)
                .Select(tp => new SelectListItem
                {
                    Value = tp.IdTipoPago.ToString(),
                    Text = tp.TipoPago1
                })
                .ToListAsync();
        }

        // =========================================================================
        // MÉTODO AUXILIAR: REGISTRO DE CLIENTE Y TARJETA RECURRENTE EN OPENPAY
        // =========================================================================
        private async Task<(bool Exito, string? IdentificadorCompuesto, string? ErrorMensaje)> RegistrarTarjetaEnOpenpayAsync(
            Usuario usuario, 
            string tokenTarjeta, 
            string? deviceSessionId,
            string? ipCliente = null)
        {
            string merchantId = _configuration["Openpay:MerchantId"] ?? "";
            string privateKey = _configuration["Openpay:PrivateApiKey"] ?? "";
            bool isSandbox = _configuration.GetValue<bool>("Openpay:SandboxMode", true);
            string pais = _configuration["Openpay:Pais"] ?? "CO";

            if (string.IsNullOrWhiteSpace(merchantId) || string.IsNullOrWhiteSpace(privateKey))
            {
                return (false, null, "La configuración de Openpay no se encuentra disponible.");
            }

            try
            {
                string dominio = pais.Equals("MX", StringComparison.OrdinalIgnoreCase) ? "openpay.mx" : "openpay.co";
                string apiBase = isSandbox 
                    ? $"https://sandbox-api.{dominio}/v1/{merchantId}" 
                    : $"https://api.{dominio}/v1/{merchantId}";

                using var client = new HttpClient();
                var credencialesBytes = Encoding.ASCII.GetBytes($"{privateKey}:");
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(credencialesBytes));

                // Cabecera obligatoria de Openpay para antifraude y geolocalización
                if (!string.IsNullOrWhiteSpace(ipCliente))
                {
                    client.DefaultRequestHeaders.Add("X-Forwarded-For", ipCliente);
                }

                // 1. Obtener o crear el Cliente en Openpay
                string externalId = $"campesinapp_user_{usuario.IdUsuario}";
                string customerId = "";

                try
                {
                    var searchResp = await client.GetAsync($"{apiBase}/customers?external_id={externalId}");
                    if (searchResp.IsSuccessStatusCode)
                    {
                        var searchBody = await searchResp.Content.ReadAsStringAsync();
                        using var docSearch = JsonDocument.Parse(searchBody);
                        if (docSearch.RootElement.ValueKind == JsonValueKind.Array && docSearch.RootElement.GetArrayLength() > 0)
                        {
                            var primerCliente = docSearch.RootElement[0];
                            if (primerCliente.TryGetProperty("id", out var idProp))
                            {
                                customerId = idProp.GetString() ?? "";
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    // Fallo no crítico en la búsqueda del cliente; se procede a crear uno nuevo si no se encuentra
                }

                if (string.IsNullOrWhiteSpace(customerId))
                {
                    string telefono = usuario.TelelfonoUsu.ToString();
                    if (telefono.Length < 10)
                    {
                        telefono = "300" + telefono.PadLeft(7, '0');
                    }

                    var payloadCliente = new
                    {
                        external_id = externalId,
                        name = string.IsNullOrWhiteSpace(usuario.NombreCom) ? "Usuario" : usuario.NombreCom.Trim(),
                        last_name = string.IsNullOrWhiteSpace(usuario.ApellidoCom) ? "CampesinApp" : usuario.ApellidoCom.Trim(),
                        email = string.IsNullOrWhiteSpace(usuario.CorreoUsu) ? "usuario@campesinapp.test" : usuario.CorreoUsu.Trim(),
                        phone_number = telefono,
                        requires_account = false
                    };

                    var postClienteContent = new StringContent(JsonSerializer.Serialize(payloadCliente), Encoding.UTF8, "application/json");
                    var postClienteResp = await client.PostAsync($"{apiBase}/customers", postClienteContent);
                    var postClienteBody = await postClienteResp.Content.ReadAsStringAsync();

                    if (!postClienteResp.IsSuccessStatusCode)
                    {
                        string errorDesc = ExtraerErrorOpenpay(postClienteBody);
                        return (false, null, errorDesc);
                    }

                    using var docCli = JsonDocument.Parse(postClienteBody);
                    if (docCli.RootElement.TryGetProperty("id", out var idCliProp))
                    {
                        customerId = idCliProp.GetString() ?? "";
                    }
                }

                if (string.IsNullOrWhiteSpace(customerId))
                {
                    return (false, null, "No fue posible registrar el cliente en la pasarela de pagos.");
                }

                // 2. Asociar la tarjeta al Cliente de Openpay de forma permanente
                var payloadTarjeta = new
                {
                    token_id = tokenTarjeta,
                    device_session_id = string.IsNullOrWhiteSpace(deviceSessionId) ? "device_session_default" : deviceSessionId
                };

                var postTarjetaContent = new StringContent(JsonSerializer.Serialize(payloadTarjeta), Encoding.UTF8, "application/json");
                var postTarjetaResp = await client.PostAsync($"{apiBase}/customers/{customerId}/cards", postTarjetaContent);
                var postTarjetaBody = await postTarjetaResp.Content.ReadAsStringAsync();

                if (!postTarjetaResp.IsSuccessStatusCode)
                {
                    int errCode = 0;
                    try
                    {
                        using var errDoc = JsonDocument.Parse(postTarjetaBody);
                        if (errDoc.RootElement.TryGetProperty("error_code", out var cProp))
                        {
                            errCode = cProp.GetInt32();
                        }
                    }
                    catch (Exception)
                    {
                        // Fallo no crítico al parsear el JSON de error
                    }

                    // Código 2001: La tarjeta ya está registrada para este cliente
                    if (errCode == 2001)
                    {
                        var getCardsResp = await client.GetAsync($"{apiBase}/customers/{customerId}/cards");
                        if (getCardsResp.IsSuccessStatusCode)
                        {
                            var getCardsBody = await getCardsResp.Content.ReadAsStringAsync();
                            using var docCards = JsonDocument.Parse(getCardsBody);
                            if (docCards.RootElement.ValueKind == JsonValueKind.Array && docCards.RootElement.GetArrayLength() > 0)
                            {
                                var ultimaTarjeta = docCards.RootElement[docCards.RootElement.GetArrayLength() - 1];
                                if (ultimaTarjeta.TryGetProperty("id", out var idExistenteProp))
                                {
                                    string existingCardId = idExistenteProp.GetString() ?? "";
                                    if (!string.IsNullOrWhiteSpace(existingCardId))
                                    {
                                        return (true, $"{customerId}|{existingCardId}", null);
                                    }
                                }
                            }
                        }
                    }

                    string errorDesc = ExtraerErrorOpenpay(postTarjetaBody);
                    return (false, null, errorDesc);
                }

                string cardId = "";
                using var docCard = JsonDocument.Parse(postTarjetaBody);
                if (docCard.RootElement.TryGetProperty("id", out var idCardProp))
                {
                    cardId = idCardProp.GetString() ?? "";
                }

                if (string.IsNullOrWhiteSpace(cardId))
                {
                    return (false, null, "No fue posible registrar la tarjeta de crédito/débito con la pasarela.");
                }

                // 3. Devolvemos el identificador permanente compuesto: customerId|cardId
                return (true, $"{customerId}|{cardId}", null);
            }
            catch (Exception ex)
            {
                return (false, null, "Error de comunicación con la pasarela de pagos: " + ex.Message);
            }
        }

        // =========================================================================
        // MÉTODO AUXILIAR: TRADUCE ERRORES DE REGISTRO DE TARJETA EN OPENPAY
        // =========================================================================
        private string ExtraerErrorOpenpay(string responseBody)
        {
            try
            {
                using var doc = JsonDocument.Parse(responseBody);
                int codigo = 0;
                string desc = "";
                if (doc.RootElement.TryGetProperty("error_code", out var codProp))
                {
                    codigo = codProp.GetInt32();
                }
                if (doc.RootElement.TryGetProperty("description", out var descProp))
                {
                    desc = (descProp.GetString() ?? "").ToLower();
                }

                if (desc.Contains("already used") || desc.Contains("source_id"))
                {
                    return "El token de la tarjeta ya fue utilizado o expiró. Intente registrar la tarjeta de nuevo.";
                }
                if (desc.Contains("already registered") || codigo == 2001)
                {
                    return "La tarjeta ingresada ya se encuentra registrada en la pasarela de pagos.";
                }
                if (desc.Contains("card_number") || desc.Contains("card number") || desc.Contains("luhn") || codigo == 2004)
                {
                    return "El número de tarjeta no es válido o no cumple con el formato bancario.";
                }
                if (desc.Contains("cvc") || desc.Contains("cvv") || desc.Contains("security code") || codigo == 2006 || codigo == 2009)
                {
                    return "El código de seguridad (CVC) es inválido.";
                }
                if (desc.Contains("expir") || codigo == 2005)
                {
                    return "La tarjeta ingresada se encuentra vencida o inactiva.";
                }
                if (codigo == 1003)
                {
                    return "El servicio de pagos no se encuentra disponible temporalmente. Intente en unos minutos.";
                }
                if (codigo == 3006 || desc.Contains("request not allowed"))
                {
                    return "La operación no está permitida para esta tarjeta o comercio. En modo pruebas (Sandbox).";
                }

                return !string.IsNullOrWhiteSpace(desc) ? $"Error en pasarela de pagos: {desc}" : "No fue posible registrar los datos en la pasarela de pagos.";
            }
            catch (Exception)
            {
                return "Respuesta inesperada de la pasarela de pagos.";
            }
        }

        private string ObtenerIpCliente(string? xForwardedFor = null)
        {
            if (!string.IsNullOrWhiteSpace(xForwardedFor))
            {
                string ip = xForwardedFor.Split(',')[0].Trim();
                if (!string.IsNullOrWhiteSpace(ip) && ip != "::1" && ip != "127.0.0.1")
                {
                    return ip;
                }
            }

            var remoteIp = HttpContext.Connection.RemoteIpAddress?.ToString();
            if (!string.IsNullOrWhiteSpace(remoteIp) && remoteIp != "::1" && remoteIp != "127.0.0.1")
            {
                return remoteIp;
            }

            // IP colombiana de contingencia para pruebas académicas en sandbox leída de configuración (Regla SonarQube S1313)
            return _configuration["Openpay:IpContingenciaSandbox"] ?? string.Empty;
        }
    }
}

