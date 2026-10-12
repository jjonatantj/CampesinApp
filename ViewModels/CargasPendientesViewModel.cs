using System;
using System.Collections.Generic;

namespace CampesinApp.ViewModels
{
    /// <summary>
    /// ViewModel para la pantalla de Cargas Pendientes (Paso 2 de CU-020).
    /// Contiene la lista de demandas que aún no cuentan con un camión asignado (id_Camion == null).
    /// </summary>
    public class CargasPendientesViewModel
    {
        public List<CargaPendienteItemViewModel> Cargas { get; set; } = new List<CargaPendienteItemViewModel>();
        public bool TieneErrorConexion { get; set; }
        public string? MensajeError { get; set; }
    }

    /// <summary>
    /// Modelo representativo para cada fila o tarjeta de la lista de cargas pendientes.
    /// </summary>
    public class CargaPendienteItemViewModel
    {
        public int IdDemanda { get; set; }
        public string NombreProducto { get; set; } = string.Empty;
        public string Variedad { get; set; } = string.Empty;
        public int CantidadKg { get; set; }
        public string LugarDestino { get; set; } = string.Empty;
        public DateTime FechaEntrega { get; set; }
        public string NombreComprador { get; set; } = string.Empty;
    }
}

