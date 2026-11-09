namespace CampesinApp.Dtos
{
    /// <summary>
    /// Datos de la barra de paginación (vista parcial Shared/_Paginacion). Se usa en las listas y tablas de los casos de uso
    /// que pueden tener muchas filas: pedidos, historiales, precios y notificaciones.
    /// </summary>
    public class PaginacionDto
    {
        public int Pagina { get; set; } = 1;

        public int TamanoPagina { get; set; } = 10;

        public int TotalRegistros { get; set; }

        /// <summary>Nombre de lo que se lista, en plural ("pedidos", "transacciones"…); aparece en "(14 pedidos en total)".</summary>
        public string Etiqueta { get; set; } = "registros";

        /// <summary>Dirección de cada página; "{p}" se reemplaza por el número de página.</summary>
        public string Plantilla { get; set; } = string.Empty;

        /// <summary>
        /// Dónde se pinta la respuesta al pulsar un número: "principal" (ventana principal), "pagina" (panel de una pantalla
        /// completa) o "enlace" (navegación normal, para pantallas que no usan ventanas).
        /// </summary>
        public string Destino { get; set; } = "principal";

        public int TotalPaginas => Math.Max(1, (int)Math.Ceiling(TotalRegistros / (double)Math.Max(1, TamanoPagina)));

        public string Url(int pagina) => Plantilla.Replace("{p}", pagina.ToString());

        /// <summary>Números que se muestran: primera, última y las cercanas a la actual; null es "…".</summary>
        public IEnumerable<int?> Ventana()
        {
            var total = TotalPaginas;
            var mostrar = new SortedSet<int> { 1, total };
            for (var p = Pagina - 1; p <= Pagina + 1; p++)
            {
                if (p >= 1 && p <= total) mostrar.Add(p);
            }

            var anterior = 0;
            foreach (var p in mostrar)
            {
                if (p - anterior > 1) yield return null;
                yield return p;
                anterior = p;
            }
        }

        public static PaginacionDto Crear(int? pagina, int total, int tamano, string etiqueta, string plantilla, string destino)
        {
            var paginas = Math.Max(1, (int)Math.Ceiling(total / (double)Math.Max(1, tamano)));
            return new PaginacionDto
            {
                Pagina = Math.Clamp(pagina ?? 1, 1, paginas),
                TamanoPagina = tamano,
                TotalRegistros = total,
                Etiqueta = etiqueta,
                Plantilla = plantilla,
                Destino = destino
            };
        }
    }
}
