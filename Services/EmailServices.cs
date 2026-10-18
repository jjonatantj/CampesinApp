using System;
using System.Diagnostics;
using System.IO;
using MailKit.Security;
using MimeKit;
using Microsoft.AspNetCore.Hosting;

namespace CampesinApp.Services
{
    public static class EmailService
    {
        private static readonly string SmtpServer = "smtp.gmail.com";
        private static readonly int SmtpPort = 587;

        private static readonly string EmailEmisor = "soporte.campesinapp@gmail.com";
        private static readonly string PasswordEmisor = "qeen efki bnes incx";

        // Datos de contacto y redes configurables
        private static readonly string NumeroWhatsApp = "+57 300 000 0000";
        private static readonly string HorarioAtencion = "Lunes a Sábado de 8:00 a.m. a 6:00 p.m.";
        private static readonly string EnlaceFacebook = "https://facebook.com/campesinapp";
        private static readonly string EnlaceInstagram = "https://instagram.com/campesinapp";

        /// <summary>
        /// Método auxiliar para construir el correo incrustando físicamente la imagen desde wwwroot/images/
        /// </summary>
        private static MimeMessage ConstruirMensajeConLogoFisico(string correoDestino, string nombreUsuario, string asunto, string contenidoHtmlCore, string colorBorde, IWebHostEnvironment? environment)
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
                // Buscamos las extensiones comunes en la carpeta wwwroot/images/
                string rutaLogoJpg = Path.Combine(environment.WebRootPath, "images", "logo.jpeg");
                string rutaLogoPng = Path.Combine(environment.WebRootPath, "images", "logo.png");
                string rutaLogoMayus = Path.Combine(environment.WebRootPath, "images", "logo.JPG");

                string rutaFinal = string.Empty;

                if (File.Exists(rutaLogoJpg)) rutaFinal = rutaLogoJpg;
                else if (File.Exists(rutaLogoPng)) rutaFinal = rutaLogoPng;
                else if (File.Exists(rutaLogoMayus)) rutaFinal = rutaLogoMayus;

                if (!string.IsNullOrEmpty(rutaFinal))
                {
                    // Añadimos la imagen como un recurso vinculado (LinkedResource) para que MimeKit la incruste
                    var recursoImagen = builder.LinkedResources.Add(rutaFinal);
                    recursoImagen.ContentId = cidLogo;
                    imagenAgregadaExitosamente = true;
                }
            }

            // HTML Estructurado apuntando obligatoriamente a cid:logoCampesinAppUnico
            string tagImgLogo = imagenAgregadaExitosamente
                ? $"<img src='cid:{cidLogo}' alt='CampesinApp Logo' width='80' height='80' style='display: block; margin: 0 auto; border-radius: 50%; border: 2px solid {colorBorde}; object-fit: cover;' />"
                : $"<h2 style='color: {colorBorde}; margin: 0;'>CampesinApp</h2>";

            builder.HtmlBody = $@"
                <div style='font-family: Arial, sans-serif; padding: 20px; background-color: #f4f6f8;'>
                    <div style='max-width: 520px; margin: auto; background: white; padding: 30px; border-radius: 10px; border-top: 5px solid {colorBorde}; box-shadow: 0 4px 6px rgba(0,0,0,0.05);'>
                        
                        <!-- LOGOTIPO OBLIGATORIO -->
                        <div style='text-align: center; margin-bottom: 20px;'>
                            {tagImgLogo}
                        </div>

                        {contenidoHtmlCore}
                        
                        <!-- SECCIÓN DE CONTACTO Y SOPORTE -->
                        <div style='background-color: #f9f9f9; padding: 15px; border-radius: 8px; margin: 25px 0; font-size: 12px; color: #555; text-align: center;'>
                            <p style='margin: 0 0 8px 0; color: {colorBorde}; font-weight: bold;'>⚠️ Buzón automático (no respondas a este correo).</p>
                            <p style='margin: 0 0 5px 0;'>¿Dudas o inconvenientes? Escríbenos al WhatsApp: <strong>{NumeroWhatsApp}</strong></p>
                            <p style='margin: 0;'><strong>Horario de atención:</strong> {HorarioAtencion}</p>
                        </div>

