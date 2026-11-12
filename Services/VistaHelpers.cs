using CampesinApp.Repositories.Models;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace CampesinApp.Services
{
    public static class VistaHelpers
    {
        private static readonly System.Text.Json.JsonSerializerOptions OpcionesJson = new()
        {
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
        };

        /// <summary>Puntos del mapa como JSON para el atributo data-puntos de mapas.js.</summary>
        public static string PuntosJson(IEnumerable<CampesinApp.Dtos.PuntoMapaDto> puntos) =>
            System.Text.Json.JsonSerializer.Serialize(puntos, OpcionesJson);

        /// <summary>Puntos de acopio como JSON para el mapa donde se elige el lugar del reporte.</summary>
        public static string PuntosAcopioJson(IEnumerable<CampesinApp.Dtos.PuntoAcopioOpcionDto> puntos) =>
            System.Text.Json.JsonSerializer.Serialize(puntos.Select(p => new { id = p.Id, titulo = p.Nombre, direccion = p.Direccion, lat = p.Lat, lng = p.Lng }), OpcionesJson);

        /// <summary>Viajes como JSON para que el formulario sepa si ya tienen origen y destino.</summary>
        public static string ViajesJson(IEnumerable<CampesinApp.Dtos.ViajeOpcionDto> viajes) =>
            System.Text.Json.JsonSerializer.Serialize(viajes.Select(v => new
            {
                id = v.IdViaje,
                origen = v.Origen,
                destino = v.Destino,
                tieneReportes = v.TieneReportes,
                fechaInicial = Formato.Fecha(v.FechaInicio),
                fechaFinal = Formato.Fecha(v.FechaFinal),
                retrasado = v.Retrasado,
                diasRetraso = v.DiasRetraso,
                hoy = Formato.Fecha(DateTime.Today)
            }), OpcionesJson);

        /// <summary>Municipios como JSON para el mapa del reporte (se geolocalizan en el navegador si no traen coordenadas).</summary>
        public static string MunicipiosJson(IEnumerable<CampesinApp.Dtos.MunicipioOpcionDto> municipios) =>
            System.Text.Json.JsonSerializer.Serialize(municipios.Select(m => new { titulo = m.Nombre, departamento = m.Departamento, region = m.Region, lat = m.Lat, lng = m.Lng }), OpcionesJson);

        /// <summary>"4.628000, -74.064000" (punto decimal) o "No registradas" si el reporte no tiene coordenadas.</summary>
        public static string Coordenadas(double? latitud, double? longitud)
        {
            if (latitud == null || longitud == null) return "No registradas";
            var cultura = System.Globalization.CultureInfo.InvariantCulture;
            return $"{latitud.Value.ToString("0.000000", cultura)}, {longitud.Value.ToString("0.000000", cultura)}";
        }

        /// <summary>Devuelve "campo-error" (borde negro resaltado) si el campo tiene errores en el ModelState.</summary>
        public static string ClaseError(ViewDataDictionary viewData, string campo)
        {
            return viewData.ModelState.TryGetValue(campo, out var entrada) && entrada.Errors.Count > 0 ? "campo-error" : string.Empty;
        }

        public static string MensajeError(ViewDataDictionary viewData, string campo)
        {
            return viewData.ModelState.TryGetValue(campo, out var entrada)
                ? entrada.Errors.Select(e => e.ErrorMessage).FirstOrDefault(m => !string.IsNullOrWhiteSpace(m) && m != "Campo obligatorio") ?? string.Empty
                : string.Empty;
        }

        public static string Estrellas(int cantidad)
        {
            cantidad = Math.Clamp(cantidad, 0, 5);
            return new string('★', cantidad) + new string('☆', 5 - cantidad);
        }

        /// <summary>Icono, color y título de cada tipo de notificación (campana y avisos flotantes).</summary>
        public static (string Icono, string Clase, string Titulo) DatosNotificacion(string tipo) => tipo switch
        {
            TiposNotificacion.Ubicacion => ("bi-geo-alt-fill", "nt-verde", "Ubicación actualizada"),
            TiposNotificacion.Llegada => ("bi-truck", "nt-azul", "Su pedido llegó"),
            TiposNotificacion.EntregaCompletada => ("bi-check-circle-fill", "nt-verde", "Entrega completada"),
            TiposNotificacion.Novedad => ("bi-exclamation-triangle-fill", "nt-amarillo", "Novedad en la entrega"),
            TiposNotificacion.Asignacion => ("bi-signpost-split-fill", "nt-azul", "Transporte asignado"),
            TiposNotificacion.EntregaPendiente => ("bi-hourglass-split", "nt-naranja", "Falta una confirmación"),
            TiposNotificacion.Calificacion => ("bi-star-fill", "nt-amarillo", "Nueva calificación"),
            _ => ("bi-bell-fill", "nt-verde", "Notificación")
        };

        /// <summary>
        /// Pantalla a la que lleva el botón "Ver detalle" de una notificación, según lo que informa: la ubicación abre el
        /// seguimiento; la asignación abre Mis Viajes (transportador) o el seguimiento; las confirmaciones abren la lista de pedidos.
        /// Devuelve null si la notificación no está ligada a un pedido.
        /// </summary>
        public static (string Url, string Destino)? DestinoDetalleNotificacion(string tipo, int? idDemanda, string rol)
        {
            if (idDemanda == null) return null;

            return tipo switch
            {
                TiposNotificacion.Ubicacion => ($"/Ubicacion/Seguimiento?idDemanda={idDemanda}", "superpuesta"),
                TiposNotificacion.Asignacion when rol == "Transportador" => ("/Transporte/MisViajes", "principal"),
                TiposNotificacion.Asignacion => ($"/Ubicacion/Seguimiento?idDemanda={idDemanda}", "superpuesta"),
                TiposNotificacion.Llegada or TiposNotificacion.EntregaPendiente => ("/Pedidos/Lista", "principal"),
                TiposNotificacion.EntregaCompletada or TiposNotificacion.Novedad or TiposNotificacion.Calificacion => ("/Pedidos/Lista?terminados=true", "principal"),
                _ => ("/Pedidos/Lista", "principal")
            };
        }

        public static string ClaseEstado(string estado) => estado switch
        {
            EstadosEnvio.EntregadoRecibido => "bg-success",
            EstadosEnvio.EntregadoConObservaciones => "bg-warning text-dark",
            EstadosEnvio.Entregado => "bg-info text-dark",
            EstadosEnvio.EnTransito => "bg-primary",
            EstadosEnvio.Asignado => "bg-secondary",
            _ => "bg-light text-dark border"
        };
    }
}
