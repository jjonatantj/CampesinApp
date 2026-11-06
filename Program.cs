using CampesinApp.Repositories;
using CampesinApp.Repositories.Models;
using CampesinApp.Services;
using Hangfire;
using Hangfire.SqlServer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

// Configuracion local opcional (no se sube a git): permite que cada integrante use su propia cadena de conexion
// sin tocar appsettings.json. Ver appsettings.Local.json en .gitignore.
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

// 1. Agregar servicios MVC al contenedor
builder.Services.AddControllersWithViews();

// 2. REGISTRAR EL DbContext con la cadena de conexión definida en appsettings.json
var connectionString = builder.Configuration.GetConnectionString("Test");
builder.Services.AddDbContext<Campesinapp3Context>(options =>
    options.UseSqlServer(connectionString)
           // La cadena de conexion usa MARS, que deshabilita los savepoints de EF. Las transacciones manuales de los
           // casos de uso 21-32 hacen rollback al liberarse sin Commit, por lo que el aviso no aplica.
           .ConfigureWarnings(w => w.Ignore(SqlServerEventId.SavepointsDisabledBecauseOfMARS)));

// 3. REGISTRAR LOS REPOSITORIOS (inyección de dependencias)
//    Agrega aquí cada nuevo repositorio que crees en la carpeta Repositories/
builder.Services.AddScoped<UsuariosRepository>();
builder.Services.AddScoped<PantallaPrincipalRepository>();
builder.Services.AddScoped<PaginaBienvenidaRepository>();
builder.Services.AddScoped<DemandaRepository>();
builder.Services.AddScoped<IPuntoAcopioRepository, PuntoAcopioRepository>();
builder.Services.AddScoped<IAsignacionVehiculoRepository, AsignacionVehiculoRepository>();

// 3.1 CASOS DE USO 21 a 32 (ubicacion, transporte, entrega, calificacion, historiales, precios, perfil y cierre de sesion).
//     Todo lo que necesitan se registra en Services/CasosUsoServiceCollectionExtensions.cs
builder.Services.AddCasosDeUso(builder.Configuration);

// 3.2 HANGFIRE (Servicio en segundo plano para sp_CalcularEstimadosDiarios a las 8:30 AM)
// Inicializar explícitamente el almacenamiento para forzar la creación inmediata del esquema [HangFire].*
var hangfireStorage = new SqlServerStorage(connectionString, new SqlServerStorageOptions
{
    CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
    SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
    QueuePollInterval = TimeSpan.Zero,
    UseRecommendedIsolationLevel = true,
    DisableGlobalLocks = true,
    PrepareSchemaIfNecessary = true // Ejecuta SqlServerObjectsInstaller.Install al inicializar
});

JobStorage.Current = hangfireStorage;

builder.Services.AddHangfire(configuration => configuration
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseStorage(hangfireStorage));

builder.Services.AddHangfireServer();
builder.Services.AddScoped<IEstimadosDiariosJob, EstimadosDiariosJob>();

// 4. REGISTRAR SERVICIO DE SESIÓN (Permite conservar el Rol y la Cédula tras iniciar sesión)
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Tiempo de expiración tras inactividad
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// Configurar el pipeline de solicitudes HTTP

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Pantalla "Servicio no disponible" (503) de CampesinApp cuando falla la base de datos (CU-021 a CU-032 y demás pantallas).
// Cualquier otro error pasa de largo y lo atiende el manejo de errores de arriba, como antes. Ver ErrorController.
app.UseExceptionHandler("/Error/Excepcion");

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// 5. ACTIVAR EL MIDDLEWARE DE SESIÓN (Obligatorio antes de UseAuthorization)
app.UseSession();

app.UseAuthorization();

// 5.1 HANGFIRE DASHBOARD Y PROGRAMACIÓN DE ESTIMADOS DIARIOS (8:30 AM hora Colombia)
app.UseHangfireDashboard("/hangfire");

// Resolver zona horaria de Colombia (UTC-5)
TimeZoneInfo zonaColombia;
try
{
    zonaColombia = TimeZoneInfo.FindSystemTimeZoneById("SA Pacific Standard Time");
}
catch
{
    try
    {
        zonaColombia = TimeZoneInfo.FindSystemTimeZoneById("America/Bogota");
    }
    catch
    {
        zonaColombia = TimeZoneInfo.Local;
    }
}

try
{
    // Programar ejecución fija diaria a las 8:30 AM
    RecurringJob.AddOrUpdate<IEstimadosDiariosJob>(
        "sp-calcular-estimados-diarios",
        job => job.EjecutarSpAsync(),
        "30 8 * * *",
        new RecurringJobOptions { TimeZone = zonaColombia });

    // Si el equipo estuvo apagado a las 8:30 AM y el aplicativo arranca después, se encola en background para revisar y ejecutar si falta hoy
    BackgroundJob.Enqueue<IEstimadosDiariosJob>(job => job.VerificarYEjecutarPendienteAsync());
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "Aviso: No se pudo registrar la tarea programada de Hangfire en este arranque.");
}

// 6. RUTA PREDETERMINADA: Mantiene 'PaginaBienvenida' como la primera pantalla al abrir el sitio
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=PaginaBienvenida}/{action=Bienvenida}/{id?}");

app.Run();