                        <!-- REDES SOCIALES -->
                        <div style='text-align: center; margin-bottom: 20px;'>
                            <a href='{EnlaceFacebook}' target='_blank' style='color: {colorBorde}; text-decoration: none; margin: 0 10px; font-size: 12px; font-weight: bold;'>Facebook</a> | 
                            <a href='{EnlaceInstagram}' target='_blank' style='color: {colorBorde}; text-decoration: none; margin: 0 10px; font-size: 12px; font-weight: bold;'>Instagram</a>
                        </div>

                        <!-- TÉRMINOS Y CONDICIONES EN TEXTO Y PIE DE PÁGINA -->
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
            try
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

                using (var client = new MailKit.Net.Smtp.SmtpClient())
                {
                    client.Connect(SmtpServer, SmtpPort, SecureSocketOptions.StartTls);
                    client.Authenticate(EmailEmisor, PasswordEmisor);
                    client.Send(message);
                    client.Disconnect(true);
                }

                Debug.WriteLine($"---> [ÉXITO MAILKIT] Correo enviado a {correoDestino}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"---> [ERROR MAILKIT]: {ex.Message}");
                return false;
            }
        }

        public static bool EnviarNotificacionAdmin(string correoDestino, string nombreUsuario, IWebHostEnvironment? environment = null)
        {
            try
            {
                string contenidoHtml = $@"
                    <h2 style='color: #2e7d32; text-align: center; margin-bottom: 20px; margin-top: 0;'>¡Cuenta Administrador Creada!</h2>
                    <p style='font-size: 15px; color: #333;'>Hola <strong>{nombreUsuario}</strong>,</p>
                    <p style='font-size: 14px; color: #555; line-height: 1.5;'>
                        Se ha creado tu cuenta de <strong>Administrador</strong> en <strong>CampesinApp</strong> con éxito.
                    </p>
                    <div style='background-color: #e8f5e9; padding: 15px 20px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #2e7d32;'>
                        <p style='margin: 0; font-size: 14px; color: #1b5e20;'>
                            Puedes ingresar a la plataforma utilizando tu correo electrónico (<strong>{correoDestino}</strong>) y tu número de cédula.
                        </p>
                    </div>";

                var message = ConstruirMensajeConLogoFisico(correoDestino, nombreUsuario, "Confirmación de Cuenta Administrador - CampesinApp", contenidoHtml, "#2e7d32", environment);

                using (var client = new MailKit.Net.Smtp.SmtpClient())
                {
                    client.Connect(SmtpServer, SmtpPort, SecureSocketOptions.StartTls);
                    client.Authenticate(EmailEmisor, PasswordEmisor);
                    client.Send(message);
                    client.Disconnect(true);
                }

                Debug.WriteLine($"---> [ÉXITO MAILKIT ADMIN] Correo enviado a {correoDestino}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"---> [ERROR MAILKIT ADMIN]: {ex.Message}");
                return false;
            }
        }

        public static bool EnviarCodigoEliminacionCuenta(string correoDestino, string nombreUsuario, string codigoCorreo, IWebHostEnvironment? environment = null)
        {
            try
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

                using (var client = new MailKit.Net.Smtp.SmtpClient())
                {
                    client.Connect(SmtpServer, SmtpPort, SecureSocketOptions.StartTls);
                    client.Authenticate(EmailEmisor, PasswordEmisor);
                    client.Send(message);
                    client.Disconnect(true);
                }

                Debug.WriteLine($"---> [ÉXITO MAILKIT ELIMINAR CUENTA] Correo enviado a {correoDestino}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"---> [ERROR MAILKIT ELIMINAR CUENTA]: {ex.Message}");
                return false;
            }
        }
    }
}