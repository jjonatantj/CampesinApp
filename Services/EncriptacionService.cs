using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace CampesinApp.Services
{
    public static class EncriptacionService
    {
        // Clave de 32 bytes exactos (256 bits) para AES-256
        private static readonly byte[] KeyBytes = Encoding.UTF8.GetBytes("CampesinAppKey2026SecretSystem!!");

        // IV de 16 bytes exactos (128 bits) -> Garantiza que no falle la propiedad aes.IV
        private static readonly byte[] IvBytes = new byte[16] {
            0x43, 0x61, 0x6d, 0x70, 0x65, 0x73, 0x69, 0x6e,
            0x41, 0x70, 0x70, 0x49, 0x56, 0x32, 0x30, 0x32
        }; // Representa la cadena "CampesinAppIV202" en 16 bytes fijos

        public static string Encriptar(string textoPlano)
        {
            if (string.IsNullOrEmpty(textoPlano)) return string.Empty;

            using (Aes aes = Aes.Create())
            {
                aes.Key = KeyBytes;
                aes.IV = IvBytes; // Asignación segura de 16 bytes

                ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter sw = new StreamWriter(cs))
                        {
                            sw.Write(textoPlano);
                        }
                    }
                    return Convert.ToBase64String(ms.ToArray());
                }
            }
        }

        public static string Desencriptar(string textoEncriptado)
        {
            if (string.IsNullOrEmpty(textoEncriptado)) return string.Empty;

            try
            {
                using (Aes aes = Aes.Create())
                {
                    aes.Key = KeyBytes;
                    aes.IV = IvBytes;

                    ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

                    using (MemoryStream ms = new MemoryStream(Convert.FromBase64String(textoEncriptado)))
                    {
                        using (CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                        {
                            using (StreamReader sr = new StreamReader(cs))
                            {
                                return sr.ReadToEnd();
                            }
                        }
                    }
                }
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}