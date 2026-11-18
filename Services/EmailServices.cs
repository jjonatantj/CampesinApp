using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using CampesinApp.Dtos;
using MailKit.Security;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using MimeKit;

namespace CampesinApp.Services
{
    public static class EmailService
    {
        private static IConfiguration? _configuration;

        /// <summary>
        /// Inicializa la configuración para leer los secretos de forma segura.
        /// Se debe llamar desde Program.cs al arrancar la aplicación.
        /// </summary>
        public static void Initialize(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private static readonly string SmtpServer = "smtp.gmail.com";
        private static readonly int SmtpPort = 587;

        // Credenciales obtenidas de forma segura (Secretos de usuario o Variables de entorno)
        private static string EmailEmisor => _configuration?["EmailSettings:Email"] ?? string.Empty;
        private static string PasswordEmisor => _configuration?["EmailSettings:Password"] ?? string.Empty;

        // Datos de contacto y redes configurables
        private static readonly string NumeroWhatsApp = "+57 300 000 0000";
        private static readonly string HorarioAtencion = "Lunes a Sábado de 8:00 a.m. a 6:00 p.m.";
        private static readonly string EnlaceFacebook = "https://facebook.com/campesinapp";
        private static readonly string EnlaceInstagram = "https://instagram.com/campesinapp";

        /// <summary>
        /// Despacho síncrono reutilizable mediante MailKit para evitar duplicación de código.
        /// </summary>
        private static bool EnviarMensajeSmtp(MimeMessage message, string logTipo, string correoDestino)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(EmailEmisor) || string.IsNullOrWhiteSpace(PasswordEmisor))
                {
                    Debug.WriteLine($"---> [AVISO MAILKIT {logTipo}]: No hay credenciales configuradas en EmailSettings.");
                    return false;
                }

                using var client = new MailKit.Net.Smtp.SmtpClient();
                client.Connect(SmtpServer, SmtpPort, SecureSocketOptions.StartTls);
                client.Authenticate(EmailEmisor, PasswordEmisor);
                client.Send(message);
                client.Disconnect(true);

                Debug.WriteLine($"---> [ÉXITO MAILKIT {logTipo}] Correo enviado a {correoDestino}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"---> [ERROR MAILKIT {logTipo}]: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Despacho asíncrono reutilizable mediante MailKit para no bloquear hilos en procesos transaccionales.
        /// </summary>
        private static async Task<bool> EnviarMensajeSmtpAsync(MimeMessage message, string logTipo, string correoDestino)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(EmailEmisor) || string.IsNullOrWhiteSpace(PasswordEmisor))
                {
                    Debug.WriteLine($"---> [AVISO MAILKIT {logTipo}]: No hay credenciales configuradas en EmailSettings.");
                    return false;
                }

