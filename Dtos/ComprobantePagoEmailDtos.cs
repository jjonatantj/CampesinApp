using System;

namespace CampesinApp.Dtos
{
    /// <summary>
    /// Datos requeridos para generar y enviar el comprobante de liquidación por correo al campesino.
    /// </summary>
    public class ComprobantePagoCampesinoEmailDto
    {
        public string CorreoDestino { get; set; } = string.Empty;
        public string NombreCampesino { get; set; } = string.Empty;
        public string CodigoPedido { get; set; } = string.Empty;
        public string Producto { get; set; } = string.Empty;
        public string Variedad { get; set; } = string.Empty;
        public int KilosAportados { get; set; }
        public double PrecioPorKilo { get; set; }
        public double TotalLiquidado { get; set; }
        public string BancoDestino { get; set; } = string.Empty;
        public string CuentaDestino { get; set; } = string.Empty;
        public DateTime FechaPago { get; set; }
    }

    /// <summary>
    /// Datos requeridos para generar y enviar el comprobante de pago de flete por correo al transportador.
    /// </summary>
    public class ComprobantePagoTransportadorEmailDto
    {
        public string CorreoDestino { get; set; } = string.Empty;
        public string NombreTransportador { get; set; } = string.Empty;
        public string CodigoPedido { get; set; } = string.Empty;
        public string Producto { get; set; } = string.Empty;
        public string Variedad { get; set; } = string.Empty;
        public int TotalKilos { get; set; }
        public string DestinoEntrega { get; set; } = string.Empty;
        public double FleteTotal { get; set; }
        public double Anticipo { get; set; }
        public double SaldoLiquidado { get; set; }
        public string BancoDestino { get; set; } = string.Empty;
        public string CuentaDestino { get; set; } = string.Empty;
        public DateTime FechaPago { get; set; }
    }
}

