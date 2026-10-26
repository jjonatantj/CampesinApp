using System;
using System.Collections.Generic;

namespace CampesinApp.ViewModels
{
    /// <summary>
    /// ViewModel principal para CU-016: Consulta de Ofertadores.
    /// Contiene la lista de demandas accesibles y los ofertadores asociados a cada una.
    /// </summary>
    public class ConsultaOfertadoresViewModel
    {
        public string RolUsuario { get; set; } = "Usuario";
        public int? IdDemandaSeleccionada { get; set; }
        public List<DemandaOfertadorItemViewModel> Demandas { get; set; } = new List<DemandaOfertadorItemViewModel>();
    }

    /// <summary>
    /// Representa los datos de una demanda en la lista (Vista 1) y su cabecera en el detalle (Vista 2).
    /// </summary>
    public class DemandaOfertadorItemViewModel
    {
        public int IdDemanda { get; set; }
        public string NombreProducto { get; set; } = string.Empty;
        public string Variedad { get; set; } = string.Empty;
        public int CantidadTotal { get; set; }
        public int CantidadAportada { get; set; }
        public int CantidadRestante { get; set; }
        public DateTime FechaLimiteOfertar { get; set; }
        public DateTime FechaEntrega { get; set; }
        public int TotalOfertadores { get; set; }
        public List<OfertadorDetalleViewModel> Ofertadores { get; set; } = new List<OfertadorDetalleViewModel>();
    }

    /// <summary>
    /// Representa a un campesino ofertador en la tabla de detalle (Vista 2).
    /// </summary>
    public class OfertadorDetalleViewModel
    {
        public int IdAporte { get; set; }
        public int IdCampesino { get; set; }
        public string NombreCampesino { get; set; } = string.Empty;
        public int CantidadAportada { get; set; }
        public string UbicacionOrigen { get; set; } = string.Empty;
    }
}