                using var client = new MailKit.Net.Smtp.SmtpClient();
                await client.ConnectAsync(SmtpServer, SmtpPort, SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(EmailEmisor, PasswordEmisor);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                Debug.WriteLine($"---> [ÉXITO MAILKIT {logTipo}] Correo enviado a {correoDestino}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"---> [ERROR MAILKIT {logTipo}]: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Método auxiliar para construir el correo incrustando físicamente la imagen desde wwwroot/images/
        /// </summary>
        private static MimeMessage ConstruirMensajeConLogoFisico(
            string correoDestino,
            string nombreUsuario,
            string asunto,
            string contenidoHtmlCore,
            string colorBorde,
            IWebHostEnvironment? environment,
            bool incluirOpenpay = false)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("CampesinApp Colombia", EmailEmisor));
            message.To.Add(new MailboxAddress(nombreUsuario, correoDestino));
            message.Subject = asunto;

            var builder = new BodyBuilder();

            // Referencia única para el ID de contenido del logo
            string cidLogo = "logoCampesinAppUnico";
            bool imagenAgregadaExitosamente = false;

            if (environment != null && !string.IsNullOrEmpty(environment.WebRootPath))
            {
                string rutaLogoJpg = Path.Combine(environment.WebRootPath, "images", "logo.jpeg");
                string rutaLogoPng = Path.Combine(environment.WebRootPath, "images", "logo.png");
                string rutaLogoMayus = Path.Combine(environment.WebRootPath, "images", "logo.JPG");

                string rutaFinal = string.Empty;

                if (File.Exists(rutaLogoJpg)) rutaFinal = rutaLogoJpg;
                else if (File.Exists(rutaLogoPng)) rutaFinal = rutaLogoPng;
                else if (File.Exists(rutaLogoMayus)) rutaFinal = rutaLogoMayus;

                if (!string.IsNullOrEmpty(rutaFinal))
                {
                    var recursoImagen = builder.LinkedResources.Add(rutaFinal);
                    recursoImagen.ContentId = cidLogo;
                    imagenAgregadaExitosamente = true;
                }

                if (incluirOpenpay)
                {
                    string rutaOpenpay = Path.Combine(environment.WebRootPath, "images", "OPENPAY_logo.png");
                    if (File.Exists(rutaOpenpay))
                    {
                        var recursoOpenpay = builder.LinkedResources.Add(rutaOpenpay);
                        recursoOpenpay.ContentId = "logoOpenpayUnico";
                    }
                }
            }

            string tagImgLogo = imagenAgregadaExitosamente
                ? $"<img src='cid:{cidLogo}' alt='CampesinApp Logo' width='80' height='80' style='display: block; margin: 0 auto; border-radius: 50%; border: 2px solid {colorBorde}; object-fit: cover;' />"
                : $"<h2 style='color: {colorBorde}; margin: 0;'>CampesinApp</h2>";

            builder.HtmlBody = $@"
                <div style='font-family: Arial, sans-serif; padding: 20px; background-color: #f4f6f8;'>
                    <div style='max-width: 520px; margin: auto; background: white; padding: 30px; border-radius: 10px; border-top: 5px solid {colorBorde}; box-shadow: 0 4px 6px rgba(0,0,0,0.05);'>
                        
                        <div style='text-align: center; margin-bottom: 20px;'>
                            {tagImgLogo}
                        </div>

                        {contenidoHtmlCore}
                        
                        <div style='background-color: #f9f9f9; padding: 15px; border-radius: 8px; margin: 25px 0; font-size: 12px; color: #555; text-align: center;'>
                            <p style='margin: 0 0 8px 0; color: {colorBorde}; font-weight: bold;'>⚠️ Buzón automático (no respondas a este correo).</p>
                            <p style='margin: 0 0 5px 0;'>¿Dudas o inconvenientes? Escríbenos al WhatsApp: <strong>{NumeroWhatsApp}</strong></p>
                            <p style='margin: 0;'><strong>Horario de atención:</strong> {HorarioAtencion}</p>
                        </div>

                        <div style='text-align: center; margin-bottom: 20px;'>
                            <a href='{EnlaceFacebook}' target='_blank' style='color: {colorBorde}; text-decoration: none; margin: 0 10px; font-size: 12px; font-weight: bold;'>Facebook</a> | 
                            <a href='{EnlaceInstagram}' target='_blank' style='color: {colorBorde}; text-decoration: none; margin: 0 10px; font-size: 12px; font-weight: bold;'>Instagram</a>
                        </div>

                        <hr style='border: none; border-top: 1px solid #eee; margin: 20px 0 15px 0;' />
                        <p style='font-size: 11px; color: #888; text-align: center; line-height: 1.4; margin: 0;'>
                            CampesinApp - Plataforma de Comercialización Agrícola Directa.<br>
                            Este mensaje y sus servicios asociados están sujetos a los <strong>Términos y Condiciones</strong> de uso institucional y a nuestras políticas de privacidad y protección de datos.
                        </p>
                    </div>
                </div>";

            message.Body = builder.ToMessageBody();
            return message;
        }

        public static bool EnviarCodigoVerificacion(string correoDestino, string nombreUsuario, string codigoCorreo, IWebHostEnvironment? environment = null)
        {
            string contenidoHtml = $@"
                <h2 style='color: #2e7d32; text-align: center; margin-top: 0;'>¡Bienvenido a CampesinApp!</h2>
                <p>Hola <strong>{nombreUsuario}</strong>,</p>
                <p>Tu código de verificación para completar el registro es:</p>
                <div style='text-align: center; margin: 25px 0;'>
                    <span style='background-color: #e8f5e9; color: #1b5e20; font-size: 32px; font-weight: bold; letter-spacing: 5px; padding: 10px 25px; border-radius: 8px; border: 1px dashed #2e7d32;'>{codigoCorreo}</span>
                </div>
                <p style='color: #d32f2f; font-weight: bold; font-size: 13px; text-align: center;'>⏰ Este código caduca estrictamente en 5 minutos.</p>";

            var message = ConstruirMensajeConLogoFisico(correoDestino, nombreUsuario, "Código de Verificación - Registro CampesinApp", contenidoHtml, "#2e7d32", environment);
            return EnviarMensajeSmtp(message, "VERIFICACIÓN", correoDestino);
        }

