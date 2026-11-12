using System;
using System.Collections.Generic;

namespace CampesinApp.ViewModels
{
    /// <summary>
    /// ViewModel para el caso de uso CU-015: Consulta Perfil Campesino/Comprador.
    /// Maneja los datos del perfil, reglas de privacidad y pestañas (tabs).
    /// </summary>
    public class ConsultaPerfilViewModel
    {
        // ==========================================
        // 1. Identidad básica del usuario consultado
        // ==========================================
        public int IdUsuario { get; set; }
        public string NombreCompleto { get; set; } = string.Empty;
        public string Rol { get; set; } = string.Empty; // "Campesino", "Comprador", "Transportador", "Administrador"
        public string? FotoUrl { get; set; }
        public string Iniciales { get; set; } = "U";
        public string Correo { get; set; } = string.Empty;

        // ==========================================
        // 2. Control de Privacidad y Roles (RN-01, RN-02, RN-05)
        // ==========================================
        public bool EsPerfilPropio { get; set; }
        public bool EsAdmin { get; set; }
        public string? CedulaEnmascarada { get; set; }
        public string? CedulaExacta { get; set; } // Solo visible para perfil propio o Admin
        public string EstadoCuenta { get; set; } = "Activo"; // Solo visible para Admin (RN-05)

        // ==========================================
        // 3. Pestaña 1: Información General
        // ==========================================
        public string Municipio { get; set; } = "No especificado";
        public string Departamento { get; set; } = "Colombia";
        public string? Direccion { get; set; }
        public string Telefono { get; set; } = "No registrado";
        public double CalificacionPromedio { get; set; } = 5.0;
        public int TotalResenas { get; set; } = 0;

        // ==========================================
        // 4. Pestaña 2: Productos Cultivados (Campesino) o Historial de Ventas (Comprador)
        // ==========================================
        public string TituloPestana2 { get; set; } = "Productos Cultivados";
        public List<ItemPerfilViewModel> ItemsPestana2 { get; set; } = new List<ItemPerfilViewModel>();
        public List<string> CategoriasDisponibles { get; set; } = new List<string>();

        // ==========================================
        // 5. Modal: Reputación y Calificaciones
        // ==========================================
        public List<CalificacionItemViewModel> Calificaciones { get; set; } = new List<CalificacionItemViewModel>();
    }

    /// <summary>
    /// Representa un producto cultivado por un campesino o una compra/demanda de un comprador.
    /// </summary>
    public class ItemPerfilViewModel
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Categoria { get; set; } = "General";
        public string Variedad { get; set; } = "Estándar";
        public decimal PrecioEstimado { get; set; }
        public string UnidadMedida { get; set; } = "Kg";
        public string? Descripcion { get; set; }
        public string? ImagenUrl { get; set; }
        public int Cantidad { get; set; }
        public decimal Total { get; set; }
        public string? UbicacionCultivo { get; set; }
        public DateTime? Fecha { get; set; }
        public string Estado { get; set; } = "Completada";
    }

    /// <summary>
    /// Representa una calificación/opinión de otros usuarios para la ventana emergente.
    /// </summary>
    public class CalificacionItemViewModel
    {
        public string Autor { get; set; } = "Usuario";
        public string RolAutor { get; set; } = "Comprador";
        public int Estrellas { get; set; } = 5;
        public string Comentario { get; set; } = string.Empty;
        public DateTime Fecha { get; set; } = DateTime.Now;
    }
}

