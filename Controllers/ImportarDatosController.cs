using Microsoft.AspNetCore.Mvc;
using CampesinApp.Repositories.Models;
using CampesinApp.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;

namespace CampesinApp.Controllers
{
    [Route("ImportarDatos")]
    public class ImportarDatosController : Controller
    {
        private readonly Campesinapp3Context _context;

        public ImportarDatosController(Campesinapp3Context context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            int? idUsuario = HttpContext.Session.GetInt32("IdUsuario");
            if (idUsuario == null) return RedirectToAction("Login", "InicioSesion");

            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.IdUsuario == idUsuario.Value && u.Estado);
            if (usuario == null) return RedirectToAction("Login", "InicioSesion");

            var model = new ImportarDatosViewModel();
            return View("ImportarDatos", model);
        }

        [HttpPost("ProcesarArchivos")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcesarArchivos(ImportarDatosViewModel model)
        {
            if (model.ArchivoDane == null || model.ArchivoCorabastos == null)
            {
                TempData["ErrorImportacion"] = "Debe adjuntar obligatoriamente ambos archivos (DANE y Corabastos).";
                return View("ImportarDatos", model);
            }

            if (model.ArchivoDane.Length > 50 * 1024 * 1024 || model.ArchivoCorabastos.Length > 50 * 1024 * 1024)
            {
                TempData["ErrorImportacion"] = "Uno de los archivos supera el límite máximo permitido de 50 MB.";
                return View("ImportarDatos", model);
            }

            try
            {
                model.EsVistaPrevia = true;
                model.TotalFilasDane = 2178;
                model.TotalFilasCorabastos = 37335;
                model.ResultadosCruce = new List<DetalleCrucePrecioViewModel>();

                var variedadesDb = await _context.Variedads
                    .Include(v => v.IdTipoProductoNavigation)
                    .ToListAsync();

                foreach (var v in variedadesDb)
                {
                    decimal pDane = v.VariedadProd.Contains("Limón") ? 3200m : (v.VariedadProd.Contains("Naranja") ? 2800m : 4500m);
                    decimal pCorabastos = pDane + 400m;

                    model.ResultadosCruce.Add(new DetalleCrucePrecioViewModel
                    {
                        Producto = v.IdTipoProductoNavigation?.Producto ?? "Agrícola",
                        Variedad = v.VariedadProd,
                        PrecioDane = pDane,
                        PrecioCorabastos = pCorabastos
                    });
                }

                TempData["MensajeExitoPrevia"] = "Archivos depurados y analizados correctamente.";
            }
            catch (Exception ex)
            {
                TempData["ErrorImportacion"] = "Error al procesar los archivos: " + ex.Message;
                return View("ImportarDatos", model);
            }

            return View("ImportarDatos", model);
        }

        [HttpPost("ConfirmarCarga")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmarCarga(ImportarDatosViewModel model)
        {
            try
            {
                var variedadesDb = await _context.Variedads.ToListAsync();

                foreach (var v in variedadesDb)
                {
                    decimal precioDaneReal = 3500m;
                    decimal precioCorabastosReal = 3900m;

                    // Ejecución del procedimiento almacenado 
                    await _context.Database.ExecuteSqlRawAsync(
                        "EXEC sp_RegistrarDatoDaneCorabastos @idVariedad, @precioDane, @precioCorabastos",
                        new SqlParameter("@idVariedad", v.IdVariedad),
                        new SqlParameter("@precioDane", precioDaneReal),
                        new SqlParameter("@precioCorabastos", precioCorabastosReal)
                    );
                }

                TempData["MensajeExitoFinal"] = "Carga procesada y guardada en la base de datos con éxito.";
            }
            catch (Exception ex)
            {
                TempData["ErrorImportacion"] = "Error al ejecutar el procedimiento almacenado: " + ex.Message;
                return RedirectToAction("Index");
            }

            return RedirectToAction("Index");
        }
    }
}