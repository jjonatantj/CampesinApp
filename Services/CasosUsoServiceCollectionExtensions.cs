namespace CampesinApp.Services
{
    /// <summary>
    /// Registro de todo lo que necesitan los casos de uso 21 a 32 (excepto el modelo estadístico, CU-029).
    /// Así Program.cs solo suma una línea y no se mezcla con lo que registren los demás casos de uso.
    /// El DbContext y la sesión HTTP se registran en Program.cs (los comparte todo el equipo).
    /// </summary>
    public static class CasosUsoServiceCollectionExtensions
    {
        public static IServiceCollection AddCasosDeUso(this IServiceCollection services, IConfiguration configuration)
        {
            // Las ventanas emergentes envían el token CSRF en la cabecera RequestVerificationToken (ver modales.js).
            services.AddAntiforgery(options => options.HeaderName = "RequestVerificationToken");
            services.AddHttpContextAccessor();

            services.Configure<SoporteOptions>(configuration.GetSection(SoporteOptions.Seccion));

            // El usuario actual sale de la sesión HTTP que crea el inicio de sesión (CU-004).
            services.AddScoped<UsuarioSesionService>();
            services.AddScoped<IUsuarioActualService>(sp => sp.GetRequiredService<UsuarioSesionService>());

            services.AddScoped<ErrorAjaxFilter>();
            services.AddScoped<ILogService, LogService>();
            services.AddScoped<NotificacionService>();
            services.AddScoped<PedidoService>();
            services.AddScoped<UbicacionService>();
            services.AddScoped<CalificacionService>();
            services.AddScoped<TransporteService>();
            services.AddScoped<EntregaService>();
            services.AddScoped<HistorialService>();
            services.AddScoped<PreciosService>();
            services.AddScoped<PerfilService>();

            return services;
        }
    }
}
