using CampesinApp.Dtos;
using CampesinApp.Repositories.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CampesinApp.Services
{
    /// <summary>
    /// Genera el PDF del Historial Global del Aplicativo (CU-030): logo, nombre de CampesinApp, resumen, historial de
    /// ventas general y logs del sistema, con los filtros aplicados. El contenido siempre sale de la base de datos en el
    /// momento de la descarga, así que refleja los cambios más recientes del aplicativo.
    /// </summary>
    public class HistorialPdfService
    {
        private const string VerdeOscuro = "#1b5e20";
        private const string VerdeClaro = "#e8f5e9";
        private const string Gris = "#616161";

        private readonly HistorialService _historial;
        private readonly IWebHostEnvironment _entorno;

        static HistorialPdfService()
        {
            // QuestPDF Community: gratuito para organizaciones con menos de 1 millón de dólares de ingresos anuales.
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public HistorialPdfService(HistorialService historial, IWebHostEnvironment entorno)
        {
            _historial = historial;
            _entorno = entorno;
        }

        /// <summary>Consulta ventas y logs (sin el límite de filas de la ventana emergente) y arma el documento.</summary>
        public async Task<byte[]> GenerarAsync(FiltroHistorialGlobalDto filtro, string generadoPor)
        {
            const int limite = 5000;

            var ventas = (await _historial.ObtenerGlobalAsync(new FiltroHistorialGlobalDto
            {
                Pestana = "ventas",
                IdUsuario = filtro.IdUsuario,
                Rol = filtro.Rol,
                Desde = filtro.Desde,
                Hasta = filtro.Hasta
            }, limite)).Ventas;

            var logs = (await _historial.ObtenerGlobalAsync(new FiltroHistorialGlobalDto
            {
                Pestana = "logs",
                IdUsuario = filtro.IdUsuario,
                Desde = filtro.Desde,
                Hasta = filtro.Hasta
            }, limite)).Logs;

            return Armar(filtro, ventas, logs, generadoPor, DateTime.Now);
        }

        private byte[] Armar(FiltroHistorialGlobalDto filtro, List<TransaccionDto> ventas, List<LogVistaDto> logs, string generadoPor, DateTime ahora)
        {
            var rutaLogo = Path.Combine(_entorno.WebRootPath ?? string.Empty, "images", "logo.jpeg");
            var logo = File.Exists(rutaLogo) ? File.ReadAllBytes(rutaLogo) : null;

            return Document.Create(documento =>
            {
                documento.Page(pagina =>
                {
                    pagina.Size(PageSizes.A4.Landscape());
                    pagina.Margin(28);
                    pagina.DefaultTextStyle(x => x.FontSize(9).FontColor(Colors.Black));

                    pagina.Header().Element(cabecera => Cabecera(cabecera, logo, filtro, generadoPor, ahora));

                    pagina.Content().PaddingVertical(8).Column(columna =>
                    {
                        columna.Spacing(14);
                        columna.Item().Element(c => Resumen(c, ventas, logs));
                        columna.Item().Element(c => TablaVentas(c, ventas));
                        columna.Item().Element(c => TablaLogs(c, logs));
                    });

                    pagina.Footer().Row(fila =>
                    {
                        fila.RelativeItem().Text("CampesinApp · Campesino valorado, alimento bien pagado").FontSize(8).FontColor(Gris);
                        fila.RelativeItem().AlignRight().Text(texto =>
                        {
                            texto.DefaultTextStyle(x => x.FontSize(8).FontColor(Gris));
                            texto.Span("Página ");
                            texto.CurrentPageNumber();
                            texto.Span(" de ");
                            texto.TotalPages();
                        });
                    });
                });
            }).GeneratePdf();
        }

        private static void Cabecera(IContainer contenedor, byte[]? logo, FiltroHistorialGlobalDto filtro, string generadoPor, DateTime ahora)
        {
            contenedor.PaddingBottom(8).BorderBottom(1.5f).BorderColor(VerdeOscuro).Row(fila =>
            {
                if (logo != null)
                {
                    fila.ConstantItem(58).Height(58).Image(logo).FitArea();
                    fila.ConstantItem(12);
                }

                fila.RelativeItem().Column(c =>
                {
                    c.Item().Text("CampesinApp").FontSize(22).Bold().FontColor(VerdeOscuro);
                    c.Item().Text("Historial Global del Aplicativo").FontSize(13).SemiBold();
                    c.Item().Text($"Filtros: {DescribirFiltros(filtro)}").FontSize(8).FontColor(Gris);
                });

                fila.ConstantItem(190).AlignRight().Column(c =>
                {
                    c.Item().AlignRight().Text($"Generado: {Formato.FechaHora(ahora)}").FontSize(8).FontColor(Gris);
                    c.Item().AlignRight().Text($"Por: {generadoPor}").FontSize(8).FontColor(Gris);
                });
            });
        }

        private static string DescribirFiltros(FiltroHistorialGlobalDto filtro)
        {
            var partes = new List<string>();
            if (filtro.IdUsuario != null) partes.Add($"usuario {filtro.IdUsuario}");
            if (!string.IsNullOrWhiteSpace(filtro.Rol)) partes.Add($"rol {filtro.Rol} (solo ventas)");
            if (filtro.Desde != null) partes.Add($"desde {Formato.Fecha(filtro.Desde.Value)}");
            if (filtro.Hasta != null) partes.Add($"hasta {Formato.Fecha(filtro.Hasta.Value)}");
            return partes.Count == 0 ? "ninguno (toda la información)" : string.Join(" · ", partes);
        }

        private static void Resumen(IContainer contenedor, List<TransaccionDto> ventas, List<LogVistaDto> logs)
        {
            var valorTotal = ventas.Sum(v => v.Valor);
            var correctas = ventas.Count(v => v.Estado == EstadosEnvio.EntregadoRecibido);
            var conObservaciones = ventas.Count(v => v.Estado == EstadosEnvio.EntregadoConObservaciones);
            var errores = logs.Count(l => l.Nivel == NivelesLog.Error);
            var advertencias = logs.Count(l => l.Nivel == NivelesLog.Advertencia);

            contenedor.Column(c =>
            {
                c.Item().PaddingBottom(4).Text("Resumen").FontSize(12).Bold().FontColor(VerdeOscuro);
                c.Item().Row(fila =>
                {
                    fila.Spacing(8);
                    Tarjeta(fila.RelativeItem(), "Transacciones", Formato.Entero(ventas.Count));
                    Tarjeta(fila.RelativeItem(), "Valor total", Formato.Moneda(valorTotal));
                    Tarjeta(fila.RelativeItem(), "Entregadas correctamente", Formato.Entero(correctas));
                    Tarjeta(fila.RelativeItem(), "Con observaciones", Formato.Entero(conObservaciones));
                    Tarjeta(fila.RelativeItem(), "Eventos del sistema", Formato.Entero(logs.Count));
                    Tarjeta(fila.RelativeItem(), "Errores / advertencias", $"{errores} / {advertencias}");
                });
            });
        }

        private static void Tarjeta(IContainer contenedor, string titulo, string valor)
        {
            contenedor.Background(VerdeClaro).Border(0.5f).BorderColor("#a5d6a7").Padding(6).Column(c =>
            {
                c.Item().Text(titulo).FontSize(7.5f).FontColor(Gris);
                c.Item().Text(valor).FontSize(13).Bold().FontColor(VerdeOscuro);
            });
        }

        private static void TablaVentas(IContainer contenedor, List<TransaccionDto> ventas)
        {
            contenedor.Column(c =>
            {
                c.Item().PaddingBottom(4).Text("Historial de ventas general").FontSize(12).Bold().FontColor(VerdeOscuro);

                if (ventas.Count == 0)
                {
                    c.Item().Text("No hay transacciones para los filtros seleccionados.").Italic().FontColor(Gris);
                    return;
                }

                c.Item().Table(tabla =>
                {
                    tabla.ColumnsDefinition(columnas =>
                    {
                        columnas.ConstantColumn(52);   // código
                        columnas.RelativeColumn(1.4f); // producto
                        columnas.ConstantColumn(34);   // cantidad
                        columnas.ConstantColumn(62);   // valor
                        columnas.ConstantColumn(58);   // fecha
                        columnas.RelativeColumn(1.3f); // comprador
                        columnas.RelativeColumn(1.5f); // vendedores
                        columnas.RelativeColumn(1.3f); // transportador
                        columnas.RelativeColumn(1.4f); // estado
                    });

                    tabla.Header(h =>
                    {
                        foreach (var titulo in new[] { "Código", "Producto", "Cant.", "Valor", "Fecha", "Comprador", "Vendedor(es)", "Transportador", "Estado" })
                        {
                            h.Cell().Background(VerdeOscuro).Padding(4).Text(titulo).FontColor(Colors.White).Bold().FontSize(8);
                        }
                    });

                    var i = 0;
                    foreach (var v in ventas)
                    {
                        var fondo = i++ % 2 == 0 ? "#ffffff" : "#f5f5f5";
                        void Celda(string texto, bool derecha = false)
                        {
                            var celda = tabla.Cell().Background(fondo).BorderBottom(0.25f).BorderColor("#e0e0e0").Padding(3);
                            (derecha ? celda.AlignRight() : celda).Text(texto).FontSize(8);
                        }

                        Celda(v.Codigo);
                        Celda(v.Producto);
                        Celda(Formato.Entero(v.Cantidad), true);
                        Celda(Formato.Moneda(v.Valor), true);
                        Celda(Formato.Fecha(v.Fecha));
                        Celda(v.Comprador);
                        Celda(v.Vendedores);
                        Celda(v.Transportador);
                        Celda(v.Estado);
                    }
                });
            });
        }

        private static void TablaLogs(IContainer contenedor, List<LogVistaDto> logs)
        {
            contenedor.Column(c =>
            {
                c.Item().PaddingBottom(4).Text("Logs del sistema").FontSize(12).Bold().FontColor(VerdeOscuro);

                if (logs.Count == 0)
                {
                    c.Item().Text("No hay eventos para los filtros seleccionados.").Italic().FontColor(Gris);
                    return;
                }

                c.Item().Table(tabla =>
                {
                    tabla.ColumnsDefinition(columnas =>
                    {
                        columnas.ConstantColumn(112);  // fecha
                        columnas.ConstantColumn(44);   // nivel
                        columnas.RelativeColumn(1.2f); // usuario
                        columnas.RelativeColumn(1.5f); // origen
                        columnas.RelativeColumn(4f);   // mensaje
                    });

                    tabla.Header(h =>
                    {
                        foreach (var titulo in new[] { "Fecha", "Nivel", "Usuario", "Origen", "Mensaje" })
                        {
                            h.Cell().Background(VerdeOscuro).Padding(4).Text(titulo).FontColor(Colors.White).Bold().FontSize(8);
                        }
                    });

                    var i = 0;
                    foreach (var l in logs)
                    {
                        var fondo = i++ % 2 == 0 ? "#ffffff" : "#f5f5f5";
                        var colorNivel = l.Nivel == NivelesLog.Error ? "#c62828" : l.Nivel == NivelesLog.Advertencia ? "#ef6c00" : Gris;

                        tabla.Cell().Background(fondo).BorderBottom(0.25f).BorderColor("#e0e0e0").Padding(3).Text(Formato.FechaHora(l.Fecha)).FontSize(8);
                        tabla.Cell().Background(fondo).BorderBottom(0.25f).BorderColor("#e0e0e0").Padding(3).Text(l.Nivel).FontSize(8).Bold().FontColor(colorNivel);
                        tabla.Cell().Background(fondo).BorderBottom(0.25f).BorderColor("#e0e0e0").Padding(3).Text(l.Usuario).FontSize(8);
                        tabla.Cell().Background(fondo).BorderBottom(0.25f).BorderColor("#e0e0e0").Padding(3).Text(l.Origen).FontSize(8);
                        tabla.Cell().Background(fondo).BorderBottom(0.25f).BorderColor("#e0e0e0").Padding(3).Text(l.Mensaje).FontSize(8);
                    }
                });
            });
        }
    }
}
