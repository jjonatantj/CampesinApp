using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace CampesinApp.Repositories.Models;

public partial class Campesinapp3Context : DbContext
{
    public Campesinapp3Context()
    {
    }

    public Campesinapp3Context(DbContextOptions<Campesinapp3Context> options)
        : base(options)
    {
    }

    public virtual DbSet<AporteCamp> AporteCamps { get; set; }

    public virtual DbSet<Camion> Camions { get; set; }

    public virtual DbSet<Consignacion> Consignacions { get; set; }

    public virtual DbSet<DatoDane> DatoDanes { get; set; }

    public virtual DbSet<Demandum> Demanda { get; set; }

    public virtual DbSet<Departamento> Departamentos { get; set; }

    public virtual DbSet<EntidadFinanciera> EntidadFinancieras { get; set; }

    public virtual DbSet<EstadoDemandum> EstadoDemanda { get; set; }

    public virtual DbSet<Municipio> Municipios { get; set; }

    public virtual DbSet<Observacion> Observacions { get; set; }

    public virtual DbSet<PagoComprador> PagoCompradors { get; set; }

    public virtual DbSet<PagoTransportador> PagoTransportadors { get; set; }

    public virtual DbSet<PerfilPago> PerfilPagos { get; set; }

    public virtual DbSet<PrecioEstimado> PrecioEstimados { get; set; }

    public virtual DbSet<Producto> Productos { get; set; }

    public virtual DbSet<ReporteTransportador> ReporteTransportadors { get; set; }

    public virtual DbSet<Rol> Rols { get; set; }

    public virtual DbSet<TipoCuentum> TipoCuenta { get; set; }

    public virtual DbSet<TipoPago> TipoPagos { get; set; }

    public virtual DbSet<TipoProducto> TipoProductos { get; set; }

    public virtual DbSet<Usuario> Usuarios { get; set; }

    public virtual DbSet<Variedad> Variedads { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseSqlServer("Name=ConnectionStrings:Test");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AporteCamp>(entity =>
        {
            entity.HasKey(e => e.IdAporte);

            entity.ToTable("aporte_Camp");

            entity.Property(e => e.IdAporte)
                .ValueGeneratedNever()
                .HasColumnName("id_Aporte");
            entity.Property(e => e.CantAporte).HasColumnName("cant_Aporte");
            entity.Property(e => e.IdDemanda).HasColumnName("id_Demanda");
            entity.Property(e => e.IdUsuario).HasColumnName("id_Usuario");
            entity.Property(e => e.Imagen)
                .HasMaxLength(60)
                .IsUnicode(false)
                .HasColumnName("imagen");

            entity.HasOne(d => d.IdDemandaNavigation).WithMany(p => p.AporteCamps)
                .HasForeignKey(d => d.IdDemanda)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_aporte_Camp_demanda");

            entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.AporteCamps)
                .HasForeignKey(d => d.IdUsuario)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_aporte_Camp_usuario");
        });

