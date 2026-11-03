using CampesinApp.Dtos;
using CampesinApp.Repositories.Models;
using Microsoft.EntityFrameworkCore;

namespace CampesinApp.Services
{
    /// <summary>
    /// Consulta de precios sugeridos (CU-028). Lee de la tabla precio_Estimado (la misma que usa la pantalla principal CU-009)
    /// el último precio de cada producto y variedad. El modelo estadístico (CU-029) es quien alimenta esa tabla.
    /// </summary>
    public class PreciosService
    {
        private readonly Campesinapp3Context _db;

        public PreciosService(Campesinapp3Context db)
        {
            _db = db;
        }

        /// <summary>
        /// Segmentación por rol (RN-01-CU029): el comprador ve el valor de compra, el campesino el valor de venta
        /// y el administrador ambos. No se exponen fórmulas ni valores intermedios (RN-02-CU028).
        /// </summary>
        public async Task<ListaPreciosDto> ListarAsync(UsuarioActual usuario)
        {
            var ultimos = await _db.PrecioEstimados
                .AsNoTracking()
                .Where(p => p.IdPrecioEstimado == _db.PrecioEstimados
                    .Where(q => q.IdTipoProducto == p.IdTipoProducto && q.IdVariedad == p.IdVariedad)
                    .OrderByDescending(q => q.FechaPreci)
                    .ThenByDescending(q => q.IdPrecioEstimado)
                    .Select(q => q.IdPrecioEstimado)
                    .First())
                .OrderBy(p => p.IdTipoProductoNavigation.Producto)
                .ThenBy(p => p.IdVariedadNavigation.VariedadProd)
                .Select(p => new
                {
                    p.IdTipoProductoNavigation.Producto,
                    p.IdVariedadNavigation.VariedadProd,
                    p.PrecioEstiCampesino,
                    p.PrecioEstiComprador,
                    p.FechaPreci
                })
                .ToListAsync();

            var verComprador = usuario.Rol is RolUsuario.Comprador or RolUsuario.Administrador;
            var verCampesino = usuario.Rol is RolUsuario.Campesino or RolUsuario.Administrador;

            return new ListaPreciosDto
            {
                Usuario = usuario,
                Precios = ultimos.Select(p => new PrecioVistaDto
                {
                    Producto = p.Producto,
                    Variedad = p.VariedadProd,
                    PrecioComprador = verComprador ? (decimal)Math.Round(p.PrecioEstiComprador, 0) : null,
                    PrecioCampesino = verCampesino ? (decimal)Math.Round(p.PrecioEstiCampesino, 0) : null,
                    FechaCalculo = p.FechaPreci.ToDateTime(TimeOnly.MinValue)
                }).ToList()
            };
        }
    }
}
