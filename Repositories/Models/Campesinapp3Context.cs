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

    public virtual DbSet<AlertaGuardadum> AlertaGuardada { get; set; }

    public virtual DbSet<AporteCamp> AporteCamps { get; set; }

    public virtual DbSet<Camion> Camions { get; set; }

    public virtual DbSet<Consignacion> Consignacions { get; set; }

    public virtual DbSet<DatoDane> DatoDanes { get; set; }

    public virtual DbSet<Demandum> Demanda { get; set; }

    public virtual DbSet<Departamento> Departamentos { get; set; }

    public virtual DbSet<EntidadFinanciera> EntidadFinancieras { get; set; }

    public virtual DbSet<EstadoDemandum> EstadoDemanda { get; set; }

    public virtual DbSet<LogSistema> LogSistemas { get; set; }

    public virtual DbSet<Municipio> Municipios { get; set; }

    public virtual DbSet<Notificacion> Notificacions { get; set; }

    public virtual DbSet<Observacion> Observacions { get; set; }

    public virtual DbSet<PagoComprador> PagoCompradors { get; set; }

    public virtual DbSet<PagoTransportador> PagoTransportadors { get; set; }

    public virtual DbSet<PerfilPago> PerfilPagos { get; set; }

    public virtual DbSet<PrecioEstimado> PrecioEstimados { get; set; }

    public virtual DbSet<Producto> Productos { get; set; }

    public virtual DbSet<PuntoAcopio> PuntoAcopios { get; set; }

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
        modelBuilder.Entity<AlertaGuardadum>(entity =>
        {
            entity.HasKey(e => e.IdAlertaGuardada);

            entity.ToTable("alerta_Guardada");

            entity.HasIndex(e => new { e.IdReporte, e.IdUsuario }, "UQ_alerta_Guardada").IsUnique();

            entity.Property(e => e.IdAlertaGuardada).HasColumnName("id_Alerta_Guardada");
            entity.Property(e => e.FechaGuardado)
                .HasColumnType("datetime")
                .HasColumnName("fecha_Guardado");
            entity.Property(e => e.IdReporte).HasColumnName("id_Reporte");
            entity.Property(e => e.IdUsuario).HasColumnName("id_Usuario");

            entity.HasOne(d => d.IdReporteNavigation).WithMany(p => p.AlertaGuardada)
                .HasForeignKey(d => d.IdReporte)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_alerta_Guardada_reporte_Transportador");

            entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.AlertaGuardada)
                .HasForeignKey(d => d.IdUsuario)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_alerta_Guardada_usuario");
        });

        modelBuilder.Entity<AporteCamp>(entity =>
        {
            entity.HasKey(e => e.IdAporte);

            entity.ToTable("aporte_Camp");

            entity.Property(e => e.IdAporte).HasColumnName("id_Aporte");
            entity.Property(e => e.CantAporte).HasColumnName("cant_Aporte");
            entity.Property(e => e.IdDemanda).HasColumnName("id_Demanda");
            entity.Property(e => e.IdPuntoAcopio).HasColumnName("id_Punto_Acopio");
            entity.Property(e => e.IdUsuario).HasColumnName("id_Usuario");
            entity.Property(e => e.Imagen)
                .HasMaxLength(60)
                .IsUnicode(false)
                .HasColumnName("imagen");

            entity.HasOne(d => d.IdDemandaNavigation).WithMany(p => p.AporteCamps)
                .HasForeignKey(d => d.IdDemanda)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_aporte_Camp_demanda");

            entity.HasOne(d => d.IdPuntoAcopioNavigation).WithMany(p => p.AporteCamps)
                .HasForeignKey(d => d.IdPuntoAcopio)
                .HasConstraintName("FK_aporte_Camp_punto_Acopio");

            entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.AporteCamps)
                .HasForeignKey(d => d.IdUsuario)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_aporte_Camp_usuario");
        });

        modelBuilder.Entity<Camion>(entity =>
        {
            entity.HasKey(e => e.IdCamion);

            entity.ToTable("camion");

            entity.Property(e => e.IdCamion).HasColumnName("id_Camion");
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

            entity.Property(e => e.IdConsignacionCam).HasColumnName("id_Consignacion_Cam");
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

            entity.Property(e => e.IdDatoDane).HasColumnName("id_Dato_Dane");
            entity.Property(e => e.FechaDato).HasColumnName("fecha_Dato");
            entity.Property(e => e.IdVariedad).HasColumnName("id_Variedad");
            entity.Property(e => e.PrecioCorabastos)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("precio_Corabastos");
            entity.Property(e => e.PrecioDane).HasColumnName("precio_Dane");

            entity.HasOne(d => d.IdVariedadNavigation).WithMany(p => p.DatoDanes)
                .HasForeignKey(d => d.IdVariedad)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_dato_Dane_variedad");
        });

        modelBuilder.Entity<Demandum>(entity =>
        {
            entity.HasKey(e => e.IdDemanda);

            entity.ToTable("demanda");

            entity.Property(e => e.IdDemanda).HasColumnName("id_Demanda");
            entity.Property(e => e.CantidadDeman).HasColumnName("cantidad_Deman");
            entity.Property(e => e.DescripcionDeman)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("descripcion_Deman");
            entity.Property(e => e.DireccionInicial)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("direccion_Inicial");
            entity.Property(e => e.FechaEntrega)
                .HasColumnType("datetime")
                .HasColumnName("fecha_Entrega");
            entity.Property(e => e.IdCamion).HasColumnName("id_Camion");
            entity.Property(e => e.IdEstadoDeman).HasColumnName("id_Estado_Deman");
            entity.Property(e => e.IdPuntoAcopio).HasColumnName("id_Punto_Acopio");
            entity.Property(e => e.IdTipoProducto).HasColumnName("id_Tipo_Producto");
            entity.Property(e => e.IdUsuario).HasColumnName("id_Usuario");
            entity.Property(e => e.IdVariedad).HasColumnName("id_Variedad");
            entity.Property(e => e.LimTiemDeman)
                .HasColumnType("datetime")
                .HasColumnName("lim_Tiem_Deman");
            entity.Property(e => e.PrecioDeman).HasColumnName("precio_Deman");

            entity.HasOne(d => d.IdCamionNavigation).WithMany(p => p.Demanda)
                .HasForeignKey(d => d.IdCamion)
                .HasConstraintName("FK_demanda_camion");

            entity.HasOne(d => d.IdEstadoDemanNavigation).WithMany(p => p.Demanda)
                .HasForeignKey(d => d.IdEstadoDeman)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_demanda_estado_Demanda");

            entity.HasOne(d => d.IdPuntoAcopioNavigation).WithMany(p => p.Demanda)
                .HasForeignKey(d => d.IdPuntoAcopio)
                .HasConstraintName("FK_demanda_punto_Acopio");

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

            entity.Property(e => e.IdDepartamento).HasColumnName("id_Departamento");
            entity.Property(e => e.Departamento1)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("departamento");
        });

        modelBuilder.Entity<EntidadFinanciera>(entity =>
        {
            entity.HasKey(e => e.IdEntiFinanc);

            entity.ToTable("entidad_Financiera");

            entity.Property(e => e.IdEntiFinanc).HasColumnName("id_Enti_Financ");
            entity.Property(e => e.EntiFinanc)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("enti_Financ");
        });

        modelBuilder.Entity<EstadoDemandum>(entity =>
        {
            entity.HasKey(e => e.IdEstadoDeman);

            entity.ToTable("estado_Demanda");

            entity.Property(e => e.IdEstadoDeman).HasColumnName("id_Estado_Deman");
            entity.Property(e => e.Estado)
                .HasMaxLength(15)
                .IsUnicode(false)
                .HasColumnName("estado");
        });

        modelBuilder.Entity<LogSistema>(entity =>
        {
            entity.HasKey(e => e.IdLog);

            entity.ToTable("log_Sistema");

            entity.HasIndex(e => e.Fecha, "IX_log_Sistema_fecha").IsDescending();

            entity.Property(e => e.IdLog).HasColumnName("id_Log");
            entity.Property(e => e.Fecha)
                .HasColumnType("datetime")
                .HasColumnName("fecha");
            entity.Property(e => e.IdUsuario).HasColumnName("id_Usuario");
            entity.Property(e => e.Mensaje)
                .HasMaxLength(1000)
                .HasColumnName("mensaje");
            entity.Property(e => e.Nivel)
                .HasMaxLength(10)
                .HasColumnName("nivel");
            entity.Property(e => e.Origen)
                .HasMaxLength(60)
                .HasColumnName("origen");

            entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.LogSistemas)
                .HasForeignKey(d => d.IdUsuario)
                .HasConstraintName("FK_log_Sistema_usuario");
        });

        modelBuilder.Entity<Municipio>(entity =>
        {
            entity.HasKey(e => e.IdMunicipio);

            entity.ToTable("municipio");

            entity.Property(e => e.IdMunicipio).HasColumnName("id_Municipio");
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

        modelBuilder.Entity<Notificacion>(entity =>
        {
            entity.HasKey(e => e.IdNotificacion);

            entity.ToTable("notificacion");

            entity.HasIndex(e => new { e.IdUsuario, e.Leida, e.Fecha }, "IX_notificacion_usuario").IsDescending(false, false, true);

            entity.Property(e => e.IdNotificacion).HasColumnName("id_Notificacion");
            entity.Property(e => e.Fecha)
                .HasColumnType("datetime")
                .HasColumnName("fecha");
            entity.Property(e => e.IdDemanda).HasColumnName("id_Demanda");
            entity.Property(e => e.IdReporte).HasColumnName("id_Reporte");
            entity.Property(e => e.IdUsuario).HasColumnName("id_Usuario");
            entity.Property(e => e.Leida).HasColumnName("leida");
            entity.Property(e => e.Mensaje)
                .HasMaxLength(300)
                .HasColumnName("mensaje");
            entity.Property(e => e.PopupMostrado).HasColumnName("popup_Mostrado");
            entity.Property(e => e.Tipo)
                .HasMaxLength(30)
                .HasColumnName("tipo");

            entity.HasOne(d => d.IdDemandaNavigation).WithMany(p => p.Notificacions)
                .HasForeignKey(d => d.IdDemanda)
                .HasConstraintName("FK_notificacion_demanda");

            entity.HasOne(d => d.IdReporteNavigation).WithMany(p => p.Notificacions)
                .HasForeignKey(d => d.IdReporte)
                .HasConstraintName("FK_notificacion_reporte_Transportador");

            entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.Notificacions)
                .HasForeignKey(d => d.IdUsuario)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_notificacion_usuario");
        });

        modelBuilder.Entity<Observacion>(entity =>
        {
            entity.HasKey(e => e.IdObservacion);

            entity.ToTable("observacion");

            entity.Property(e => e.IdObservacion).HasColumnName("id_Observacion");
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

            entity.Property(e => e.IdPago).HasColumnName("id_Pago");
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

            entity.Property(e => e.IdConsignacionTrans).HasColumnName("id_Consignacion_Trans");
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

            entity.Property(e => e.IdPerfilPago).HasColumnName("id_Perfil_Pago");
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

            entity.Property(e => e.IdPrecioEstimado).HasColumnName("id_Precio_Estimado");
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

            entity.Property(e => e.IdProducto).HasColumnName("id_Producto");
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

        modelBuilder.Entity<PuntoAcopio>(entity =>
        {
            entity.HasKey(e => e.IdPuntoAcopio);

            entity.ToTable("punto_Acopio");

            entity.HasIndex(e => new { e.Latitud, e.Longitud }, "IX_punto_acopio_coordenadas");

            entity.Property(e => e.IdPuntoAcopio).HasColumnName("id_Punto_Acopio");
            entity.Property(e => e.AccesoVehicular).HasColumnName("acceso_Vehicular");
            entity.Property(e => e.Capacidad)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("capacidad");
            entity.Property(e => e.Direccion)
                .HasMaxLength(150)
                .IsUnicode(false)
                .HasColumnName("direccion");
            entity.Property(e => e.FechaRegistro)
                .HasColumnType("datetime")
                .HasColumnName("fecha_Registro");
            entity.Property(e => e.HorarioAtencion)
                .HasMaxLength(80)
                .IsUnicode(false)
                .HasColumnName("horario_Atencion");
            entity.Property(e => e.IdMunicipio).HasColumnName("id_Municipio");
            entity.Property(e => e.Latitud)
                .HasColumnType("decimal(10, 7)")
                .HasColumnName("latitud");
            entity.Property(e => e.Longitud)
                .HasColumnType("decimal(10, 7)")
                .HasColumnName("longitud");
            entity.Property(e => e.NombrePunto)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("nombre_Punto");
            entity.Property(e => e.Telefono)
                .HasMaxLength(15)
                .IsUnicode(false)
                .HasColumnName("telefono");

            entity.HasOne(d => d.IdMunicipioNavigation).WithMany(p => p.PuntoAcopios)
                .HasForeignKey(d => d.IdMunicipio)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_punto_Acopio_municipio");
        });

        modelBuilder.Entity<ReporteTransportador>(entity =>
        {
            entity.HasKey(e => e.IdReporte);

            entity.ToTable("reporte_Transportador");

            entity.Property(e => e.IdReporte).HasColumnName("id_Reporte");
            entity.Property(e => e.FechaActual)
                .HasColumnType("datetime")
                .HasColumnName("fecha_Actual");
            entity.Property(e => e.FechaFinal)
                .HasColumnType("datetime")
                .HasColumnName("fecha_Final");
            entity.Property(e => e.FechaInicio)
                .HasColumnType("datetime")
                .HasColumnName("fecha_Inicio");
            entity.Property(e => e.Hito)
                .HasMaxLength(30)
                .HasColumnName("hito");
            entity.Property(e => e.IdDemanda).HasColumnName("id_Demanda");
            entity.Property(e => e.IdUsuario).HasColumnName("id_Usuario");
            entity.Property(e => e.PuntoDestino)
                .HasMaxLength(100)
                .HasColumnName("punto_Destino");
            entity.Property(e => e.PuntoOrigen)
                .HasMaxLength(100)
                .HasColumnName("punto_Origen");
            entity.Property(e => e.Region)
                .HasMaxLength(40)
                .HasColumnName("region");
            entity.Property(e => e.UbicacionActual)
                .HasMaxLength(150)
                .HasColumnName("ubicacion_Actual");

            entity.HasOne(d => d.IdDemandaNavigation).WithMany(p => p.ReporteTransportadors)
                .HasForeignKey(d => d.IdDemanda)
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

            entity.Property(e => e.IdRol).HasColumnName("id_Rol");
            entity.Property(e => e.TipoRol)
                .HasMaxLength(15)
                .IsUnicode(false)
                .HasColumnName("tipo_Rol");
        });

        modelBuilder.Entity<TipoCuentum>(entity =>
        {
            entity.HasKey(e => e.IdTipoCuenta);

            entity.ToTable("tipo_Cuenta");

            entity.Property(e => e.IdTipoCuenta).HasColumnName("id_Tipo_Cuenta");
            entity.Property(e => e.TipoCuenta)
                .HasMaxLength(15)
                .IsUnicode(false)
                .HasColumnName("tipo_Cuenta");
        });

        modelBuilder.Entity<TipoPago>(entity =>
        {
            entity.HasKey(e => e.IdTipoPago);

            entity.ToTable("tipo_Pago");

            entity.Property(e => e.IdTipoPago).HasColumnName("id_Tipo_Pago");
            entity.Property(e => e.TipoPago1)
                .HasMaxLength(12)
                .IsUnicode(false)
                .HasColumnName("tipo_Pago");
        });

        modelBuilder.Entity<TipoProducto>(entity =>
        {
            entity.HasKey(e => e.IdTipoProducto).HasName("PK_producto");

            entity.ToTable("tipo_Producto");

            entity.Property(e => e.IdTipoProducto).HasColumnName("id_Tipo_Producto");
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

            entity.HasMany(d => d.IdPuntoAcopios).WithMany(p => p.IdUsuarios)
                .UsingEntity<Dictionary<string, object>>(
                    "UsuarioPuntoAcopio",
                    r => r.HasOne<PuntoAcopio>().WithMany()
                        .HasForeignKey("IdPuntoAcopio")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_usuario_Punto_Acopio_punto_Acopio"),
                    l => l.HasOne<Usuario>().WithMany()
                        .HasForeignKey("IdUsuario")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_usuario_Punto_Acopio_usuario"),
                    j =>
                    {
                        j.HasKey("IdUsuario", "IdPuntoAcopio");
                        j.ToTable("usuario_Punto_Acopio");
                        j.IndexerProperty<int>("IdUsuario").HasColumnName("id_Usuario");
                        j.IndexerProperty<int>("IdPuntoAcopio").HasColumnName("id_Punto_Acopio");
                    });
        });

        modelBuilder.Entity<Variedad>(entity =>
        {
            entity.HasKey(e => e.IdVariedad);

            entity.ToTable("variedad");

            entity.Property(e => e.IdVariedad).HasColumnName("id_Variedad");
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
