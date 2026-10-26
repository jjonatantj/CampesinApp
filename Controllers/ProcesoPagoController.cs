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

        public ProcesoPagoController(Campesinapp3Context context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
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
                return RedirectToAction("Index", "PerfilPago");
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
                        return View(model);
                    }
                }
                catch { }
            }

            // Paso E: Si no hay borrador en sesión, buscamos la demanda en la base de datos (por id o última creada)
            Demandum? demanda = null;

            if (idDemanda.HasValue && idDemanda.Value > 0)
            {
                demanda = await _context.Demanda
                    .Include(d => d.IdTipoProductoNavigation)
                    .Include(d => d.IdVariedadNavigation)
                    .Include(d => d.IdPuntoAcopioNavigation)
                    .FirstOrDefaultAsync(d => d.IdDemanda == idDemanda.Value && d.IdUsuario == usuario.IdUsuario);
            }
            else
            {
                // Si no hay borrador ni ID específico, buscamos la última demanda pendiente de pago en la BD
                demanda = await _context.Demanda
                    .Include(d => d.IdTipoProductoNavigation)
                    .Include(d => d.IdVariedadNavigation)
                    .Include(d => d.IdPuntoAcopioNavigation)
                    .Where(d => d.IdUsuario == usuario.IdUsuario)
                    .OrderByDescending(d => d.IdDemanda)
                    .FirstOrDefaultAsync();
            }

            if (demanda == null)
            {
                TempData["MensajeInfo"] = "No se encontró ningún pedido pendiente de pago asociado a su cuenta.";
                return RedirectToAction("Index", "PublicarDemanda");
            }

            await PoblarDatosDemandaAsync(model, demanda, usuario.IdUsuario);
            return View(model);
        }

        // =========================================================================
        // ACCIÓN 2: POST /ProcesoPago/ConfirmarPago
        // Ejecuta el cargo con Openpay (Private Key + Customer) y publica el pedido SOLO si el pago es exitoso
        // =========================================================================
        [HttpPost("")]
        [HttpPost("ConfirmarPago")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmarPago(ProcesoPagoViewModel model)
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
                    usuario: usuario
                );

                // Si Openpay RECHAZA el pago:
                if (!resultadoCargo.Aprobado)
                {
                    // Regla de Negocio: ¡NO SE INSERTA NINGÚN REGISTRO EN LA BASE DE DATOS!
                    ModelState.AddModelError("", resultadoCargo.MensajeError ?? "El pago no pudo ser procesado. Verifique los datos o fondos de su tarjeta.");
                    await PoblarDesdeBorradorAsync(model, borrador, idUsuarioSesion.Value);
                    return View("Index", model);
                }

                // 2. Si Openpay APRUEBA el pago: procedemos a guardar la demanda y el pago en la base de datos
                var nuevaDemanda = new Demandum
                {
                    IdUsuario = idUsuarioSesion.Value,
                    IdTipoProducto = borrador.IdTipoProducto,
                    IdVariedad = borrador.IdVariedad,
                    CantidadDeman = borrador.Cantidad,
                    PrecioDeman = borrador.PrecioEstimado,
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

                TempData["MensajeExito"] = $"¡Pago de ${valorTotalPagar:N0} recibido con éxito! Tu demanda ha sido publicada y el dinero queda custodiado para aportes campesinos 🌾.";
                return RedirectToAction("Index", "PantallaPrincipal");
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
                usuario: usuario
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

            _context.PagoCompradors.Add(pagoGuardado);
            demandaExistente.IdEstadoDeman = 1; // Demanda confirmada y activa
            await _context.SaveChangesAsync();

            TempData["MensajeExito"] = $"¡Pago de ${montoPagar:N0} recibido con éxito! El dinero ha sido retenido y tu demanda está activa para aportes 🌾.";
            return RedirectToAction("Index", "PantallaPrincipal");
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
            double tarifaFleteTransportador = 150000;
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
            Usuario usuario)
        {
            string merchantId = _configuration["Openpay:MerchantId"] ?? "";
            string privateKey = _configuration["Openpay:PrivateApiKey"] ?? "";
            bool isSandbox = _configuration.GetValue<bool>("Openpay:SandboxMode", true);

            if (string.IsNullOrWhiteSpace(merchantId) || string.IsNullOrWhiteSpace(privateKey) || string.IsNullOrWhiteSpace(tokenTarjeta))
            {
                return (false, "Configuración de pasarela de pago o medio de pago no disponible.", null);
            }

            // Validación de longitud del token para Openpay (longitud máxima: 25 caracteres)
            if (tokenTarjeta.Length > 25 || tokenTarjeta.StartsWith("tkn_sandbox_"))
            {
                return (false, "El medio de pago seleccionado corresponde a un registro simulado anterior con un token no reconocido por Openpay. Por favor selecciona tu tarjeta real en el menú desplegable o regístrala nuevamente en Perfil de Pago.", null);
            }

            try
            {
                string pais = _configuration["Openpay:Pais"] ?? "CO";
                string dominio = pais.Equals("MX", StringComparison.OrdinalIgnoreCase) ? "openpay.mx" : "openpay.co";
                string baseUrl = isSandbox 
                    ? $"https://sandbox-api.{dominio}/v1/{merchantId}/charges" 
                    : $"https://api.{dominio}/v1/{merchantId}/charges";

                using var client = new HttpClient();

                var credencialesBytes = Encoding.ASCII.GetBytes($"{privateKey}:");
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(credencialesBytes));

                string telefono = usuario.TelelfonoUsu.ToString();
                if (telefono.Length < 10)
                {
                    telefono = "300" + telefono.PadLeft(7, '0');
                }

                var payload = new
                {
                    source_id = tokenTarjeta,
                    method = "card",
                    amount = monto,
                    currency = "COP",
                    description = descripcion,
                    device_session_id = string.IsNullOrWhiteSpace(deviceSessionId) ? "device_session_default" : deviceSessionId,
                    customer = new
                    {
                        name = string.IsNullOrWhiteSpace(usuario.NombreCom) ? "Usuario" : usuario.NombreCom.Trim(),
                        last_name = string.IsNullOrWhiteSpace(usuario.ApellidoCom) ? "CampesinApp" : usuario.ApellidoCom.Trim(),
                        email = string.IsNullOrWhiteSpace(usuario.CorreoUsu) ? "comprador@campesinapp.test" : usuario.CorreoUsu.Trim(),
                        phone_number = telefono
                    }
                };

                var jsonContent = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                var response = await client.PostAsync(baseUrl, jsonContent);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    string? idTransaccion = null;
                    try
                    {
                        using var doc = JsonDocument.Parse(responseBody);
                        if (doc.RootElement.TryGetProperty("id", out var idProp))
                        {
                            idTransaccion = idProp.GetString();
                        }
                    }
                    catch { }

                    return (true, null, idTransaccion);
                }
                else
                {
                    string mensajeTraducido = "El banco emisor o la pasarela declinó la transacción. Por favor verifique el medio de pago.";
                    try
                    {
                        using var errorDoc = JsonDocument.Parse(responseBody);
                        int errorCode = 0;
                        string? descripcionApi = null;

                        if (errorDoc.RootElement.TryGetProperty("error_code", out var codeProp))
                        {
                            errorCode = codeProp.GetInt32();
                        }
                        if (errorDoc.RootElement.TryGetProperty("description", out var descProp))
                        {
                            descripcionApi = descProp.GetString();
                        }

                        if (!string.IsNullOrWhiteSpace(descripcionApi))
                        {
                            mensajeTraducido = descripcionApi;
                        }
                        else
                        {
                            mensajeTraducido = errorCode switch
                            {
                                1001 => "Los datos del pago requeridos por la entidad bancaria están incompletos.",
                                2004 => "El código de seguridad (CVC) o los datos de la tarjeta son inválidos.",
                                2005 => "La tarjeta seleccionada se encuentra vencida.",
                                3001 => "La tarjeta fue declinada por la entidad bancaria emisora.",
                                3002 => "La tarjeta se encuentra inactiva o expirada.",
                                3003 => "Fondos insuficientes en la tarjeta o cuenta para cubrir el monto del pedido.",
                                3004 => "La tarjeta se encuentra bloqueada por el banco.",
                                3005 => "Transacción rechazada por políticas de seguridad del banco emisor.",
                                _ => mensajeTraducido
                            };
                        }
                    }
                    catch { }

                    return (false, mensajeTraducido, null);
                }
            }
            catch (Exception ex)
            {
                return (false, "Error de comunicación con la pasarela de pagos: " + ex.Message, null);
            }
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
                catch { }
            }

            await RecargarListaMetodosPagoAsync(model, idUsuario);
        }

        private async Task RecargarListaMetodosPagoAsync(ProcesoPagoViewModel model, int idUsuario)
        {
            var perfiles = await _context.PerfilPagos
                .Include(p => p.IdEntiFinancNavigation)
                .Include(p => p.IdTipoCuentaNavigation)
                .Where(p => p.IdUsuario == idUsuario)
                .ToListAsync();

            model.ListaMetodosPago = perfiles.Select(p => new SelectListItem
            {
                Value = p.IdPerfilPago.ToString(),
                Text = (p.Cvc != null && (p.Cvc.Length > 25 || p.Cvc.StartsWith("tkn_sandbox_")))
                    ? $"{p.IdEntiFinancNavigation.EntiFinanc} - {p.IdTipoCuentaNavigation.TipoCuenta} (•••• {p.NumCuenta.Substring(Math.Max(0, p.NumCuenta.Length - 4))}) [Prueba anterior - No válido]"
                    : $"{p.IdEntiFinancNavigation.EntiFinanc} - {p.IdTipoCuentaNavigation.TipoCuenta} (•••• {p.NumCuenta.Substring(Math.Max(0, p.NumCuenta.Length - 4))})"
            }).ToList();
        }
    }
}

