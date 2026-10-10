using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CampesinApp.Repositories.Models
{
    /// <summary>
    /// Entidad para el Caso de Uso CU-018: Registro de Puntos de Acopio.
    /// Mapea la tabla punto_Acopio en la base de datos de SQL Server.
    /// </summary>
    [Table("punto_Acopio")]
    public class PuntoAcopio
    {
        [Key]
        [Column("id_Punto_Acopio")]
        public int IdPuntoAcopio { get; set; }

        [Column("id_Usuario")]
        public int IdUsuario { get; set; }

        [Required]
        [Column("nombre_Punto")]
        [StringLength(100)]
        public string NombrePunto { get; set; } = string.Empty;

        [Column("id_Departamento")]
        public int IdDepartamento { get; set; }

        [Column("id_Municipio")]
        public int IdMunicipio { get; set; }

        [Required]
        [Column("direccion")]
        [StringLength(150)]
        public string Direccion { get; set; } = string.Empty;

        // Regla RN-01-CU018: Ubicación geográfica obligatoria
        [Column("latitud", TypeName = "decimal(10, 7)")]
        public decimal Latitud { get; set; }

        [Column("longitud", TypeName = "decimal(10, 7)")]
        public decimal Longitud { get; set; }

        [Column("capacidad", TypeName = "decimal(10, 2)")]
        public decimal Capacidad { get; set; }

        // Regla RN-02-CU018: Acceso vehicular para camiones
        [Column("acceso_Vehicular")]
        public bool AccesoVehicular { get; set; }

        [Column("fecha_Registro", TypeName = "datetime")]
        public DateTime FechaRegistro { get; set; } = DateTime.Now;

        // Propiedades de Navegación relacional
        [ForeignKey("IdUsuario")]
        public virtual Usuario? IdUsuarioNavigation { get; set; }

        [ForeignKey("IdDepartamento")]
        public virtual Departamento? IdDepartamentoNavigation { get; set; }

        [ForeignKey("IdMunicipio")]
        public virtual Municipio? IdMunicipioNavigation { get; set; }
    }
}

