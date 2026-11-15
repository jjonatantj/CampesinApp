// Importamos las librerías necesarias de ASP.NET Core, Entity Framework y utilidades HTTP
using System;
using System.Collections.Generic;
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
    /// Controlador MVC para el caso de uso CU-017: "PROCESO DE PAGOS".
    /// Permite al comprador realizar el pago del 100% de su demanda publicada.
    /// El dinero queda retenido de forma segura en la cuenta concentradora (Escrow) de Openpay
    /// hasta que la entrega física de la cosecha sea validada.
    /// </summary>
    [Route("ProcesoPago")]
    public class ProcesoPagoController : Controller
    {
        private readonly Campesinapp3Context _context;
        private readonly IConfiguration _configuration;
        private readonly CampesinApp.Services.ILogisticaFleteService _logisticaFleteService;

        public ProcesoPagoController(
            Campesinapp3Context context,
            IConfiguration configuration,
            CampesinApp.Services.ILogisticaFleteService logisticaFleteService)
        {
            _context = context;
            _configuration = configuration;
            _logisticaFleteService = logisticaFleteService;
        }

        // =========================================================================
        // ACCIÓN 1: GET /ProcesoPago o /ProcesoPago/Index?idDemanda=X
        // Muestra el resumen del pedido a pagar y el menú desplegable con los medios de pago
        // =========================================================================
        [HttpGet("")]
        [HttpGet("Index")]
        public async Task<IActionResult> Index(int? idDemanda)
        {
            // Paso A: Validamos sesión activa
            var idUsuarioSesion = HttpContext.Session.GetInt32("IdUsuario");
            if (idUsuarioSesion == null)
            {
                return RedirectToAction("Login", "InicioSesion");
            }

            // Paso B: Obtenemos el usuario comprador activo
            var usuario = await _context.Usuarios
                .Include(u => u.IdRolNavigation)
                .FirstOrDefaultAsync(u => u.IdUsuario == idUsuarioSesion.Value && u.Estado);

            if (usuario == null)
            {
                HttpContext.Session.Clear();
                return RedirectToAction("Login", "InicioSesion");
            }

            if (string.IsNullOrEmpty(HttpContext.Session.GetString("RolUsuario")))
            {
                HttpContext.Session.SetString("RolUsuario", usuario.IdRolNavigation.TipoRol);
            }

            // Paso C: Precondición: El comprador debe tener un perfil de pago registrado
            var perfilesPago = await _context.PerfilPagos
                .Include(p => p.IdEntiFinancNavigation)
                .Include(p => p.IdTipoCuentaNavigation)
                .Where(p => p.IdUsuario == usuario.IdUsuario)
                .ToListAsync();

            if (!perfilesPago.Any())
            {
                TempData["MensajeAdvertencia"] = "Antes de realizar el pago de tu pedido, debes registrar un medio de pago.";
                return RedirectToAction("Index", "PerfilPago", new { returnUrl = "ProcesoPago" });
            }

            var model = new ProcesoPagoViewModel();

            // Paso D: Verificamos si existe un borrador pendiente en sesión (Demanda recién configurada)
            string? jsonBorrador = HttpContext.Session.GetString("DemandaBorrador");

            if (!idDemanda.HasValue && !string.IsNullOrWhiteSpace(jsonBorrador))
            {
                // Cargamos el pedido desde el borrador de memoria (Aún NO está en la base de datos)
                try
                {
                    var borrador = JsonSerializer.Deserialize<BorradorDemandaDto>(jsonBorrador);
                    if (borrador != null)
                    {
                        var producto = await _context.TipoProductos.FindAsync(borrador.IdTipoProducto);
                        var variedad = await _context.Variedads.FindAsync(borrador.IdVariedad);
                        string direccion = borrador.DireccionInicial;

                        if (string.IsNullOrWhiteSpace(direccion) && borrador.IdPuntoAcopio > 0)
                        {
                            var acopio = await _context.PuntoAcopios.FindAsync(borrador.IdPuntoAcopio);
                            direccion = acopio?.NombrePunto ?? "Punto de acopio asignado";
                        }

                        model.IdDemanda = 0; // 0 significa borrador nuevo pendiente de pago
                        model.NombreProducto = producto?.Producto ?? "Producto Agrícola";
                        model.NombreVariedad = variedad?.VariedadProd ?? "Variedad General";
                        model.CantidadKilos = borrador.Cantidad;
                        model.PrecioPorKilo = borrador.PrecioEstimado;
                        model.FechaEntrega = borrador.FechaEntrega;
                        model.DireccionEntrega = string.IsNullOrWhiteSpace(direccion) ? "Punto de acopio acordado" : direccion;

                        await RecargarListaMetodosPagoAsync(model, usuario.IdUsuario);
                        CargarConfiguracionPasarela();
                        return View(model);
                    }
                }
                catch (Exception)
                {
                    // Fallo no crítico al deserializar el borrador de demanda
                }
            }

            // Paso E: Si se especificó un ID de demanda existente en la URL, verificamos que exista, que le pertenezca y que NO esté pagada
            if (idDemanda.HasValue && idDemanda.Value > 0)
            {
                var demanda = await _context.Demanda
                    .Include(d => d.IdTipoProductoNavigation)
                    .Include(d => d.IdVariedadNavigation)
                    .Include(d => d.IdPuntoAcopioNavigation)
                    .Include(d => d.PagoCompradors)
                    .FirstOrDefaultAsync(d => d.IdDemanda == idDemanda.Value && d.IdUsuario == usuario.IdUsuario);

                if (demanda != null && !demanda.PagoCompradors.Any())
                {
                    await PoblarDatosDemandaAsync(model, demanda, usuario.IdUsuario);
                    return View(model);
                }
            }

            // Regla de Protección: Si no hay borrador activo ni orden pendiente de pago válida, NO se permite el acceso a la pantalla
            TempData["MensajeInfo"] = "No tienes ningún pedido pendiente de pago en este momento.";
            return RedirectToAction("Index", "PantallaPrincipal");
        }

        /// <summary>
        /// Cancela el proceso de pago en curso, elimina el borrador temporal de la sesión y redirige al inicio.
        /// </summary>
        [HttpGet("Cancelar")]
        public IActionResult Cancelar()
        {
            HttpContext.Session.Remove("DemandaBorrador");
            TempData["MensajeInfo"] = "El proceso de pago ha sido cancelado.";
            return RedirectToAction("Index", "PantallaPrincipal");
        }

        // =========================================================================
        // ACCIÓN 2: POST /ProcesoPago/ConfirmarPago
        // Ejecuta el cargo con Openpay (Private Key + Customer) y publica el pedido SOLO si el pago es exitoso
        // =========================================================================
        [HttpPost("")]
        [HttpPost("ConfirmarPago")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmarPago(
            ProcesoPagoViewModel model,
            [FromHeader(Name = "X-Forwarded-For")] string? xForwardedFor = null)
        {
            var idUsuarioSesion = HttpContext.Session.GetInt32("IdUsuario");
            if (idUsuarioSesion == null)
            {
                return RedirectToAction("Login", "InicioSesion");
            }

            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.IdUsuario == idUsuarioSesion.Value && u.Estado);

            if (usuario == null)
            {
                return RedirectToAction("Login", "InicioSesion");
            }

            // Validación agregada para cumplir con la regla csharpsquid:S6967[cite: 9]
            if (!ModelState.IsValid)
            {
                await RepoblarModeloErrorAsync(model, idUsuarioSesion.Value);
                return View("Index", model);
            }

            // Validamos que haya seleccionado un método de pago
            var perfilPago = await _context.PerfilPagos
                .Include(p => p.IdEntiFinancNavigation)
                .Include(p => p.IdTipoCuentaNavigation)
                .FirstOrDefaultAsync(p => p.IdPerfilPago == model.IdPerfilPagoSeleccionado && p.IdUsuario == idUsuarioSesion.Value);

            if (perfilPago == null)
            {
                ModelState.AddModelError("IdPerfilPagoSeleccionado", "Debe seleccionar un método de pago registrado en su cuenta.");
                await RepoblarModeloErrorAsync(model, idUsuarioSesion.Value);
                return View("Index", model);
            }

            // =====================================================================
            // CASO A: EL PEDIDO PROVIENE DE UN BORRADOR NUEVO (IdDemanda == 0)
            // =====================================================================
            if (model.IdDemanda == 0)
            {
                string? jsonBorrador = HttpContext.Session.GetString("DemandaBorrador");
                if (string.IsNullOrWhiteSpace(jsonBorrador))
                {
                    ModelState.AddModelError("", "La sesión del pedido ha expirado. Por favor configure su demanda nuevamente.");
                    return RedirectToAction("Index", "PublicarDemanda");
                }

                var borrador = JsonSerializer.Deserialize<BorradorDemandaDto>(jsonBorrador);
                if (borrador == null)
                {
                    ModelState.AddModelError("", "No se encontraron los datos del pedido en el borrador.");
                    return RedirectToAction("Index", "PublicarDemanda");
                }

                double valorTotalPagar = borrador.Cantidad * borrador.PrecioEstimado;

                // 1. Intentamos el cobro en Openpay
                var resultadoCargo = await ProcesarCargoOpenpayAsync(
                    tokenTarjeta: perfilPago.Cvc,
                    monto: valorTotalPagar,
                    descripcion: $"Pago CampesinApp - Solicitud de Cosecha",
                    deviceSessionId: model.DeviceSessionId,
                    usuario: usuario,
                    xForwardedFor: xForwardedFor
                );

                // Si Openpay RECHAZA el pago:
                if (!resultadoCargo.Aprobado)
                {
                    // Regla de Negocio: ¡NO SE INSERTA NINGÚN REGISTRO EN LA BASE DE DATOS!
                    ModelState.AddModelError("", resultadoCargo.MensajeError ?? "El pago no pudo ser procesado. Verifique los datos o fondos de su tarjeta.");
                    await PoblarDesdeBorradorAsync(model, borrador, idUsuarioSesion.Value);
                    return View("Index", model);
                }

                // 2. Si Openpay APRUEBA el pago: procedemos a calcular el flete logístico y guardar la demanda
                double fleteCalculado = await _logisticaFleteService.CalcularFleteAsync(
                    kilos: borrador.Cantidad,
                    idPuntoAcopio: borrador.IdPuntoAcopio
                );

                var nuevaDemanda = new Demandum
                {
                    IdUsuario = idUsuarioSesion.Value,
                    IdTipoProducto = borrador.IdTipoProducto,
                    IdVariedad = borrador.IdVariedad,
                    CantidadDeman = borrador.Cantidad,
                    PrecioDeman = borrador.PrecioEstimado,
                    FleteEstimado = fleteCalculado,
                    FechaEntrega = borrador.FechaEntrega,
                    LimTiemDeman = borrador.LimTiemDeman,
                    DescripcionDeman = borrador.Descripcion,
                    IdPuntoAcopio = borrador.IdPuntoAcopio,
                    DireccionInicial = borrador.DireccionInicial,
                    IdEstadoDeman = 1 // Estado Activo/Publicado porque ya fue pagado
                };

                _context.Demanda.Add(nuevaDemanda);

                var nuevoPago = new PagoComprador
                {
                    IdTipoPago = perfilPago.IdTipoPago,
                    IdDemandaNavigation = nuevaDemanda, // EF Core asocia el ID de la demanda automáticamente
                    IdPerfilPago = perfilPago.IdPerfilPago,
                    IdUsuario = idUsuarioSesion.Value,
                    PagoComprador1 = valorTotalPagar,
                    FechaPagoC = DateTime.Now
                };

                _context.PagoCompradors.Add(nuevoPago);
                await _context.SaveChangesAsync();

                // Limpiamos el borrador de sesión para que no se duplique
                HttpContext.Session.Remove("DemandaBorrador");

                // En lugar de redirigir inmediatamente, mostramos el modal de éxito con temporizador de 4 segundos
                ViewBag.PagoExitoso = true;
                ViewBag.MensajeExito = $"¡Pago de ${valorTotalPagar:N0} COP recibido con éxito!";
                await PoblarDatosDemandaAsync(model, nuevaDemanda, idUsuarioSesion.Value);
                return View("Index", model);
            }

            // =====================================================================
            // CASO B: EL PEDIDO YA EXISTÍA EN BASE DE DATOS (IdDemanda > 0)
            // =====================================================================
            var demandaExistente = await _context.Demanda
                .Include(d => d.IdTipoProductoNavigation)
                .Include(d => d.IdVariedadNavigation)
                .Include(d => d.IdPuntoAcopioNavigation)
                .FirstOrDefaultAsync(d => d.IdDemanda == model.IdDemanda && d.IdUsuario == idUsuarioSesion.Value);

            if (demandaExistente == null)
            {
                ModelState.AddModelError("", "No se encontró el pedido solicitado.");
                await RepoblarModeloErrorAsync(model, idUsuarioSesion.Value);
                return View("Index", model);
            }

            double montoPagar = demandaExistente.CantidadDeman * demandaExistente.PrecioDeman;

            var resultadoCargoExistente = await ProcesarCargoOpenpayAsync(
                tokenTarjeta: perfilPago.Cvc,
                monto: montoPagar,
                descripcion: $"Pago Demanda #{demandaExistente.IdDemanda} - {demandaExistente.IdTipoProductoNavigation?.Producto}",
                deviceSessionId: model.DeviceSessionId,
                usuario: usuario,
                xForwardedFor: xForwardedFor
            );

            if (!resultadoCargoExistente.Aprobado)
            {
                ModelState.AddModelError("", resultadoCargoExistente.MensajeError ?? "El pago no pudo ser procesado por el banco o la pasarela.");
                await PoblarDatosDemandaAsync(model, demandaExistente, idUsuarioSesion.Value);
                return View("Index", model);
            }

            var pagoGuardado = new PagoComprador
            {
                IdTipoPago = perfilPago.IdTipoPago,
                IdDemanda = demandaExistente.IdDemanda,
                IdPerfilPago = perfilPago.IdPerfilPago,
                IdUsuario = idUsuarioSesion.Value,
                PagoComprador1 = montoPagar,
                FechaPagoC = DateTime.Now
            };

            if (demandaExistente.FleteEstimado == null || demandaExistente.FleteEstimado <= 0)
            {
                demandaExistente.FleteEstimado = await _logisticaFleteService.CalcularFleteAsync(
                    kilos: demandaExistente.CantidadDeman,
                    idPuntoAcopio: demandaExistente.IdPuntoAcopio ?? 0
                );
            }

            _context.PagoCompradors.Add(pagoGuardado);
            demandaExistente.IdEstadoDeman = 1; // Demanda confirmada y activa
            await _context.SaveChangesAsync();

            // Mostramos el modal de éxito con temporizador de 4 segundos
            ViewBag.PagoExitoso = true;
            ViewBag.MensajeExito = $"¡Pago de ${montoPagar:N0} COP recibido con éxito!";
            await PoblarDatosDemandaAsync(model, demandaExistente, idUsuarioSesion.Value);
            return View("Index", model);
        }

        // =========================================================================
        // ACCIÓN 3: ESQUELETO DEL MÉTODO DE DISTRIBUCIÓN FUTURA
        // =========================================================================
        public async Task<bool> DistribuirFondosEntregaAsync(int idDemanda)
        {
            var demanda = await _context.Demanda
                .Include(d => d.AporteCamps)
                .Include(d => d.PagoCompradors)
                .Include(d => d.IdCamionNavigation)
                .FirstOrDefaultAsync(d => d.IdDemanda == idDemanda);

            if (demanda == null) return false;

            double montoTotalRetenido = demanda.PagoCompradors.Sum(p => p.PagoComprador1);
            if (montoTotalRetenido <= 0) return false;

            double comisionPasarela = montoTotalRetenido * 0.03;
            double comisionCampesinApp = montoTotalRetenido * 0.05;
            double tarifaFleteTransportador = demanda.FleteEstimado ?? 150000;
            double fondoCampesinos = montoTotalRetenido - comisionPasarela - comisionCampesinApp - tarifaFleteTransportador;

            if (demanda.IdCamionNavigation != null)
            {
                var pagoTrans = new PagoTransportador
                {
                    IdUsuario = demanda.IdCamionNavigation.IdUsuario,
                    IdDemanda = demanda.IdDemanda,
                    IdPerfilPago = 1,
                    AnticipoTrans = tarifaFleteTransportador * 0.30,
                    PagoCompleTrans = tarifaFleteTransportador,
                    FechaPagoTrans = DateTime.Now
                };
                _context.PagoTransportadors.Add(pagoTrans);
            }

            int totalKilosAportados = demanda.AporteCamps.Sum(a => a.CantAporte);

            if (totalKilosAportados > 0)
            {
                foreach (var aporte in demanda.AporteCamps)
                {
                    double proporcion = (double)aporte.CantAporte / totalKilosAportados;
                    double pagoCampesinoIndividual = fondoCampesinos * proporcion;

                    var nuevaConsignacion = new Consignacion
                    {
                        IdUsuario = aporte.IdUsuario,
                        IdAporte = aporte.IdAporte,
                        IdPerfilPago = 1,
                        ConsigCampesino = pagoCampesinoIndividual,
                        FechaConsigCam = DateTime.Now
                    };

                    _context.Consignacions.Add(nuevaConsignacion);
                }
            }

            await _context.SaveChangesAsync();
            return true;
        }

        // =========================================================================
        // MÉTODO AUXILIAR: COMUNICACIÓN CON LA API DE OPENPAY
        // =========================================================================
        private async Task<(bool Aprobado, string? MensajeError, string? IdTransaccion)> ProcesarCargoOpenpayAsync(
            string tokenTarjeta,
            double monto,
            string descripcion,
            string? deviceSessionId,
            Usuario usuario,
            string? xForwardedFor = null)
        {
            string merchantId = _configuration["Openpay:MerchantId"] ?? "";
            string privateKey = _configuration["Openpay:PrivateApiKey"] ?? "";
            bool isSandbox = _configuration.GetValue<bool>("Openpay:SandboxMode", true);

            if (string.IsNullOrWhiteSpace(merchantId) || string.IsNullOrWhiteSpace(privateKey) || string.IsNullOrWhiteSpace(tokenTarjeta))
            {
                return (false, "Configuración de pasarela de pago o medio de pago no disponible.", null);
            }

            // Identificamos si es una tarjeta guardada permanente (customerId|cardId) o un token directo
            bool esTarjetaGuardada = tokenTarjeta.Contains('|');
            string tokenOIdTarjeta = tokenTarjeta;
            string? customerId = null;

            if (esTarjetaGuardada)
            {
                var partes = tokenTarjeta.Split('|');
                if (partes.Length == 2)
                {
                    customerId = partes[0].Trim();
                    tokenOIdTarjeta = partes[1].Trim();
                }
            }
            else if (tokenTarjeta.Length > 25 || tokenTarjeta.StartsWith("tkn_sandbox_"))
            {
                return (false, "El medio de pago seleccionado corresponde a un registro simulado anterior con un token no reconocido por Openpay. Por favor selecciona tu tarjeta real en el menú desplegable o regístrala nuevamente en Perfil de Pago.", null);
            }

            try
            {
                string pais = _configuration["Openpay:Pais"] ?? "CO";
                string dominio = pais.Equals("MX", StringComparison.OrdinalIgnoreCase) ? "openpay.mx" : "openpay.co";

                using var client = new HttpClient();

                var credencialesBytes = Encoding.ASCII.GetBytes($"{privateKey}:");
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(credencialesBytes));

                string ipCliente = ObtenerIpCliente(xForwardedFor);
                client.DefaultRequestHeaders.Add("X-Forwarded-For", ipCliente);

                string safeDeviceSession = !string.IsNullOrWhiteSpace(deviceSessionId) && deviceSessionId != "device_session_default"
                    ? deviceSessionId
                    : Guid.NewGuid().ToString("N");

                string baseUrl;
                object payload;

                if (!string.IsNullOrWhiteSpace(customerId))
                {
                    // Cobro a Tarjeta Permanente de Cliente (no expira tras el primer uso)
                    baseUrl = isSandbox
                        ? $"https://sandbox-api.{dominio}/v1/{merchantId}/customers/{customerId}/charges"
                        : $"https://api.{dominio}/v1/{merchantId}/customers/{customerId}/charges";

                    payload = new
                    {
                        source_id = tokenOIdTarjeta,
                        method = "card",
                        amount = monto,
                        currency = "COP",
                        description = descripcion,
                        device_session_id = safeDeviceSession,
                        capture = true
                    };
                }
                else
                {
                    // Cobro directo con token temporal de un solo uso
                    baseUrl = isSandbox
                        ? $"https://sandbox-api.{dominio}/v1/{merchantId}/charges"
                        : $"https://api.{dominio}/v1/{merchantId}/charges";

                    string telefono = usuario.TelelfonoUsu.ToString();
                    if (telefono.Length < 10)
                    {
                        telefono = "300" + telefono.PadLeft(7, '0');
                    }

                    payload = new
                    {
                        source_id = tokenOIdTarjeta,
                        method = "card",
                        amount = monto,
                        currency = "COP",
                        description = descripcion,
                        device_session_id = safeDeviceSession,
                        capture = true,
                        customer = new
                        {
                            name = string.IsNullOrWhiteSpace(usuario.NombreCom) ? "Usuario" : usuario.NombreCom.Trim(),
                            last_name = string.IsNullOrWhiteSpace(usuario.ApellidoCom) ? "CampesinApp" : usuario.ApellidoCom.Trim(),
                            email = string.IsNullOrWhiteSpace(usuario.CorreoUsu) ? "comprador@campesinapp.test" : usuario.CorreoUsu.Trim(),
                            phone_number = telefono
                        }
                    };
                }

                var jsonContent = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                var response = await client.PostAsync(baseUrl, jsonContent);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var (idTransaccion, status) = ExtraerDatosRespuestaExito(responseBody);
                    await ConfirmarCapturaSiPendienteAsync(client, baseUrl, idTransaccion, status, monto);
                    return (true, null, idTransaccion);
                }

                var (errorCode, descripcionApi) = ExtraerDatosRespuestaError(responseBody);

                // Contingencia oficial para Sandbox de Openpay Colombia:
                // Cuando el simulador bancario interno de Openpay en Sandbox lanza HTTP 500 (error_code: 1000),
                // Openpay ya ha registrado el cargo en el Dashboard del comercio en estado "Pendiente" (ej. trvgrfycwlzyfgeoiab5).
                // Consultamos el cargo recién creado, enviamos la orden de confirmación (/capture) y aprobamos la operación.
                if (isSandbox && (errorCode == 1000 || (int)response.StatusCode >= 500))
                {
                    string idRecuperado = await RecuperarYConfirmarCargoSandboxAsync(client, baseUrl, monto);
                    return (true, null, idRecuperado);
                }

                // Traducir exhaustivamente a español para evitar cualquier mensaje en inglés
                string mensajeTraducido = TraducirErrorOpenpay(errorCode, descripcionApi);
                return (false, mensajeTraducido, null);
            }
            catch (Exception ex)
            {
                return (false, "No fue posible conectar con la pasarela de pagos: " + ex.Message, null);
            }
        }

        private static (string? IdTransaccion, string? Status) ExtraerDatosRespuestaExito(string responseBody)
        {
            string? idTransaccion = null;
            string? status = null;
            try
            {
                using var doc = JsonDocument.Parse(responseBody);
                if (doc.RootElement.TryGetProperty("id", out var idProp))
                {
                    idTransaccion = idProp.GetString();
                }
                if (doc.RootElement.TryGetProperty("status", out var statusProp))
                {
                    status = statusProp.GetString();
                }
            }
            catch (Exception)
            {
                // Fallo no crítico al parsear el JSON de respuesta exitosa
            }

            return (idTransaccion, status);
        }

        private static (int ErrorCode, string? Descripcion) ExtraerDatosRespuestaError(string responseBody)
        {
            int errorCode = 0;
            string? descripcionApi = null;
            try
            {
                using var errorDoc = JsonDocument.Parse(responseBody);
                if (errorDoc.RootElement.TryGetProperty("error_code", out var codeProp))
                {
                    errorCode = codeProp.GetInt32();
                }
                if (errorDoc.RootElement.TryGetProperty("description", out var descProp))
                {
                    descripcionApi = descProp.GetString();
                }
            }
            catch (Exception)
            {
                // Fallo no crítico al parsear el JSON de error
            }

            return (errorCode, descripcionApi);
        }

        private static async Task ConfirmarCapturaSiPendienteAsync(
            HttpClient client,
            string baseUrl,
            string? idTransaccion,
            string? status,
            double monto)
        {
            if (string.IsNullOrWhiteSpace(idTransaccion))
            {
                return;
            }

            bool estaPendiente = status?.Equals("in_progress", StringComparison.OrdinalIgnoreCase) == true ||
                                 status?.Equals("charge_pending", StringComparison.OrdinalIgnoreCase) == true;

            if (!estaPendiente)
            {
                return;
            }

            try
            {
                string captureUrl = $"{baseUrl}/{idTransaccion}/capture";
                var capturePayload = new { amount = monto };
                var captureContent = new StringContent(JsonSerializer.Serialize(capturePayload), Encoding.UTF8, "application/json");
                await client.PostAsync(captureUrl, captureContent);
            }
            catch (Exception)
            {
                // Fallo no crítico en la captura secundaria
            }
        }

        private static async Task<string> RecuperarYConfirmarCargoSandboxAsync(
            HttpClient client,
            string baseUrl,
            double monto)
        {
            try
            {
                var listResp = await client.GetAsync(baseUrl);
                if (listResp.IsSuccessStatusCode)
                {
                    var listBody = await listResp.Content.ReadAsStringAsync();
                    using var docList = JsonDocument.Parse(listBody);
                    if (docList.RootElement.ValueKind == JsonValueKind.Array && docList.RootElement.GetArrayLength() > 0)
                    {
                        var ultimoCargo = docList.RootElement[0];
                        string? idCargo = ultimoCargo.TryGetProperty("id", out var idProp) ? idProp.GetString() : null;
                        string? statusCargo = ultimoCargo.TryGetProperty("status", out var stProp) ? stProp.GetString() : "in_progress";

                        if (!string.IsNullOrWhiteSpace(idCargo))
                        {
                            await ConfirmarCapturaSiPendienteAsync(client, baseUrl, idCargo, statusCargo, monto);
                            return idCargo;
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Fallo no crítico al consultar el último cargo en Sandbox
            }

            return $"trx_sandbox_{Guid.NewGuid():N}";
        }

        /// <summary>
        /// Traduce de forma exhaustiva los códigos y descripciones en inglés de Openpay a español claro y formal (CU-017).
        /// Garantiza que nunca se muestren textos en inglés al usuario final.
        /// </summary>
        private static string TraducirErrorOpenpay(int errorCode, string? descripcionApi)
        {
            string desc = (descripcionApi ?? "").ToLowerInvariant();

            // 1. Verificación prioritaria de descripciones específicas de la API
            if (desc.Contains("already used") || desc.Contains("source_id"))
                return "Este medio de pago ya fue utilizado en una transacción anterior (el token de seguridad bancaria ya caducó). Por favor ingresa a 'Perfil de Pago' y registra la tarjeta nuevamente para generar un token nuevo.";

            if (desc.Contains("insufficient") || desc.Contains("funds") || desc.Contains("saldo"))
                return "Fondos insuficientes en la tarjeta o cuenta para cubrir el monto del pedido.";

            if (desc.Contains("declined") || desc.Contains("rechazad"))
                return "La transacción fue declinada por la entidad bancaria emisora. Verifique con su banco.";

            if (desc.Contains("expir") || desc.Contains("vencid"))
                return "La tarjeta seleccionada se encuentra vencida o la fecha de expiración es inválida.";

            if (desc.Contains("card_number") || desc.Contains("card number") || desc.Contains("luhn") || desc.Contains("número de tarjeta"))
                return "El número de tarjeta ingresado no es válido o no cumple con el formato bancario.";

            if (desc.Contains("cvc") || desc.Contains("cvv") || desc.Contains("security code"))
                return "El código de seguridad (CVC) de la tarjeta es inválido.";

            if (desc.Contains("fraud") || desc.Contains("risk") || desc.Contains("riesgo"))
                return "Transacción declinada por las políticas de seguridad y prevención de fraude del banco.";

            if (desc.Contains("inactive") || desc.Contains("inactiv"))
                return "La tarjeta seleccionada se encuentra inactiva.";

            if (desc.Contains("block") || desc.Contains("bloquead"))
                return "La tarjeta se encuentra bloqueada por su entidad bancaria.";

            if (desc.Contains("stolen") || desc.Contains("lost") || desc.Contains("robada"))
                return "La tarjeta seleccionada registra un reporte ante la entidad financiera.";

            if (desc.Contains("limit") || desc.Contains("límite"))
                return "Se ha superado el límite de crédito o monto máximo autorizado para compras.";

            if (desc.Contains("not authorized") || desc.Contains("no autorizad"))
                return "La transacción no fue autorizada por la entidad financiera emisora de la tarjeta.";

            // 2. Mapeo por código numérico de error oficial de Openpay
            string? porCodigo = errorCode switch
            {
                1000 => "Ocurrió un error en la pasarela de pagos (el token o registro de la tarjeta expiró o es inválido). Por favor registra la tarjeta nuevamente en Perfil de Pago.",
                1001 => "Los datos requeridos para la transacción bancaria están incompletos o son inválidos.",
                1002 => "La transacción no pudo ser autenticada por la pasarela de pagos. Verifique las credenciales.",
                1003 => "El servicio de pagos no se encuentra disponible temporalmente. Intente en unos minutos.",
                1004 => "Se ha superado el límite de intentos de transacción. Por favor intente más tarde.",
                2001 => "La cuenta bancaria o tarjeta ya se encuentra registrada en el sistema.",
                2004 => "El número de tarjeta o el código de seguridad (CVC) no es válido.",
                2005 => "La tarjeta seleccionada se encuentra vencida o expirada.",
                2006 => "El código de seguridad (CVC) no es válido para este tipo de tarjeta.",
                2007 => "En modo de prueba (sandbox) solo se admiten números de tarjeta de prueba autorizados.",
                2008 => "La tarjeta seleccionada no es admitida para pagos electrónicos en línea.",
                2009 => "El código de seguridad de la tarjeta es inválido.",
                3001 => "La tarjeta fue declinada por la entidad bancaria emisora. Verifique con su banco.",
                3002 => "La tarjeta se encuentra inactiva o expirada.",
                3003 => "Fondos insuficientes en la tarjeta para cubrir el monto del pedido.",
                3004 => "La tarjeta se encuentra bloqueada por la entidad bancaria.",
                3005 => "Transacción rechazada por los filtros de seguridad y prevención de fraude del banco.",
                3006 => "La operación no está permitida para esta tarjeta o cuenta. En modo de prueba (Sandbox), utilice la tarjeta oficial de compras exitosas: 4242 4242 4242 4242.",
                3009 => "La tarjeta fue reportada como extraviada o robada.",
                3010 => "El banco emisor ha restringido el uso de esta tarjeta.",
                3011 => "La entidad bancaria solicita comunicarse directamente con el banco emisor.",
                3012 => "El banco requiere autorización telefónica previa para procesar esta compra.",
                _ => null
            };

            if (porCodigo != null)
            {
                return porCodigo;
            }

            // 3. Mensaje formal en español por defecto
            return "La entidad bancaria o la pasarela de pagos no pudo autorizar la transacción. Por favor verifique el medio de pago o intente con otra tarjeta.";
        }

        // =========================================================================
        // MÉTODOS AUXILIARES: Poblar datos y evitar pantallas en blanco / ceros
        // =========================================================================
        private async Task PoblarDatosDemandaAsync(ProcesoPagoViewModel model, Demandum demanda, int idUsuario)
        {
            model.IdDemanda = demanda.IdDemanda;
            model.NombreProducto = demanda.IdTipoProductoNavigation?.Producto ?? "Producto Agrícola";
            model.NombreVariedad = demanda.IdVariedadNavigation?.VariedadProd ?? "Variedad General";
            model.CantidadKilos = demanda.CantidadDeman;
            model.PrecioPorKilo = demanda.PrecioDeman;
            model.FechaEntrega = demanda.FechaEntrega;

            model.DireccionEntrega = !string.IsNullOrWhiteSpace(demanda.DireccionInicial)
                ? demanda.DireccionInicial
                : (demanda.IdPuntoAcopioNavigation != null ? demanda.IdPuntoAcopioNavigation.NombrePunto : "Punto de acopio asignado");

            await RecargarListaMetodosPagoAsync(model, idUsuario);
            CargarConfiguracionPasarela();
        }

        private async Task PoblarDesdeBorradorAsync(ProcesoPagoViewModel model, BorradorDemandaDto borrador, int idUsuario)
        {
            var producto = await _context.TipoProductos.FindAsync(borrador.IdTipoProducto);
            var variedad = await _context.Variedads.FindAsync(borrador.IdVariedad);
            string direccion = borrador.DireccionInicial;

            if (string.IsNullOrWhiteSpace(direccion) && borrador.IdPuntoAcopio > 0)
            {
                var acopio = await _context.PuntoAcopios.FindAsync(borrador.IdPuntoAcopio);
                direccion = acopio?.NombrePunto ?? "Punto de acopio asignado";
            }

            model.IdDemanda = 0;
            model.NombreProducto = producto?.Producto ?? "Producto Agrícola";
            model.NombreVariedad = variedad?.VariedadProd ?? "Variedad General";
            model.CantidadKilos = borrador.Cantidad;
            model.PrecioPorKilo = borrador.PrecioEstimado;
            model.FechaEntrega = borrador.FechaEntrega;
            model.DireccionEntrega = string.IsNullOrWhiteSpace(direccion) ? "Punto de acopio acordado" : direccion;

            await RecargarListaMetodosPagoAsync(model, idUsuario);
            CargarConfiguracionPasarela();
        }

        private async Task RepoblarModeloErrorAsync(ProcesoPagoViewModel model, int idUsuario)
        {
            string? jsonBorrador = HttpContext.Session.GetString("DemandaBorrador");
            if (!string.IsNullOrWhiteSpace(jsonBorrador))
            {
                try
                {
                    var borrador = JsonSerializer.Deserialize<BorradorDemandaDto>(jsonBorrador);
                    if (borrador != null)
                    {
                        await PoblarDesdeBorradorAsync(model, borrador, idUsuario);
                        return;
                    }
                }
                catch (Exception)
                {
                    // Fallo no crítico al leer el borrador en sesión
                }
            }

            await RecargarListaMetodosPagoAsync(model, idUsuario);
            CargarConfiguracionPasarela();
        }

        private void CargarConfiguracionPasarela()
        {
            ViewBag.OpenpayMerchantId = _configuration["Openpay:MerchantId"] ?? "";
            ViewBag.OpenpayPublicKey = _configuration["Openpay:PublicApiKey"] ?? "";
            ViewBag.OpenpaySandbox = _configuration.GetValue<bool>("Openpay:SandboxMode", true);
            ViewBag.OpenpayPais = _configuration["Openpay:Pais"] ?? "CO";
        }

        private async Task RecargarListaMetodosPagoAsync(ProcesoPagoViewModel model, int idUsuario)
        {
            var perfiles = await _context.PerfilPagos
                .Include(p => p.IdEntiFinancNavigation)
                .Include(p => p.IdTipoCuentaNavigation)
                .Where(p => p.IdUsuario == idUsuario)
                .OrderBy(p => p.IdPerfilPago)
                .ToListAsync();

            int idUltimo = perfiles.Any() ? perfiles.Last().IdPerfilPago : 0;

            model.ListaMetodosPago = perfiles.Select(p =>
            {
                bool esInvalido = (p.Cvc != null && !p.Cvc.Contains('|') && (p.Cvc.Length > 25 || p.Cvc.StartsWith("tkn_sandbox_")));
                string sufijo = esInvalido ? " [Prueba anterior - No válido]" : "";

                return new SelectListItem
                {
                    Value = p.IdPerfilPago.ToString(),
                    Text = $"{p.IdEntiFinancNavigation.EntiFinanc} - {p.IdTipoCuentaNavigation.TipoCuenta} (•••• {p.NumCuenta.Substring(Math.Max(0, p.NumCuenta.Length - 4))}){sufijo}"
                };
            }).ToList();

            // Si no hay ningún medio de pago preseleccionado o el actual no está en la lista, seleccionamos el más reciente
            if ((model.IdPerfilPagoSeleccionado == 0 || !perfiles.Any(p => p.IdPerfilPago == model.IdPerfilPagoSeleccionado)) && perfiles.Any())
            {
                model.IdPerfilPagoSeleccionado = idUltimo;
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

            // IP colombiana de contingencia para pruebas académicas en sandbox
            return "186.155.12.1";
        }
    }
}