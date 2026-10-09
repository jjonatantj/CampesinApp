using System;
using System.Diagnostics;
using MailKit.Security;
using MimeKit;

namespace CampesinApp.Services
{
    public static class EmailService
    {
        private static readonly string SmtpServer = "smtp.gmail.com";
        private static readonly int SmtpPort = 587;

        // COLOCA TU CORREO Y LA CLAVE DE APLICACIÓN DE 16 CARACTERES
        private static readonly string EmailEmisor = "soporte.campesinapp@gmail.com";
        private static readonly string PasswordEmisor = "qeen efki bnes incx"; // Pega tus 16 letras de Google sin espacios

        public static bool EnviarCodigoVerificacion(string correoDestino, string nombreUsuario, string codigoCorreo)
        {
            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("CampesinApp Colombia", EmailEmisor));
                message.To.Add(new MailboxAddress(nombreUsuario, correoDestino));
                message.Subject = "Código de Verificación - Registro CampesinApp";

                var bodyBuilder = new BodyBuilder
                {
                    HtmlBody = $@"
                        <div style='font-family: Arial, sans-serif; padding: 20px; background-color: #f4f6f8;'>
                            <div style='max-width: 500px; margin: auto; background: white; padding: 30px; border-radius: 10px; border-top: 5px solid #2e7d32;'>
                                <h2 style='color: #2e7d32; text-align: center;'>¡Bienvenido a CampesinApp!</h2>
                                <p>Hola <strong>{nombreUsuario}</strong>,</p>
                                <p>Tu código de verificación para completar el registro es:</p>
                                <div style='text-align: center; margin: 25px 0;'>
                                    <span style='background-color: #e8f5e9; color: #1b5e20; font-size: 32px; font-weight: bold; letter-spacing: 5px; padding: 10px 25px; border-radius: 8px; border: 1px dashed #2e7d32;'>{codigoCorreo}</span>
                                </div>
                                <p style='color: #d32f2f; font-weight: bold; font-size: 13px;'>⏰ Este código caduca estrictamente en 5 minutos.</p>
                            </div>
                        </div>"
                };

                message.Body = bodyBuilder.ToMessageBody();

                // SE ESPECIFICA MailKit.Net.Smtp.SmtpClient PARA ELIMINAR LA AMBIGÜEDAD
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
    }
}