        public static bool EnviarCodigoRecuperacionPassword(string correoDestino, string nombreUsuario, string codigoCorreo, IWebHostEnvironment? environment = null)
        {
            string contenidoHtml = $@"
                <h2 style='color: #2e7d32; text-align: center; margin-top: 0;'>Recuperación de Contraseña</h2>
                <p>Hola <strong>{nombreUsuario}</strong>,</p>
                <p>Hemos recibido una solicitud para restablecer la contraseña de tu cuenta en <strong>CampesinApp</strong>.</p>
                <p>Tu código de verificación para continuar es:</p>
                <div style='text-align: center; margin: 25px 0;'>
                    <span style='background-color: #e8f5e9; color: #1b5e20; font-size: 32px; font-weight: bold; letter-spacing: 5px; padding: 10px 25px; border-radius: 8px; border: 1px dashed #2e7d32;'>{codigoCorreo}</span>
                </div>
                <p style='color: #d32f2f; font-weight: bold; font-size: 13px; text-align: center;'>⏰ Este código caduca estrictamente en 5 minutos.</p>
                <p style='font-size: 12px; color: #888; text-align: center; margin-top: 15px;'>
                    Si no solicitaste este cambio, puedes ignorar este mensaje de forma segura.
                </p>";

            var message = ConstruirMensajeConLogoFisico(correoDestino, nombreUsuario, "Código de Verificación - Recuperación de Contraseña CampesinApp", contenidoHtml, "#2e7d32", environment);
            return EnviarMensajeSmtp(message, "RECUPERACIÓN", correoDestino);
        }

        public static bool EnviarNotificacionAdmin(string correoDestino, string nombreUsuario, string cedula, string passwordTemporal, IWebHostEnvironment? environment = null)
        {
            string contenidoHtml = $@"
                <h2 style='color: #2e7d32; text-align: center; margin-bottom: 20px; margin-top: 0;'>¡Cuenta Administrador Creada!</h2>
                <p style='font-size: 15px; color: #333;'>Hola <strong>{nombreUsuario}</strong>,</p>
                <p style='font-size: 14px; color: #555; line-height: 1.5;'>
                    Se ha creado tu cuenta de <strong>Administrador</strong> en <strong>CampesinApp</strong> con éxito. A continuación tus credenciales de acceso temporal:
                </p>
                <div style='background-color: #e8f5e9; padding: 15px 20px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #2e7d32;'>
                    <p style='margin: 0 0 8px 0; font-size: 14px; color: #1b5e20;'>
                        <strong>Número de Cédula (Usuario):</strong> <span style='font-family: monospace; background: #c8e6c9; padding: 2px 6px; border-radius: 4px;'>{cedula}</span>
                    </p>
                    <p style='margin: 0; font-size: 14px; color: #1b5e20;'>
                        <strong>Contraseña temporal:</strong> <span style='font-family: monospace; background: #c8e6c9; padding: 2px 6px; border-radius: 4px;'>{passwordTemporal}</span>
                    </p>
                </div>
                <p style='font-size: 13px; color: #d32f2f; font-weight: bold;'>
                    ⚠️ Te recomendamos iniciar sesión con tu cédula y cambiar tu contraseña por seguridad.
                </p>";

            var message = ConstruirMensajeConLogoFisico(correoDestino, nombreUsuario, "Confirmación de Cuenta Administrador - CampesinApp", contenidoHtml, "#2e7d32", environment);
            return EnviarMensajeSmtp(message, "ADMIN", correoDestino);
        }

