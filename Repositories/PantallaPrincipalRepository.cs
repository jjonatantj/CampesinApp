using CampesinApp.Repositories.Models;
using CampesinApp.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CampesinApp.Repositories
{
    public class PantallaPrincipalRepository
    {
        private readonly Campesinapp3Context _context;

        public PantallaPrincipalRepository(Campesinapp3Context context)
        {
            _context = context;
        }

        // Obtener lista de tipos de producto para el desplegable
        public List<SelectListItem> ObtenerListaTiposProducto(string categoriaSeleccionada = "")
        {
            return _context.TipoProductos
                .OrderBy(tp => tp.Producto)
                .Select(tp => new SelectListItem
                {
                    Value = tp.IdTipoProducto.ToString(),
                    Text = tp.Producto,
                    Selected = (tp.IdTipoProducto.ToString() == categoriaSeleccionada)
                })
                .ToList();
        }

        // Obtener variedades según el tipo seleccionado
        public List<SelectListItem> ObtenerListaVariedadesPorTipo(string categoria, string variedadSeleccionada = "")
        {
            if (!string.IsNullOrEmpty(categoria) && int.TryParse(categoria, out int idTipoNum))
            {
                return _context.Variedads
                    .Where(v => v.IdTipoProducto == idTipoNum)
                    .OrderBy(v => v.VariedadProd)
                    .Select(v => new SelectListItem
                    {
                        Value = v.IdVariedad.ToString(),
                        Text = v.VariedadProd,
                        Selected = (v.IdVariedad.ToString() == variedadSeleccionada)
                    })
                    .ToList();
            }
            return new List<SelectListItem>();
        }

        // Precios paginados estrictamente de 5 en 5 con filtros
        public (List<PrecioSugeridoItem> ListaPrecios, int TotalRegistros) ObtenerPreciosPaginados(string buscar = "", string categoria = "", string variedad = "", int pagina = 1, int pageSize = 5)
        {
            var query = _context.PrecioEstimados
                .Include(p => p.IdTipoProductoNavigation)
                .Include(p => p.IdVariedadNavigation)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(buscar))
            {
                query = query.Where(p => p.IdTipoProductoNavigation.Producto.Contains(buscar) || p.IdVariedadNavigation.VariedadProd.Contains(buscar));
            }

            if (!string.IsNullOrWhiteSpace(categoria))
            {
                if (int.TryParse(categoria, out int idCatNum))
                {
                    query = query.Where(p => p.IdTipoProducto == idCatNum);
                }
                else if (categoria != "Todas")
                {
                    query = query.Where(p => p.IdTipoProductoNavigation.Producto == categoria);
                }
            }

            if (!string.IsNullOrWhiteSpace(variedad) && int.TryParse(variedad, out int idVarNum))
            {
                query = query.Where(p => p.IdVariedad == idVarNum);
            }

            var ultimosPreciosIds = query
                .GroupBy(p => new { p.IdTipoProducto, p.IdVariedad })
                .Select(g => g.Max(p => p.IdPrecioEstimado))
                .ToList();

            var preciosQuery = _context.PrecioEstimados
                .Include(p => p.IdTipoProductoNavigation)
                .Include(p => p.IdVariedadNavigation)
                .Where(p => ultimosPreciosIds.Contains(p.IdPrecioEstimado))
                .Select(p => new PrecioSugeridoItem
                {
                    Producto = p.IdTipoProductoNavigation.Producto,
                    Variedad = p.IdVariedadNavigation.VariedadProd,
                    Categoria = p.IdTipoProductoNavigation.Producto,
                    UnidadMedida = "Kg",
                    PrecioVenta = (decimal)Math.Round(p.PrecioEstiCampesino, 0),
                    PrecioCompra = (decimal)Math.Round(p.PrecioEstiComprador, 0),
                });

            int totalRegistros = preciosQuery.Count();

            var listaPaginada = preciosQuery
                .Skip((pagina - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return (listaPaginada, totalRegistros);
        }

        public List<string> ObtenerCategorias()
        {
            return _context.TipoProductos
                .OrderBy(t => t.Producto)
                .Select(t => t.Producto)
                .Distinct()
                .ToList();
        }

        public List<CamionSugerenciaItem> ObtenerCamionesDisponibles(int top = 5)
        {
            return _context.Camions
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
                .Take(top)
                .Select(p => new ProductoOfertaItem
                {
                    Id = p.IdProducto,
                    IdUsuario = p.IdUsuario,
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