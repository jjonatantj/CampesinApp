using CampesinApp.Repositories.Models;
using CampesinApp.ViewModels;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CampesinApp.Repositories
{
    public class PantallaPrincipalRepository
    {
        private readonly Campesinapp3Context _context;

        public PantallaPrincipalRepository(Campesinapp3Context context)
        {
            _context = context;
        }

        // ─────────────────────────────────────────────────────────────────
        // PRECIOS SUGERIDOS DEL DÍA (precio_Estimado + tipo_Producto + variedad)
        // ─────────────────────────────────────────────────────────────────
        public List<PrecioSugeridoItem> ObtenerPreciosDelDia(string buscar = "", string categoria = "")
        {
            var query = _context.PrecioEstimados
                .Include(p => p.IdTipoProductoNavigation)
                .Include(p => p.IdVariedadNavigation)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(buscar))
            {
                query = query.Where(p => p.IdTipoProductoNavigation.Producto.Contains(buscar));
            }

            if (!string.IsNullOrWhiteSpace(categoria) && categoria != "Todas")
            {
                query = query.Where(p => p.IdTipoProductoNavigation.Producto == categoria);
            }

            // Agrupación optimizada a nivel de Servidor SQL
            var ultimosPreciosIds = query
                .GroupBy(p => new { p.IdTipoProducto, p.IdVariedad })
                .Select(g => g.Max(p => p.IdPrecioEstimado))
                .ToList();

            var precios = _context.PrecioEstimados
                .Include(p => p.IdTipoProductoNavigation)
                .Include(p => p.IdVariedadNavigation)
                .Where(p => ultimosPreciosIds.Contains(p.IdPrecioEstimado))
                .Select(p => new PrecioSugeridoItem
                {
                    Producto = p.IdTipoProductoNavigation.Producto + " (" + p.IdVariedadNavigation.VariedadProd + ")",
                    Categoria = p.IdTipoProductoNavigation.Producto,
                    UnidadMedida = "Kg",
                    PrecioVenta = (decimal)p.PrecioEsti,
                    PrecioCompra = (decimal)(p.PrecioEsti * 1.07), // Margen estimado del 7%
                })
                .ToList();

            return precios;
        }

        public List<string> ObtenerCategorias()
        {
            return _context.TipoProductos
                .OrderBy(t => t.Producto)
                .Select(t => t.Producto)
                .Distinct()
                .ToList();
        }

        // ─────────────────────────────────────────────────────────────────
        // BLOQUES DE SUGERENCIAS
        // ─────────────────────────────────────────────────────────────────

        public List<CamionSugerenciaItem> ObtenerCamionesDisponibles(int top = 5)
        {
            return _context.Camions
                .Include(c => c.IdUsuarioNavigation)
                    .ThenInclude(u => u.IdMunicipioNavigation)
                .Where(c => c.EstadoPase && c.EstadoSeguro && c.EstadoTecno)
                .Take(top)
                .Select(c => new CamionSugerenciaItem
                {
                    Id = c.IdCamion,
                    Capacidad = c.CapacidadCami + " Toneladas",
                    NombreComprador = c.IdUsuarioNavigation.NombreCom + " " + c.IdUsuarioNavigation.ApellidoCom,
                    RutaProyectada = c.IdUsuarioNavigation.IdMunicipioNavigation.Municipio1
                })
                .ToList();
        }

        public List<CampesinoDestacadoItem> ObtenerCampesinosDestacados(int top = 4)
        {
            return _context.Usuarios
                .Include(u => u.IdRolNavigation)
                .Include(u => u.IdMunicipioNavigation)
                    .ThenInclude(m => m.IdDepartamentoNavigation)
                .Where(u => u.Estado && u.IdRolNavigation.TipoRol == "Campesino")
                .Take(top)
                .Select(u => new CampesinoDestacadoItem
                {
                    Id = u.IdUsuario,
                    NombreCompleto = u.NombreCom + " " + u.ApellidoCom,
                    Ubicacion = u.IdMunicipioNavigation.Municipio1 + " / " + u.IdMunicipioNavigation.IdDepartamentoNavigation.Departamento1,
                    FotoUrl = string.IsNullOrEmpty(u.Imagen) ? "" : u.Imagen
                })
                .ToList();
        }

        public List<CompradorMayoristaItem> ObtenerCompradoresMayoristas(int top = 4)
        {
            return _context.Usuarios
                .Include(u => u.IdRolNavigation)
                .Where(u => u.Estado && u.IdRolNavigation.TipoRol == "Comprador")
                .Take(top)
                .Select(u => new CompradorMayoristaItem
                {
                    Id = u.IdUsuario,
                    NombreComercial = u.NombreCom + " " + u.ApellidoCom,
                    LogoUrl = string.IsNullOrEmpty(u.Imagen) ? "" : u.Imagen
                })
                .ToList();
        }

        public List<ProductoOfertaItem> ObtenerCatalogoProductos(int top = 6)
        {
            return _context.Productos
                .Include(p => p.IdTipoProductoNavigation)
                .Take(top)
                .Select(p => new ProductoOfertaItem
                {
                    Id = p.IdProducto,
                    NombreProducto = p.IdTipoProductoNavigation.Producto,
                    UbicacionCultivo = p.UbicacionCultivo,
                    ImagenUrl = string.IsNullOrEmpty(p.ImagenProduc) ? "" : p.ImagenProduc
                })
                .ToList();
        }

        public bool ExistenSugerencias()
        {
            return _context.Camions.Any(c => c.EstadoPase && c.EstadoSeguro && c.EstadoTecno)
                || _context.Usuarios.Any(u => u.Estado && u.IdRolNavigation.TipoRol == "Campesino")
                || _context.Productos.Any();
        }
    }
}