        public static bool EnviarCodigoEliminacionCuenta(string correoDestino, string nombreUsuario, string codigoCorreo, IWebHostEnvironment? environment = null)
        {
            string contenidoHtml = $@"
                <h2 style='color: #d32f2f; text-align: center; margin-top: 0;'>Solicitud de Eliminación de Cuenta</h2>
                <p style='font-size: 15px; color: #333;'>Hola <strong>{nombreUsuario}</strong>,</p>
                <p style='font-size: 14px; color: #555; line-height: 1.5;'>
                    Hemos recibido una solicitud para desactivar y eliminar tu cuenta en <strong>CampesinApp</strong>.
                </p>
                <p style='font-size: 14px; color: #555;'>Tu código de verificación para confirmar la eliminación es:</p>
                <div style='text-align: center; margin: 25px 0;'>
                    <span style='background-color: #ffebee; color: #b71c1c; font-size: 32px; font-weight: bold; letter-spacing: 5px; padding: 10px 25px; border-radius: 8px; border: 1px dashed #d32f2f;'>{codigoCorreo}</span>
                </div>
                <p style='color: #d32f2f; font-weight: bold; font-size: 13px; text-align: center;'>⏰ Este código caduca estrictamente en 5 minutos.</p>
                <p style='font-size: 12px; color: #888; text-align: center; margin-top: 15px;'>
                    Si no solicitaste esta acción, te recomendamos cambiar tu contraseña de inmediato.
                </p>";

            var message = ConstruirMensajeConLogoFisico(correoDestino, nombreUsuario, "Código de Verificación - Eliminación de Cuenta CampesinApp", contenidoHtml, "#d32f2f", environment);
            return EnviarMensajeSmtp(message, "ELIMINAR CUENTA", correoDestino);
        }

