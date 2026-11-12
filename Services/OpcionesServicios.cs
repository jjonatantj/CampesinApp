namespace CampesinApp.Services
{
    /// <summary>Tiempo de inactividad (en minutos) tras el cual se cierra la sesión (FA02-CU032). Igual al IdleTimeout de la sesión en Program.cs.</summary>
    public static class SesionOpciones
    {
        public const int MinutosInactividad = 30;
    }

    /// <summary>Teléfonos oficiales de soporte que se muestran en el módulo de calificación (CU-026).</summary>
    public class SoporteOptions
    {
        public const string Seccion = "Soporte";

        /// <summary>Se configuran en appsettings (Soporte:Telefonos). Sin valor por defecto: el binder sumaría la configuración a él.</summary>
        public string[] Telefonos { get; set; } = Array.Empty<string>();

        /// <summary>Correo oficial de soporte (Soporte:Correo).</summary>
        public string Correo { get; set; } = string.Empty;
    }
}
