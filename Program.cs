using CampesinApp.Repositories;
using CampesinApp.Repositories.Models;
using CampesinApp.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

// Configuracion local opcional (no se sube a git): permite que cada integrante use su propia cadena de conexion
// sin tocar appsettings.json. Ver appsettings.Local.json en .gitignore.
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

// 1. Agregar servicios MVC al contenedor
builder.Services.AddControllersWithViews();

// 2. REGISTRAR EL DbContext con la cadena de conexión definida en appsettings.json
builder.Services.AddDbContext<Campesinapp3Context>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Test"))
           // La cadena de conexion usa MARS, que deshabilita los savepoints de EF. Las transacciones manuales de los
           // casos de uso 21-32 hacen rollback al liberarse sin Commit, por lo que el aviso no aplica.
           .ConfigureWarnings(w => w.Ignore(SqlServerEventId.SavepointsDisabledBecauseOfMARS)));

// 3. REGISTRAR LOS REPOSITORIOS (inyección de dependencias)
//    Agrega aquí cada nuevo repositorio que crees en la carpeta Repositories/
builder.Services.AddScoped<UsuariosRepository>();
builder.Services.AddScoped<PantallaPrincipalRepository>();
builder.Services.AddScoped<PaginaBienvenidaRepository>();
builder.Services.AddScoped<DemandaRepository>();

// 3.1 CASOS DE USO 21 a 32 (ubicacion, transporte, entrega, calificacion, historiales, precios, perfil y cierre de sesion).
//     Todo lo que necesitan se registra en Services/CasosUsoServiceCollectionExtensions.cs
builder.Services.AddCasosDeUso(builder.Configuration);

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

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// 5. ACTIVAR EL MIDDLEWARE DE SESIÓN (Obligatorio antes de UseAuthorization)
app.UseSession();

app.UseAuthorization();

// 6. RUTA PREDETERMINADA: Mantiene 'PaginaBienvenida' como la primera pantalla al abrir el sitio
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=PaginaBienvenida}/{action=Bienvenida}/{id?}");

app.Run();

