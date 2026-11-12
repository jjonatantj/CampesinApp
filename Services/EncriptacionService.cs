using Microsoft.AspNetCore.Identity;

namespace CampesinApp.Services
{
    public static class EncriptacionService
    {
        private static readonly PasswordHasher<string> _passwordHasher = new PasswordHasher<string>();

        // Genera un hash seguro con sal aleatoria (PBKDF2)
        public static string Encriptar(string textoPlano)
        {
            if (string.IsNullOrEmpty(textoPlano)) return string.Empty;

            // El primer parámetro es un objeto dummy (string vacío), ya que usamos el hasher solo para texto de contraseña
            return _passwordHasher.HashPassword(string.Empty, textoPlano);
        }

        // Comprueba de forma segura si la contraseña ingresada coincide con el hash almacenado
        public static bool Verificar(string hashedPassword, string providedPassword)
        {
            if (string.IsNullOrEmpty(hashedPassword) || string.IsNullOrEmpty(providedPassword))
                return false;

            var result = _passwordHasher.VerifyHashedPassword(string.Empty, hashedPassword, providedPassword);

            // Retorna true si coincide (puede requerir rehash, pero para efectos prácticos evaluamos Success o SuccessRehashNeeded)
            return result == PasswordVerificationResult.Success || result == PasswordVerificationResult.SuccessRehashNeeded;
        }
    }
}