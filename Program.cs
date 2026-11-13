using CampesinApp.Repositories;
using CampesinApp.Repositories.Models;
using CampesinApp.Services;
using Hangfire;
using Hangfire.SqlServer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

// Inicializar la configuración segura para el servicio de correos
EmailService.Initialize(builder.Configuration);

// Configuracion local opcional (no se sube a git)
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

// 1. Agregar servicios MVC al contenedor
builder.Services.AddControllersWithViews();

// 2. REGISTRAR EL DbContext
var connectionString = builder.Configuration.GetConnectionString("Test");
builder.Services.AddDbContext<Campesinapp3Context>(options =>
    options.UseSqlServer(connectionString)
           .ConfigureWarnings(w => w.Ignore(SqlServerEventId.SavepointsDisabledBecauseOfMARS)));

// 3. REGISTRAR LOS REPOSITORIOS
builder.Services.AddHttpClient();
builder.Services.AddScoped<CampesinApp.Services.ILogisticaFleteService, CampesinApp.Services.LogisticaFleteService>();
builder.Services.AddScoped<UsuariosRepository>();
builder.Services.AddScoped<PantallaPrincipalRepository>();
builder.Services.AddScoped<PaginaBienvenidaRepository>();
builder.Services.AddScoped<DemandaRepository>();
builder.Services.AddScoped<IPuntoAcopioRepository, PuntoAcopioRepository>();
builder.Services.AddScoped<IAsignacionVehiculoRepository, AsignacionVehiculoRepository>();

builder.Services.AddCasosDeUso(builder.Configuration);

var hangfireStorage = new SqlServerStorage(connectionString, new SqlServerStorageOptions
{
    CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
    SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
    QueuePollInterval = TimeSpan.Zero,
    UseRecommendedIsolationLevel = true,
    DisableGlobalLocks = true,
    PrepareSchemaIfNecessary = true
});

JobStorage.Current = hangfireStorage;

builder.Services.AddHangfire(configuration => configuration
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseStorage(hangfireStorage));

builder.Services.AddHangfireServer();
builder.Services.AddScoped<IEstimadosDiariosJob, EstimadosDiariosJob>();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
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

// Manejador de excepciones generales de base de datos (503)
app.UseExceptionHandler("/Error/Excepcion");

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// 5. ACTIVAR EL MIDDLEWARE DE SESIÓN
app.UseSession();
app.UseAuthorization();


app.UseStatusCodePagesWithReExecute("/Error404/Index", "?statusCode={0}");

// 5.1 HANGFIRE DASHBOARD Y PROGRAMACIÓN DE ESTIMADOS DIARIOS
app.UseHangfireDashboard("/hangfire");

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
    RecurringJob.AddOrUpdate<IEstimadosDiariosJob>(
        "sp-calcular-estimados-diarios",
        job => job.EjecutarSpAsync(),
        "30 8 * * *",
        new RecurringJobOptions { TimeZone = zonaColombia });

    BackgroundJob.Enqueue<IEstimadosDiariosJob>(job => job.VerificarYEjecutarPendienteAsync());
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "Aviso: No se pudo registrar la tarea programada de Hangfire en este arranque.");
}

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=PaginaBienvenida}/{action=Bienvenida}/{id?}");

app.Run();