        modelBuilder.Entity<Camion>(entity =>
        {
            entity.HasKey(e => e.IdCamion);

            entity.ToTable("camion");

            entity.Property(e => e.IdCamion)
                .ValueGeneratedNever()
                .HasColumnName("id_Camion");
            entity.Property(e => e.CapacidadCami).HasColumnName("capacidad_Cami");
            entity.Property(e => e.EstadoPase).HasColumnName("estado_pase");
            entity.Property(e => e.EstadoSeguro).HasColumnName("estado_seguro");
            entity.Property(e => e.EstadoTecno).HasColumnName("estado_tecno");
            entity.Property(e => e.IdUsuario).HasColumnName("id_Usuario");
            entity.Property(e => e.ModeloCami)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("modelo_Cami");
            entity.Property(e => e.PlacaCami)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("placa_Cami");

            entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.Camions)
                .HasForeignKey(d => d.IdUsuario)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_camion_usuario");
        });

        modelBuilder.Entity<Consignacion>(entity =>
        {
            entity.HasKey(e => e.IdConsignacionCam);

            entity.ToTable("consignacion");

            entity.Property(e => e.IdConsignacionCam)
                .ValueGeneratedNever()
                .HasColumnName("id_Consignacion_Cam");
            entity.Property(e => e.ConsigCampesino).HasColumnName("consig_Campesino");
            entity.Property(e => e.FechaConsigCam)
                .HasColumnType("datetime")
                .HasColumnName("fecha_Consig_Cam");
            entity.Property(e => e.IdAporte).HasColumnName("id_Aporte");
            entity.Property(e => e.IdPerfilPago).HasColumnName("id_Perfil_Pago");
            entity.Property(e => e.IdUsuario).HasColumnName("id_Usuario");

            entity.HasOne(d => d.IdAporteNavigation).WithMany(p => p.Consignacions)
                .HasForeignKey(d => d.IdAporte)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_consignacion_aporte_Camp");

            entity.HasOne(d => d.IdPerfilPagoNavigation).WithMany(p => p.Consignacions)
                .HasForeignKey(d => d.IdPerfilPago)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_consignacion_perfil_Pago");

            entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.Consignacions)
                .HasForeignKey(d => d.IdUsuario)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_consignacion_usuario");
        });

        modelBuilder.Entity<DatoDane>(entity =>
        {
            entity.HasKey(e => e.IdDatoDane);

            entity.ToTable("dato_Dane");

            entity.Property(e => e.IdDatoDane)
                .ValueGeneratedNever()
                .HasColumnName("id_Dato_Dane");
            entity.Property(e => e.FechaDato).HasColumnName("fecha_Dato");
            entity.Property(e => e.IdTipoProducto).HasColumnName("id_Tipo_Producto");
            entity.Property(e => e.IdVariedad).HasColumnName("id_Variedad");
            entity.Property(e => e.PrecioDato).HasColumnName("precio_Dato");

            entity.HasOne(d => d.IdTipoProductoNavigation).WithMany(p => p.DatoDanes)
                .HasForeignKey(d => d.IdTipoProducto)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_dato_Dane_tipo_Producto");

            entity.HasOne(d => d.IdVariedadNavigation).WithMany(p => p.DatoDanes)
                .HasForeignKey(d => d.IdVariedad)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_dato_Dane_variedad");
        });

        modelBuilder.Entity<Demandum>(entity =>
        {
            entity.HasKey(e => e.IdDemanda);

            entity.ToTable("demanda");

            entity.Property(e => e.IdDemanda)
                .ValueGeneratedNever()
                .HasColumnName("id_Demanda");
            entity.Property(e => e.CantidadDeman).HasColumnName("cantidad_Deman");
            entity.Property(e => e.DescripcionDeman)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("descripcion_Deman");
            entity.Property(e => e.DireccionFinal)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("direccion_Final");
            entity.Property(e => e.FechaEntrega)
                .HasColumnType("datetime")
                .HasColumnName("fecha_Entrega");
            entity.Property(e => e.IdCamion).HasColumnName("id_Camion");
            entity.Property(e => e.IdEstadoDeman).HasColumnName("id_Estado_Deman");
            entity.Property(e => e.IdMunicipio).HasColumnName("id_Municipio");
            entity.Property(e => e.IdTipoProducto).HasColumnName("id_Tipo_Producto");
            entity.Property(e => e.IdUsuario).HasColumnName("id_Usuario");
            entity.Property(e => e.IdVariedad).HasColumnName("id_Variedad");
            entity.Property(e => e.LimTiemDeman)
                .HasColumnType("datetime")
                .HasColumnName("lim_Tiem_Deman");
            entity.Property(e => e.PrecioDeman).HasColumnName("precio_Deman");

            entity.HasOne(d => d.IdEstadoDemanNavigation).WithMany(p => p.Demanda)
                .HasForeignKey(d => d.IdEstadoDeman)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_demanda_estado_Demanda");

            entity.HasOne(d => d.IdMunicipioNavigation).WithMany(p => p.Demanda)
                .HasForeignKey(d => d.IdMunicipio)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_demanda_municipio");

            entity.HasOne(d => d.IdTipoProductoNavigation).WithMany(p => p.Demanda)
                .HasForeignKey(d => d.IdTipoProducto)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_demanda_tipo_Producto");

            entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.Demanda)
                .HasForeignKey(d => d.IdUsuario)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_demanda_usuario");

            entity.HasOne(d => d.IdVariedadNavigation).WithMany(p => p.Demanda)
                .HasForeignKey(d => d.IdVariedad)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_demanda_variedad");
        });

        modelBuilder.Entity<Departamento>(entity =>
        {
            entity.HasKey(e => e.IdDepartamento);

            entity.ToTable("departamento");

            entity.Property(e => e.IdDepartamento)
                .ValueGeneratedNever()
                .HasColumnName("id_Departamento");
            entity.Property(e => e.Departamento1)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("departamento");
        });

        modelBuilder.Entity<EntidadFinanciera>(entity =>
        {
            entity.HasKey(e => e.IdEntiFinanc);

            entity.ToTable("entidad_Financiera");

            entity.Property(e => e.IdEntiFinanc)
                .ValueGeneratedNever()
                .HasColumnName("id_Enti_Financ");
            entity.Property(e => e.EntiFinanc)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("enti_Financ");
        });

        modelBuilder.Entity<EstadoDemandum>(entity =>
        {
            entity.HasKey(e => e.IdEstadoDeman);

            entity.ToTable("estado_Demanda");

            entity.Property(e => e.IdEstadoDeman)
                .ValueGeneratedNever()
                .HasColumnName("id_Estado_Deman");
            entity.Property(e => e.Estado)
                .HasMaxLength(15)
                .IsUnicode(false)
                .HasColumnName("estado");
        });

        modelBuilder.Entity<Municipio>(entity =>
        {
            entity.HasKey(e => e.IdMunicipio);

            entity.ToTable("municipio");

            entity.Property(e => e.IdMunicipio)
                .ValueGeneratedNever()
                .HasColumnName("id_Municipio");
            entity.Property(e => e.IdDepartamento).HasColumnName("id_Departamento");
            entity.Property(e => e.Municipio1)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("municipio");

            entity.HasOne(d => d.IdDepartamentoNavigation).WithMany(p => p.Municipios)
                .HasForeignKey(d => d.IdDepartamento)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_municipio_departamento");
        });

        modelBuilder.Entity<Observacion>(entity =>
        {
            entity.HasKey(e => e.IdObservacion);

            entity.ToTable("observacion");

            entity.Property(e => e.IdObservacion)
                .ValueGeneratedNever()
                .HasColumnName("id_Observacion");
            entity.Property(e => e.CalificacionEstre).HasColumnName("calificacion_Estre");
            entity.Property(e => e.HoraObser)
                .HasColumnType("datetime")
                .HasColumnName("hora_Obser");
            entity.Property(e => e.IdDemanda).HasColumnName("id_Demanda");
            entity.Property(e => e.ImagenComprador)
                .HasMaxLength(60)
                .IsUnicode(false)
                .HasColumnName("imagen_Comprador");
            entity.Property(e => e.ImagenTranspor)
                .HasMaxLength(60)
                .IsUnicode(false)
                .HasColumnName("imagen_Transpor");
            entity.Property(e => e.ObserComprador)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("obser_Comprador");
            entity.Property(e => e.ObserTransportador)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("obser_Transportador");

            entity.HasOne(d => d.IdDemandaNavigation).WithMany(p => p.Observacions)
                .HasForeignKey(d => d.IdDemanda)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_observacion_demanda");
        });

        modelBuilder.Entity<PagoComprador>(entity =>
        {
            entity.HasKey(e => e.IdPago);

            entity.ToTable("pago_Comprador");

            entity.Property(e => e.IdPago)
                .ValueGeneratedNever()
                .HasColumnName("id_Pago");
            entity.Property(e => e.FechaPagoC)
                .HasColumnType("datetime")
                .HasColumnName("fecha_Pago_C");
            entity.Property(e => e.IdDemanda).HasColumnName("id_Demanda");
            entity.Property(e => e.IdPerfilPago).HasColumnName("id_Perfil_Pago");
            entity.Property(e => e.IdTipoPago).HasColumnName("id_Tipo_Pago");
            entity.Property(e => e.IdUsuario).HasColumnName("id_Usuario");
            entity.Property(e => e.PagoComprador1).HasColumnName("pago_Comprador");

            entity.HasOne(d => d.IdDemandaNavigation).WithMany(p => p.PagoCompradors)
                .HasForeignKey(d => d.IdDemanda)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_pago_Comprador_demanda");

            entity.HasOne(d => d.IdPerfilPagoNavigation).WithMany(p => p.PagoCompradors)
                .HasForeignKey(d => d.IdPerfilPago)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_pago_Comprador_perfil_Pago");

            entity.HasOne(d => d.IdTipoPagoNavigation).WithMany(p => p.PagoCompradors)
                .HasForeignKey(d => d.IdTipoPago)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_pago_Comprador_tipo_Pago");
        });

        modelBuilder.Entity<PagoTransportador>(entity =>
        {
            entity.HasKey(e => e.IdConsignacionTrans);

            entity.ToTable("pago_Transportador");

            entity.Property(e => e.IdConsignacionTrans)
                .ValueGeneratedNever()
                .HasColumnName("id_Consignacion_Trans");
            entity.Property(e => e.AnticipoTrans).HasColumnName("anticipo_Trans");
            entity.Property(e => e.FechaAntiTrans)
                .HasColumnType("datetime")
                .HasColumnName("fecha_Anti_Trans");
            entity.Property(e => e.FechaPagoTrans)
                .HasColumnType("datetime")
                .HasColumnName("fecha_Pago_Trans");
            entity.Property(e => e.IdDemanda).HasColumnName("id_Demanda");
            entity.Property(e => e.IdPerfilPago).HasColumnName("id_Perfil_Pago");
            entity.Property(e => e.IdUsuario).HasColumnName("id_Usuario");
            entity.Property(e => e.PagoCompleTrans).HasColumnName("pago_Comple_Trans");

            entity.HasOne(d => d.IdDemandaNavigation).WithMany(p => p.PagoTransportadors)
                .HasForeignKey(d => d.IdDemanda)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_pago_Transportador_demanda");

            entity.HasOne(d => d.IdPerfilPagoNavigation).WithMany(p => p.PagoTransportadors)
                .HasForeignKey(d => d.IdPerfilPago)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_pago_Transportador_perfil_Pago");

            entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.PagoTransportadors)
                .HasForeignKey(d => d.IdUsuario)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_pago_Transportador_usuario");
        });

        modelBuilder.Entity<PerfilPago>(entity =>
        {
            entity.HasKey(e => e.IdPerfilPago);

            entity.ToTable("perfil_Pago");

            entity.Property(e => e.IdPerfilPago)
                .ValueGeneratedNever()
                .HasColumnName("id_Perfil_Pago");
            entity.Property(e => e.Cvc)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("cvc");
            entity.Property(e => e.IdEntiFinanc).HasColumnName("id_Enti_Financ");
            entity.Property(e => e.IdTipoCuenta).HasColumnName("id_Tipo_Cuenta");
            entity.Property(e => e.IdTipoPago).HasColumnName("id_Tipo_Pago");
            entity.Property(e => e.IdUsuario).HasColumnName("id_Usuario");
            entity.Property(e => e.NumCuenta)
                .HasMaxLength(12)
                .IsUnicode(false)
                .HasColumnName("num_Cuenta");

            entity.HasOne(d => d.IdEntiFinancNavigation).WithMany(p => p.PerfilPagos)
                .HasForeignKey(d => d.IdEntiFinanc)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_perfil_Pago_entidad_Financiera");

            entity.HasOne(d => d.IdTipoCuentaNavigation).WithMany(p => p.PerfilPagos)
                .HasForeignKey(d => d.IdTipoCuenta)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_perfil_Pago_tipo_Cuenta");

            entity.HasOne(d => d.IdTipoPagoNavigation).WithMany(p => p.PerfilPagos)
                .HasForeignKey(d => d.IdTipoPago)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_perfil_Pago_tipo_Pago");

            entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.PerfilPagos)
                .HasForeignKey(d => d.IdUsuario)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_perfil_Pago_usuario");
        });

        modelBuilder.Entity<PrecioEstimado>(entity =>
        {
            entity.HasKey(e => e.IdPrecioEstimado);

            entity.ToTable("precio_Estimado");

            entity.Property(e => e.IdPrecioEstimado)
                .ValueGeneratedNever()
                .HasColumnName("id_Precio_Estimado");
            entity.Property(e => e.FechaPreci).HasColumnName("fecha_Preci");
            entity.Property(e => e.IdTipoProducto).HasColumnName("id_Tipo_Producto");
            entity.Property(e => e.IdVariedad).HasColumnName("id_Variedad");
            entity.Property(e => e.PrecioEsti).HasColumnName("precio_Esti");

            entity.HasOne(d => d.IdTipoProductoNavigation).WithMany(p => p.PrecioEstimados)
                .HasForeignKey(d => d.IdTipoProducto)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_precio_Estimado_tipo_Producto");

            entity.HasOne(d => d.IdVariedadNavigation).WithMany(p => p.PrecioEstimados)
                .HasForeignKey(d => d.IdVariedad)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_precio_Estimado_variedad");
        });

        modelBuilder.Entity<Producto>(entity =>
        {
            entity.HasKey(e => e.IdProducto).HasName("PK_producto_1");

            entity.ToTable("producto");

            entity.Property(e => e.IdProducto)
                .ValueGeneratedNever()
                .HasColumnName("id_Producto");
            entity.Property(e => e.DescripcionProduc)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("descripcion_Produc");
            entity.Property(e => e.IdTipoProducto).HasColumnName("id_Tipo_Producto");
            entity.Property(e => e.IdUsuario).HasColumnName("id_Usuario");
            entity.Property(e => e.IdVariedad).HasColumnName("id_Variedad");
            entity.Property(e => e.ImagenProduc)
                .HasMaxLength(60)
                .IsUnicode(false)
                .HasColumnName("imagen_Produc");
            entity.Property(e => e.UbicacionCultivo)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("ubicacion_Cultivo");

            entity.HasOne(d => d.IdTipoProductoNavigation).WithMany(p => p.Productos)
                .HasForeignKey(d => d.IdTipoProducto)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_producto_tipo_Producto");

            entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.Productos)
                .HasForeignKey(d => d.IdUsuario)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_producto_usuario");

            entity.HasOne(d => d.IdVariedadNavigation).WithMany(p => p.Productos)
                .HasForeignKey(d => d.IdVariedad)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_producto_variedad");
        });

        modelBuilder.Entity<ReporteTransportador>(entity =>
        {
            entity.HasKey(e => e.IdReporte);

            entity.ToTable("reporte_Transportador");

            entity.Property(e => e.IdReporte)
                .ValueGeneratedNever()
                .HasColumnName("id_Reporte");
            entity.Property(e => e.FechaActual)
                .HasColumnType("datetime")
                .HasColumnName("fecha_Actual");
            entity.Property(e => e.FechaFinal)
                .HasColumnType("datetime")
                .HasColumnName("fecha_Final");
            entity.Property(e => e.FechaInicio)
                .HasColumnType("datetime")
                .HasColumnName("fecha_Inicio");
            entity.Property(e => e.IdDemanada).HasColumnName("id_Demanada");
            entity.Property(e => e.IdUsuario).HasColumnName("id_Usuario");
            entity.Property(e => e.UbucacionActual)
                .HasColumnType("datetime")
                .HasColumnName("ubucacion_Actual");

            entity.HasOne(d => d.IdDemanadaNavigation).WithMany(p => p.ReporteTransportadors)
                .HasForeignKey(d => d.IdDemanada)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_reporte_Transportador_demanda");

            entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.ReporteTransportadors)
                .HasForeignKey(d => d.IdUsuario)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_reporte_Transportador_usuario");
        });

        modelBuilder.Entity<Rol>(entity =>
        {
            entity.HasKey(e => e.IdRol);

            entity.ToTable("rol");

            entity.Property(e => e.IdRol)
                .ValueGeneratedNever()
                .HasColumnName("id_Rol");
            entity.Property(e => e.TipoRol)
                .HasMaxLength(15)
                .IsUnicode(false)
                .HasColumnName("tipo_Rol");
        });

        modelBuilder.Entity<TipoCuentum>(entity =>
        {
            entity.HasKey(e => e.IdTipoCuenta);

            entity.ToTable("tipo_Cuenta");

            entity.Property(e => e.IdTipoCuenta)
                .ValueGeneratedNever()
                .HasColumnName("id_Tipo_Cuenta");
            entity.Property(e => e.TipoCuenta)
                .HasMaxLength(15)
                .IsUnicode(false)
                .HasColumnName("tipo_Cuenta");
        });

        modelBuilder.Entity<TipoPago>(entity =>
        {
            entity.HasKey(e => e.IdTipoPago);

            entity.ToTable("tipo_Pago");

            entity.Property(e => e.IdTipoPago)
                .ValueGeneratedNever()
                .HasColumnName("id_Tipo_Pago");
            entity.Property(e => e.TipoPago1)
                .HasMaxLength(12)
                .IsUnicode(false)
                .HasColumnName("tipo_Pago");
        });

        modelBuilder.Entity<TipoProducto>(entity =>
        {
            entity.HasKey(e => e.IdTipoProducto).HasName("PK_producto");

            entity.ToTable("tipo_Producto");

            entity.Property(e => e.IdTipoProducto)
                .ValueGeneratedNever()
                .HasColumnName("id_Tipo_Producto");
            entity.Property(e => e.Producto)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("producto");
        });

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.HasKey(e => e.IdUsuario);

            entity.ToTable("usuario");

            entity.Property(e => e.IdUsuario).HasColumnName("id_Usuario");
            entity.Property(e => e.ApellidoCom)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("apellido_com");
            entity.Property(e => e.CedulaUsu).HasColumnName("cedula_Usu");
            entity.Property(e => e.ContrasenaUsu)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("contrasena_Usu");
            entity.Property(e => e.CorreoUsu)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("correo_Usu");
            entity.Property(e => e.Estado).HasColumnName("estado");
            entity.Property(e => e.IdMunicipio).HasColumnName("id_Municipio");
            entity.Property(e => e.IdRol).HasColumnName("id_Rol");
            entity.Property(e => e.Imagen)
                .HasMaxLength(60)
                .IsUnicode(false)
                .HasColumnName("imagen");
            entity.Property(e => e.NombreCom)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("nombre_com");
            entity.Property(e => e.TelelfonoUsu).HasColumnName("telelfono_Usu");

            entity.HasOne(d => d.IdMunicipioNavigation).WithMany(p => p.Usuarios)
                .HasForeignKey(d => d.IdMunicipio)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_usuario_municipio");

            entity.HasOne(d => d.IdRolNavigation).WithMany(p => p.Usuarios)
                .HasForeignKey(d => d.IdRol)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_usuario_rol");
        });

        modelBuilder.Entity<Variedad>(entity =>
        {
            entity.HasKey(e => e.IdVariedad);

            entity.ToTable("variedad");

            entity.Property(e => e.IdVariedad)
                .ValueGeneratedNever()
                .HasColumnName("id_Variedad");
            entity.Property(e => e.IdTipoProducto).HasColumnName("id_Tipo_Producto");
            entity.Property(e => e.VariedadProd)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("variedad_Prod");

            entity.HasOne(d => d.IdTipoProductoNavigation).WithMany(p => p.Variedads)
                .HasForeignKey(d => d.IdTipoProducto)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_variedad_tipo_Producto");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
