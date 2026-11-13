using CampesinApp.Repositories;
using CampesinApp.Repositories.Models;
using CampesinApp.Services;
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

// 4. SERVICIOS DE ESTIMADOS DIARIOS (Nativo de .NET 8, sin scripts .sql ni dependencias pesadas)
builder.Services.AddScoped<IEstimadosDiariosJob, EstimadosDiariosJob>();
builder.Services.AddHostedService<EstimadosDiariosBackgroundService>();

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

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=PaginaBienvenida}/{action=Bienvenida}/{id?}");

app.Run();