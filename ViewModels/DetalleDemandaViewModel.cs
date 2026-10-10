using System;

namespace CampesinApp.ViewModels
{
    /// <summary>
    /// ViewModel para la pantalla de Detalle / Responder a Demanda de Producto (CU-012).
    /// Agrupa todos los datos que la vista necesita mostrar: producto, cantidades (total y faltante),
    /// precio, fechas, logística y los datos públicos del comprador para la ventana modal.
    /// </summary>
    public class DetalleDemandaViewModel
    {
        // 1. Identificador de la Demanda
        public int IdDemanda { get; set; }

        // 2. Información del Producto Agrícola
        public string NombreProducto { get; set; } = string.Empty;
        public string VariedadProducto { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public string EstadoDemanda { get; set; } = "Activa";

        // 3. Cantidades y Cálculo de Aporte (Kilos)
        public int CantidadTotal { get; set; }
        public int CantidadAportada { get; set; }
        public int CantidadFaltante { get; set; }
        public double PorcentajeCompletado => CantidadTotal > 0 
            ? Math.Min(100.0, Math.Round((double)CantidadAportada / CantidadTotal * 100, 1)) 
            : 0;

        // 4. Condiciones Comerciales
        public double PrecioPropuesto { get; set; }

        // 5. Ubicación y Logística de Entrega
        public string DireccionEntrega { get; set; } = string.Empty;
        public string MunicipioEntrega { get; set; } = string.Empty;
        public string DepartamentoEntrega { get; set; } = string.Empty;

        // 6. Fechas Clave
        public DateTime FechaLimite { get; set; }
        public DateTime FechaEntrega { get; set; }

        // 7. Datos Públicos del Comprador (Modal FA02-CU012)
        public int CompradorId { get; set; }
        public string CompradorNombreCompleto { get; set; } = string.Empty;
        public string CompradorTelefono { get; set; } = string.Empty;
        public string CompradorCorreo { get; set; } = string.Empty;
        public string CompradorMunicipio { get; set; } = string.Empty;

        // 8. Información de Sesión del Usuario
        public bool EsUsuarioAutenticado { get; set; }
        public string? RolUsuario { get; set; }
    }
}

