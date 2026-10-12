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

        private static readonly string EmailEmisor = "soporte.campesinapp@gmail.com";
        private static readonly string PasswordEmisor = "qeen efki bnes incx";

        /// <summary>
        /// Envía el código de verificación OTP de 6 dígitos para el registro estándar de usuarios.
        /// </summary>
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

        /// <summary>
        /// Envía el correo de confirmación de cuenta creada para nuevos Administradores (CU-003).
        /// </summary>
        public static bool EnviarNotificacionAdmin(string correoDestino, string nombreUsuario)
        {
            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("CampesinApp Colombia", EmailEmisor));
                message.To.Add(new MailboxAddress(nombreUsuario, correoDestino));
                message.Subject = "Confirmación de Cuenta Administrador - CampesinApp";

                var bodyBuilder = new BodyBuilder
                {
                    HtmlBody = $@"
                        <div style='font-family: Arial, sans-serif; padding: 20px; background-color: #f4f6f8;'>
                            <div style='max-width: 520px; margin: auto; background: white; padding: 30px; border-radius: 10px; border-top: 5px solid #2e7d32;'>
                                <h2 style='color: #2e7d32; text-align: center; margin-bottom: 20px;'>¡Cuenta Administrador Creada!</h2>
                                <p style='font-size: 15px; color: #333;'>Hola <strong>{nombreUsuario}</strong>,</p>
                                <p style='font-size: 14px; color: #555; line-height: 1.5;'>
                                    Se ha creado tu cuenta de <strong>Administrador</strong> en <strong>CampesinApp</strong> con éxito.
                                </p>
                                <div style='background-color: #e8f5e9; padding: 15px 20px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #2e7d32;'>
                                    <p style='margin: 0; font-size: 14px; color: #1b5e20;'>
                                        Puedes ingresar a la plataforma utilizando tu correo electrónico (<strong>{correoDestino}</strong>) y tu número de cédula.
                                    </p>
                                </div>
                                <p style='font-size: 13px; color: #777; text-align: center; margin-top: 25px;'>
                                    CampesinApp - Plataforma de Comercialización Agrícola Directa
                                </p>
                            </div>
                        </div>"
                };

                message.Body = bodyBuilder.ToMessageBody();

                using (var client = new MailKit.Net.Smtp.SmtpClient())
                {
                    client.Connect(SmtpServer, SmtpPort, SecureSocketOptions.StartTls);
                    client.Authenticate(EmailEmisor, PasswordEmisor);

                    client.Send(message);
                    client.Disconnect(true);
                }

                Debug.WriteLine($"---> [ÉXITO MAILKIT ADMIN] Correo de bienvenida enviado a {correoDestino}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"---> [ERROR MAILKIT ADMIN]: {ex.Message}");
                return false;
            }
        }
        public static bool EnviarCodigoEliminacionCuenta(string correoDestino, string nombreUsuario, string codigoCorreo)
        {
            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("CampesinApp Colombia", EmailEmisor));
                message.To.Add(new MailboxAddress(nombreUsuario, correoDestino));
                message.Subject = "Código de Verificación - Eliminación de Cuenta CampesinApp";

                var bodyBuilder = new BodyBuilder
                {
                    HtmlBody = $@"
                        <div style='font-family: Arial, sans-serif; padding: 20px; background-color: #f4f6f8;'>
                            <div style='max-width: 500px; margin: auto; background: white; padding: 30px; border-radius: 10px; border-top: 5px solid #d32f2f;'>
                                <h2 style='color: #d32f2f; text-align: center;'>Solicitud de Eliminación de Cuenta</h2>
                                <p style='font-size: 15px; color: #333;'>Hola <strong>{nombreUsuario}</strong>,</p>
                                <p style='font-size: 14px; color: #555; line-height: 1.5;'>
                                    Hemos recibido una solicitud para desactivar y eliminar tu cuenta en <strong>CampesinApp</strong>.
                                </p>
                                <p style='font-size: 14px; color: #555;'>Tu código de verificación para confirmar la eliminación es:</p>
                                <div style='text-align: center; margin: 25px 0;'>
                                    <span style='background-color: #ffebee; color: #b71c1c; font-size: 32px; font-weight: bold; letter-spacing: 5px; padding: 10px 25px; border-radius: 8px; border: 1px dashed #d32f2f;'>{codigoCorreo}</span>
                                </div>
                                <p style='color: #d32f2f; font-weight: bold; font-size: 13px; text-align: center;'>⏰ Este código caduca estrictamente en 5 minutos.</p>
                                <p style='font-size: 12px; color: #888; text-align: center; margin-top: 25px;'>
                                    Si no solicitaste esta acción, te recomendamos cambiar tu contraseña de inmediato.
                                </p>
                            </div>
                        </div>"
                };

                message.Body = bodyBuilder.ToMessageBody();

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