        /// <summary>
        /// Envía el comprobante / resumen de liquidación de pago al Campesino por su aporte.
        /// Incluye el sello y logo de respaldo de Openpay.
        /// </summary>
        public static async Task<bool> EnviarComprobantePagoCampesinoAsync(ComprobantePagoCampesinoEmailDto dto, IWebHostEnvironment? environment = null)
        {
            bool existeLogoOpenpay = environment != null && File.Exists(Path.Combine(environment.WebRootPath, "images", "OPENPAY_logo.png"));
            string tagImgOpenpay = existeLogoOpenpay
                ? "<img src='cid:logoOpenpayUnico' alt='Openpay' style='max-height: 26px; width: auto; display: inline-block; vertical-align: middle;' />"
                : "<strong style='color: #002e5f; font-size: 13px;'>Openpay Colombia</strong>";

            string contenidoHtml = $@"
                <div style='text-align: center; margin-bottom: 15px;'>
                    <span style='background-color: #e8f5e9; color: #1b5e20; font-size: 11px; font-weight: bold; padding: 4px 12px; border-radius: 20px; text-transform: uppercase;'>
                        ✓ Liquidación Exitosa
                    </span>
                    <h2 style='color: #2e7d32; margin: 10px 0 4px 0; font-size: 22px;'>Comprobante de Pago</h2>
                    <p style='color: #666; font-size: 13px; margin: 0;'>Pedido: <strong>{dto.CodigoPedido}</strong></p>
                </div>

                <p style='font-size: 14px; color: #333;'>Hola <strong>{dto.NombreCampesino}</strong>,</p>
                <p style='font-size: 14px; color: #555; line-height: 1.5;'>
                    Te confirmamos que la entrega del pedido ha finalizado con éxito. Se ha realizado la liquidación del pago correspondiente a tu aporte agrícola:
                </p>

                <!-- TABLA TIPO RESUMEN / FACTURA -->
                <div style='border: 1px solid #e0e0e0; border-radius: 8px; overflow: hidden; margin: 20px 0;'>
                    <div style='background-color: #2e7d32; color: white; padding: 10px 15px; font-size: 13px; font-weight: bold;'>
                        Resumen de la Transacción
                    </div>
                    <table style='width: 100%; border-collapse: collapse; font-size: 13px; color: #333;'>
                        <tr style='border-bottom: 1px solid #eee;'>
                            <td style='padding: 10px 15px; color: #666;'>Producto y Variedad</td>
                            <td style='padding: 10px 15px; text-align: right; font-weight: bold;'>{dto.Producto} {dto.Variedad}</td>
                        </tr>
                        <tr style='border-bottom: 1px solid #eee; background-color: #fafafa;'>
                            <td style='padding: 10px 15px; color: #666;'>Cantidad Aportada</td>
                            <td style='padding: 10px 15px; text-align: right; font-weight: bold;'>{dto.KilosAportados:N0} Kg</td>
                        </tr>
                        <tr style='border-bottom: 1px solid #eee;'>
                            <td style='padding: 10px 15px; color: #666;'>Precio por Kilo</td>
                            <td style='padding: 10px 15px; text-align: right; font-weight: bold;'>${dto.PrecioPorKilo:N2} COP</td>
                        </tr>
                        <tr style='border-bottom: 1px solid #eee; background-color: #fafafa;'>
                            <td style='padding: 10px 15px; color: #666;'>Entidad de Abono</td>
                            <td style='padding: 10px 15px; text-align: right;'>{dto.BancoDestino}</td>
                        </tr>
                        <tr style='border-bottom: 1px solid #eee;'>
                            <td style='padding: 10px 15px; color: #666;'>Cuenta de Destino</td>
                            <td style='padding: 10px 15px; text-align: right; font-family: monospace;'>{dto.CuentaDestino}</td>
                        </tr>
                        <tr style='border-bottom: 1px solid #eee; background-color: #fafafa;'>
                            <td style='padding: 10px 15px; color: #666;'>Fecha de Liquidación</td>
                            <td style='padding: 10px 15px; text-align: right;'>{dto.FechaPago:dd/MM/yyyy hh:mm tt}</td>
                        </tr>
                        <tr style='background-color: #e8f5e9;'>
                            <td style='padding: 12px 15px; font-weight: bold; color: #1b5e20; font-size: 14px;'>TOTAL CONSIGNADO</td>
                            <td style='padding: 12px 15px; text-align: right; font-weight: bold; color: #1b5e20; font-size: 16px;'>${dto.TotalLiquidado:N2} COP</td>
                        </tr>
                    </table>
                </div>

                <!-- SELLO DE PASARELA OPENPAY -->
                <div style='text-align: center; margin: 20px 0 10px 0; padding: 12px; background-color: #f8faf9; border-radius: 8px; border: 1px solid #e0e0e0;'>
                    <span style='font-size: 10px; color: #666; font-weight: bold; text-transform: uppercase; letter-spacing: 0.5px; display: block; margin-bottom: 6px;'>
                        Transacción procesada y respaldada de forma segura por
                    </span>
                    {tagImgOpenpay}
                </div>";

            var message = ConstruirMensajeConLogoFisico(
                dto.CorreoDestino,
                dto.NombreCampesino,
                $"Comprobante de Pago - Pedido {dto.CodigoPedido} - CampesinApp",
                contenidoHtml,
                "#2e7d32",
                environment,
                incluirOpenpay: true);

            return await EnviarMensajeSmtpAsync(message, "COMPROBANTE CAMPESINO", dto.CorreoDestino);
        }

