using CampesinApp.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
// Casos de uso 21 a 30: contexto de datos (simulado o SQL Server según Datos:Modo en appsettings.json), servicios,
// filtros y proceso automático del modelo estadístico.
builder.Services.AddCasosDeUso21a30(builder.Configuration);

var app = builder.Build();

// En modo simulado carga los datos de demostración en memoria (en modo "BaseDatos" no hace nada).
await app.Services.InicializarDatosSimuladosAsync();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
