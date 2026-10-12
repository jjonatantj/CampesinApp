using System;
using System.Collections.Generic;

namespace CampesinApp.ViewModels
{
    /// <summary>
    /// ViewModel para la pantalla de Detalle y Selección de Camión (Paso 4 del CU-020).
    /// Agrupa la información centrada del pedido (origen, destino, cantidad, fecha, campesinos, comprador)
    /// y la tabla de conductores/camiones que cumplen las reglas de negocio para ser asignados.
    /// </summary>
    public class DetalleAsignacionViewModel
    {
        // ─── 1. DATOS DEL PEDIDO (CENTRADOS EN LA CABECERA) ─────────────
        public int IdDemanda { get; set; }
        public string NombreProducto { get; set; } = string.Empty;
        public string Variedad { get; set; } = string.Empty;
        public int CantidadKg { get; set; }
        public double PrecioUnitario { get; set; }
        public string LugarOrigen { get; set; } = string.Empty;
        public string LugarDestino { get; set; } = string.Empty;
        public DateTime FechaEntrega { get; set; }
        public string NombreComprador { get; set; } = string.Empty;
        public string TelefonoComprador { get; set; } = string.Empty;

        // Lista de campesinos participantes en el acopio de este pedido
        public List<CampesinoAporteItemViewModel> CampesinosParticipantes { get; set; } = new List<CampesinoAporteItemViewModel>();

        // ─── 2. TABLA DE CAMIONES / CONDUCTORES DISPONIBLES ─────────────
        public List<CamionDisponibleItemViewModel> CamionesDisponibles { get; set; } = new List<CamionDisponibleItemViewModel>();

        // ─── 3. SELECCIÓN DE CONDUCTOR PARA EL ENVÍO ────────────────────
        public int? IdCamionSeleccionado { get; set; }
    }

    /// <summary>
    /// Representa a un campesino que aportó parte de la cosecha requerida.
    /// </summary>
    public class CampesinoAporteItemViewModel
    {
        public int IdUsuario { get; set; }
        public string NombreCampesino { get; set; } = string.Empty;
        public int KilosAportados { get; set; }
        public string Municipio { get; set; } = string.Empty;
    }

    /// <summary>
    /// Representa a un camión con conductor disponible que cumple los requisitos del CU-020.
    /// </summary>
    public class CamionDisponibleItemViewModel
    {
        public int IdCamion { get; set; }
        public int IdConductor { get; set; }
        public string NombreConductor { get; set; } = string.Empty;
        public string TelefonoConductor { get; set; } = string.Empty;
        public string Placa { get; set; } = string.Empty;
        public string Modelo { get; set; } = string.Empty;
        public int CapacidadKg { get; set; }
        public bool EstadoPase { get; set; }
        public bool EstadoSeguro { get; set; }
        public bool EstadoTecno { get; set; }
    }
}

