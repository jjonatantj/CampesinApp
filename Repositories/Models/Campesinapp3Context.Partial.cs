using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace CampesinApp.Repositories.Models;

// Entidades de los casos de uso CU-021 a CU-030. Va en un archivo aparte para que un nuevo
// scaffold de la base de datos no lo sobrescriba. El script SQL está en Database/CU021-030_esquema.sql.
public partial class Campesinapp3Context
{
    public virtual DbSet<Viaje> Viajes { get; set; }

    public virtual DbSet<ReporteUbicacion> ReporteUbicacions { get; set; }

    public virtual DbSet<AlertaGuardada> AlertaGuardadas { get; set; }

    public virtual DbSet<Notificacion> Notificacions { get; set; }

    public virtual DbSet<Calificacion> Calificacions { get; set; }

    public virtual DbSet<LogSistema> LogSistemas { get; set; }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Viaje>(entity =>
        {
            entity.HasKey(e => e.IdViaje);

            entity.ToTable("viaje");

            entity.HasIndex(e => e.IdDemanda, "UQ_viaje_demanda").IsUnique();

            entity.Property(e => e.IdViaje).HasColumnName("id_Viaje");
            entity.Property(e => e.IdDemanda).HasColumnName("id_Demanda");
            entity.Property(e => e.IdUsuarioTransportador).HasColumnName("id_Usuario_Transportador");
            entity.Property(e => e.IdUsuarioAdmin).HasColumnName("id_Usuario_Admin");
            entity.Property(e => e.NombreTransportador).HasMaxLength(60).HasColumnName("nombre_Transportador");
            entity.Property(e => e.TelefonoTransportador).HasMaxLength(20).HasColumnName("telefono_Transportador");
            entity.Property(e => e.Placa).HasMaxLength(8).HasColumnName("placa");
            entity.Property(e => e.Modelo).HasMaxLength(20).HasColumnName("modelo");
            entity.Property(e => e.Capacidad).HasColumnName("capacidad");
            entity.Property(e => e.EstadoEnvio).HasMaxLength(40).HasColumnName("estado_Envio");
            entity.Property(e => e.FechaAsignacion).HasColumnType("datetime").HasColumnName("fecha_Asignacion");
            entity.Property(e => e.LlegadaNotificada).HasColumnName("llegada_Notificada");
            entity.Property(e => e.FechaLlegada).HasColumnType("datetime").HasColumnName("fecha_Llegada");
            entity.Property(e => e.TransportadorConfirma).HasColumnName("transportador_Confirma");
            entity.Property(e => e.CompradorConfirma).HasColumnName("comprador_Confirma");
            entity.Property(e => e.CompradorConObservaciones).HasColumnName("comprador_Con_Observaciones");
            entity.Property(e => e.ObservacionComprador).HasMaxLength(500).HasColumnName("observacion_Comprador");
            entity.Property(e => e.ImagenEvidencia).HasMaxLength(100).HasColumnName("imagen_Evidencia");
            entity.Property(e => e.VisibleTransportador).HasColumnName("visible_Transportador");
            entity.Property(e => e.FechaEntrega).HasColumnType("datetime").HasColumnName("fecha_Entrega");

            entity.HasOne(d => d.IdDemandaNavigation).WithMany()
                .HasForeignKey(d => d.IdDemanda)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_viaje_demanda");

            entity.HasOne(d => d.IdUsuarioTransportadorNavigation).WithMany()
                .HasForeignKey(d => d.IdUsuarioTransportador)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_viaje_usuario_transportador");
        });

        modelBuilder.Entity<ReporteUbicacion>(entity =>
        {
            entity.HasKey(e => e.IdReporteUbicacion);

            entity.ToTable("reporte_Ubicacion");

            entity.Property(e => e.IdReporteUbicacion).HasColumnName("id_Reporte_Ubicacion");
            entity.Property(e => e.IdViaje).HasColumnName("id_Viaje");
            entity.Property(e => e.IdDemanda).HasColumnName("id_Demanda");
            entity.Property(e => e.IdUsuario).HasColumnName("id_Usuario");
            entity.Property(e => e.FechaInicial).HasColumnType("date").HasColumnName("fecha_Inicial");
            entity.Property(e => e.FechaFinal).HasColumnType("date").HasColumnName("fecha_Final");
            entity.Property(e => e.Region).HasMaxLength(40).HasColumnName("region");
            entity.Property(e => e.PuntoOrigen).HasMaxLength(100).HasColumnName("punto_Origen");
            entity.Property(e => e.PuntoDestino).HasMaxLength(100).HasColumnName("punto_Destino");
            entity.Property(e => e.Lugar).HasMaxLength(100).HasColumnName("lugar");
            entity.Property(e => e.Hito).HasMaxLength(30).HasColumnName("hito");
            entity.Property(e => e.FechaHora).HasColumnType("datetime").HasColumnName("fecha_Hora");

            entity.HasOne(d => d.IdViajeNavigation).WithMany(p => p.ReporteUbicacions)
                .HasForeignKey(d => d.IdViaje)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_reporte_Ubicacion_viaje");

            entity.HasOne(d => d.IdUsuarioNavigation).WithMany()
                .HasForeignKey(d => d.IdUsuario)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_reporte_Ubicacion_usuario");
        });

