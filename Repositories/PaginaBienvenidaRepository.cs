using CampesinApp.Repositories.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CampesinApp.Repositories
{
    public class PaginaBienvenidaRepository
    {
        private readonly Campesinapp3Context _context;

        public PaginaBienvenidaRepository(Campesinapp3Context context)
        {
            _context = context;
        }

        public class ItemCatalogoPreliminar
        {
            public int IdDemanda { get; set; }
            public string NombreProducto { get; set; } = string.Empty;
            public string Categoria { get; set; } = string.Empty;
            public string PrecioSugerido { get; set; } = string.Empty;
            public string CantidadDemanda { get; set; } = string.Empty;
            public string Comprador { get; set; } = string.Empty;
            public string Ubicacion { get; set; } = string.Empty;
            public string TipoCamion { get; set; } = string.Empty;
            public string ImagenUrl { get; set; } = string.Empty;
        }

        public List<ItemCatalogoPreliminar> ObtenerCatalogoPreliminar(int top = 6)
        {
            var resultado = new List<ItemCatalogoPreliminar>();

            try
            {
                // Consulta basada exactamente en tus DbSets: Demanda, TipoProductos, Variedads, Usuarios, Municipios y Departamentos
                var listaDemandas = (from d in _context.Demanda
                                     join tp in _context.TipoProductos on d.IdTipoProducto equals tp.IdTipoProducto into tpGroup
                                     from tp in tpGroup.DefaultIfEmpty()
                                     join v in _context.Variedads on d.IdVariedad equals v.IdVariedad into vGroup
                                     from v in vGroup.DefaultIfEmpty()
                                     join u in _context.Usuarios on d.IdUsuario equals u.IdUsuario into uGroup
                                     from u in uGroup.DefaultIfEmpty()
                                     join p in _context.PuntoAcopios on d.IdPuntoAcopio equals p.IdPuntoAcopio into pGroup
                                     from p in pGroup.DefaultIfEmpty()
                                     join m in _context.Municipios on (p != null ? p.IdMunicipio : 1) equals m.IdMunicipio into mGroup
                                     from m in mGroup.DefaultIfEmpty()
                                     join dep in _context.Departamentos on (m != null ? m.IdDepartamento : 1) equals dep.IdDepartamento into depGroup
                                     from dep in depGroup.DefaultIfEmpty()
                                     select new
                                     {
                                         d.IdDemanda,
                                         d.CantidadDeman,
                                         d.PrecioDeman,
                                         d.IdTipoProducto,
                                         d.IdVariedad,
                                         Producto = tp != null ? tp.Producto : "Producto Agrícola",
                                         Variedad = v != null ? v.VariedadProd : "",
                                         NombreComprador = u != null ? (u.NombreCom + " " + u.ApellidoCom) : "Comprador Mayorista",
                                         Municipio = m != null ? m.Municipio1 : "Cundinamarca",
                                         Departamento = dep != null ? dep.Departamento1 : "Colombia"
                                     })
                                     .AsNoTracking()
                                     .Take(top)
                                     .ToList();

                foreach (var item in listaDemandas)
                {
                    decimal precioEst = 0;
                    try
                    {
                        precioEst = _context.PrecioEstimados
                            .Where(pe => pe.IdTipoProducto == item.IdTipoProducto && pe.IdVariedad == item.IdVariedad)
                            .OrderByDescending(pe => pe.FechaPreci)
                            .Select(pe => pe.PrecioEsti)
                            .FirstOrDefault();
                    }
                    catch
                    {
                        precioEst = 0;
                    }

                    string precioTexto = precioEst > 0
                        ? $"$ {precioEst:N0} / Kg"
                        : $"$ {item.PrecioDeman:N0} / Kg";

                    string nombreProdCompleto = string.IsNullOrEmpty(item.Variedad)
                        ? item.Producto
                        : $"{item.Producto} ({item.Variedad})";

                    resultado.Add(new ItemCatalogoPreliminar
                    {
                        IdDemanda = item.IdDemanda,
                        NombreProducto = nombreProdCompleto,
                        Categoria = item.Producto,
                        PrecioSugerido = precioTexto,
                        CantidadDemanda = $"{item.CantidadDeman:N0} Kg requeridos",
                        Comprador = item.NombreComprador,
                        Ubicacion = $"{item.Municipio} / {item.Departamento}",
                        TipoCamion = "Camión Carga (Estacas / Refrigerado)",
                        ImagenUrl = "/images/logo.jpeg"
                    });
                }
            }
            catch (Exception)
            {
                return new List<ItemCatalogoPreliminar>();
            }

            return resultado;
        }
    }
}