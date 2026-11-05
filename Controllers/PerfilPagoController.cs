// Importamos las librerías necesarias de ASP.NET Core y Entity Framework
using System;
using System.Linq;
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
        public async Task<IActionResult> Index()
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

            // 3. Preparamos el ViewModel con los datos iniciales
            var model = new PerfilPagoViewModel
            {
                IdUsuario = usuario.IdUsuario,
                // RN-01-CU014: El número de documento validado debe coincidir con la sesión activa
                DocumentoTitular = usuario.CedulaUsu.ToString()
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
        public async Task<IActionResult> Guardar(PerfilPagoViewModel model)
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

            try
            {
                // 6. Preparación de la entidad perfil_Pago:
                // Usamos exclusivamente las columnas exactas de la tabla perfil_Pago en CAMPESINAPP_3:
                // id_Usuario, id_Tipo_Cuenta, id_Tipo_Pago, id_Enti_Financ, num_Cuenta, cvc
                var perfilPago = new PerfilPago
                {
                    // Llave foránea del usuario activo
                    IdUsuario = usuario.IdUsuario,

                    // Llave foránea de tipo_Cuenta (Ahorros / Corriente)
                    IdTipoCuenta = model.IdTipoCuenta,

                    // Llave foránea de tipo_Pago (Tarjeta, Débito, etc.)
                    IdTipoPago = model.IdTipoPago,

                    // Llave foránea de entidad_Financiera (Banco Agrario, Bancolombia, etc.)
                    IdEntiFinanc = model.IdEntiFinanc,

                    // Para num_Cuenta (varchar(12) en BD): guardamos los últimos dígitos enmascarados o el número recibido
                    NumCuenta = model.NumCuenta.Length > 12 
                        ? model.NumCuenta.Substring(model.NumCuenta.Length - 12) 
                        : model.NumCuenta,

                    // BUENA PRÁCTICA DE SEGURIDAD (PCI-DSS):
                    // En el campo cvc (varchar(100) en BD) almacenamos el Token generado por Openpay
                    // de modo que nunca guardamos el CVC real en texto plano y cumplimos el esquema de la BD.
                    Cvc = (model.TokenOpenpay ?? "").Length > 100 
                        ? (model.TokenOpenpay ?? "").Substring(0, 100) 
                        : (model.TokenOpenpay ?? "")
                };

                // 7. Insertamos el nuevo registro en la tabla perfil_Pago
                _context.PerfilPagos.Add(perfilPago);

                // 8. Confirmamos los cambios en MySQL
                await _context.SaveChangesAsync();

                // 9. Mostramos la ventana emergente de proceso exitoso con temporizador de 4s
                ViewBag.RegistroExitoso = true;
                ViewBag.MensajeExito = "Perfil de pago registrado con éxito.";
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
    }
}