        modelBuilder.Entity<AlertaGuardada>(entity =>
        {
            entity.HasKey(e => e.IdAlertaGuardada);

            entity.ToTable("alerta_Guardada");

            entity.HasIndex(e => new { e.IdReporteUbicacion, e.IdUsuario }, "UQ_alerta_Guardada").IsUnique();

            entity.Property(e => e.IdAlertaGuardada).HasColumnName("id_Alerta_Guardada");
            entity.Property(e => e.IdReporteUbicacion).HasColumnName("id_Reporte_Ubicacion");
            entity.Property(e => e.IdUsuario).HasColumnName("id_Usuario");
            entity.Property(e => e.FechaGuardado).HasColumnType("datetime").HasColumnName("fecha_Guardado");

            entity.HasOne(d => d.IdReporteUbicacionNavigation).WithMany(p => p.AlertaGuardadas)
                .HasForeignKey(d => d.IdReporteUbicacion)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_alerta_Guardada_reporte_Ubicacion");

            entity.HasOne(d => d.IdUsuarioNavigation).WithMany()
                .HasForeignKey(d => d.IdUsuario)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_alerta_Guardada_usuario");
        });

        modelBuilder.Entity<Notificacion>(entity =>
        {
            entity.HasKey(e => e.IdNotificacion);

            entity.ToTable("notificacion");

            entity.Property(e => e.IdNotificacion).HasColumnName("id_Notificacion");
            entity.Property(e => e.IdUsuario).HasColumnName("id_Usuario");
            entity.Property(e => e.IdDemanda).HasColumnName("id_Demanda");
            entity.Property(e => e.IdReporteUbicacion).HasColumnName("id_Reporte_Ubicacion");
            entity.Property(e => e.Tipo).HasMaxLength(30).HasColumnName("tipo");
            entity.Property(e => e.Mensaje).HasMaxLength(300).HasColumnName("mensaje");
            entity.Property(e => e.Leida).HasColumnName("leida");
            entity.Property(e => e.PopupMostrado).HasColumnName("popup_Mostrado");
            entity.Property(e => e.Fecha).HasColumnType("datetime").HasColumnName("fecha");

            entity.HasOne(d => d.IdUsuarioNavigation).WithMany()
                .HasForeignKey(d => d.IdUsuario)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_notificacion_usuario");
        });

        modelBuilder.Entity<Calificacion>(entity =>
        {
            entity.HasKey(e => e.IdCalificacion);

            entity.ToTable("calificacion");

            entity.HasIndex(e => new { e.IdDemanda, e.IdUsuarioEvalua, e.IdUsuarioEvaluado }, "UQ_calificacion").IsUnique();

            entity.Property(e => e.IdCalificacion).HasColumnName("id_Calificacion");
            entity.Property(e => e.IdDemanda).HasColumnName("id_Demanda");
            entity.Property(e => e.IdUsuarioEvalua).HasColumnName("id_Usuario_Evalua");
            entity.Property(e => e.IdUsuarioEvaluado).HasColumnName("id_Usuario_Evaluado");
            entity.Property(e => e.Estrellas).HasColumnName("estrellas");
            entity.Property(e => e.Comentario).HasMaxLength(500).HasColumnName("comentario");
            entity.Property(e => e.Fecha).HasColumnType("datetime").HasColumnName("fecha");

            entity.HasOne(d => d.IdDemandaNavigation).WithMany()
                .HasForeignKey(d => d.IdDemanda)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_calificacion_demanda");

            entity.HasOne(d => d.IdUsuarioEvaluaNavigation).WithMany()
                .HasForeignKey(d => d.IdUsuarioEvalua)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_calificacion_usuario_evalua");

            entity.HasOne(d => d.IdUsuarioEvaluadoNavigation).WithMany()
                .HasForeignKey(d => d.IdUsuarioEvaluado)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_calificacion_usuario_evaluado");
        });

        modelBuilder.Entity<LogSistema>(entity =>
        {
            entity.HasKey(e => e.IdLog);

            entity.ToTable("log_Sistema");

            entity.Property(e => e.IdLog).HasColumnName("id_Log");
            entity.Property(e => e.Fecha).HasColumnType("datetime").HasColumnName("fecha");
            entity.Property(e => e.Nivel).HasMaxLength(10).HasColumnName("nivel");
            entity.Property(e => e.IdUsuario).HasColumnName("id_Usuario");
            entity.Property(e => e.Origen).HasMaxLength(60).HasColumnName("origen");
            entity.Property(e => e.Mensaje).HasMaxLength(1000).HasColumnName("mensaje");
        });

    }
}