        /// <summary>
        /// Envía el comprobante / resumen de liquidación de flete por correo al Transportador.
        /// Incluye el desglose de flete y el sello de respaldo de Openpay.
        /// </summary>
        public static async Task<bool> EnviarComprobantePagoTransportadorAsync(ComprobantePagoTransportadorEmailDto dto, IWebHostEnvironment? environment = null)
        {
            bool existeLogoOpenpay = environment != null && File.Exists(Path.Combine(environment.WebRootPath, "images", "OPENPAY_logo.png"));
            string tagImgOpenpay = existeLogoOpenpay
                ? "<img src='cid:logoOpenpayUnico' alt='Openpay' style='max-height: 26px; width: auto; display: inline-block; vertical-align: middle;' />"
                : "<strong style='color: #002e5f; font-size: 13px;'>Openpay Colombia</strong>";

            string contenidoHtml = $@"
                <div style='text-align: center; margin-bottom: 15px;'>
                    <span style='background-color: #e8f5e9; color: #1b5e20; font-size: 11px; font-weight: bold; padding: 4px 12px; border-radius: 20px; text-transform: uppercase;'>
                        ✓ Flete Liquidado con Éxito
                    </span>
                    <h2 style='color: #2e7d32; margin: 10px 0 4px 0; font-size: 22px;'>Comprobante de Flete</h2>
                    <p style='color: #666; font-size: 13px; margin: 0;'>Pedido: <strong>{dto.CodigoPedido}</strong></p>
                </div>

                <p style='font-size: 14px; color: #333;'>Hola <strong>{dto.NombreTransportador}</strong>,</p>
                <p style='font-size: 14px; color: #555; line-height: 1.5;'>
                    Te confirmamos que la entrega del pedido ha finalizado con éxito. Se ha realizado la liquidación del flete de transporte correspondiente a tu viaje:
                </p>

                <!-- TABLA TIPO RESUMEN / FACTURA -->
                <div style='border: 1px solid #e0e0e0; border-radius: 8px; overflow: hidden; margin: 20px 0;'>
                    <div style='background-color: #2e7d32; color: white; padding: 10px 15px; font-size: 13px; font-weight: bold;'>
                        Resumen del Flete
                    </div>
                    <table style='width: 100%; border-collapse: collapse; font-size: 13px; color: #333;'>
                        <tr style='border-bottom: 1px solid #eee;'>
                            <td style='padding: 10px 15px; color: #666;'>Carga Transportada</td>
                            <td style='padding: 10px 15px; text-align: right; font-weight: bold;'>{dto.Producto} {dto.Variedad} ({dto.TotalKilos:N0} Kg)</td>
                        </tr>
                        <tr style='border-bottom: 1px solid #eee; background-color: #fafafa;'>
                            <td style='padding: 10px 15px; color: #666;'>Punto de Destino</td>
                            <td style='padding: 10px 15px; text-align: right; font-weight: bold;'>{dto.DestinoEntrega}</td>
                        </tr>
                        <tr style='border-bottom: 1px solid #eee;'>
                            <td style='padding: 10px 15px; color: #666;'>Anticipo de Operación (30%)</td>
                            <td style='padding: 10px 15px; text-align: right;'>${dto.Anticipo:N2} COP</td>
                        </tr>
                        <tr style='border-bottom: 1px solid #eee; background-color: #fafafa;'>
                            <td style='padding: 10px 15px; color: #666;'>Entidad de Abono</td>
                            <td style='padding: 10px 15px; text-align: right;'>{dto.BancoDestino}</td>
                        </tr>
                        <tr style='border-bottom: 1px solid #eee;'>
                            <td style='padding: 10px 15px; color: #666;'>Cuenta de Destino</td>
                            <td style='padding: 10px 15px; text-align: right; font-family: monospace;'>{dto.CuentaDestino}</td>
                        </tr>
                        <tr style='border-bottom: 1px solid #eee; background-color: #fafafa;'>
                            <td style='padding: 10px 15px; color: #666;'>Fecha de Liquidación</td>
                            <td style='padding: 10px 15px; text-align: right;'>{dto.FechaPago:dd/MM/yyyy hh:mm tt}</td>
                        </tr>
                        <tr style='background-color: #e8f5e9;'>
                            <td style='padding: 12px 15px; font-weight: bold; color: #1b5e20; font-size: 14px;'>TOTAL FLETE LIQUIDADO</td>
                            <td style='padding: 12px 15px; text-align: right; font-weight: bold; color: #1b5e20; font-size: 16px;'>${dto.FleteTotal:N2} COP</td>
                        </tr>
                    </table>
                </div>

                <!-- SELLO DE PASARELA OPENPAY -->
                <div style='text-align: center; margin: 20px 0 10px 0; padding: 12px; background-color: #f8faf9; border-radius: 8px; border: 1px solid #e0e0e0;'>
                    <span style='font-size: 10px; color: #666; font-weight: bold; text-transform: uppercase; letter-spacing: 0.5px; display: block; margin-bottom: 6px;'>
                        Transacción procesada y respaldada de forma segura por
                    </span>
                    {tagImgOpenpay}
                </div>";

            var message = ConstruirMensajeConLogoFisico(
                dto.CorreoDestino,
                dto.NombreTransportador,
                $"Comprobante de Pago de Flete - Pedido {dto.CodigoPedido} - CampesinApp",
                contenidoHtml,
                "#2e7d32",
                environment,
                incluirOpenpay: true);

            return await EnviarMensajeSmtpAsync(message, "COMPROBANTE TRANSPORTADOR", dto.CorreoDestino);
        }
    }
}