using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace CampesinApp.Repositories.Models;

// Entidades de las tablas alerta_Guardada, notificacion y log_Sistema (CU-021 a CU-032). Van en un archivo aparte para
// que un nuevo scaffold de la base de datos no lo sobrescriba. Las demás tablas que usan esos casos de uso ya existían
// (demanda, camion, reporte_Transportador, observacion...) y se mapean en Campesinapp3Context.cs.
public partial class Campesinapp3Context
{
    public virtual DbSet<AlertaGuardada> AlertaGuardadas { get; set; }

    public virtual DbSet<Notificacion> Notificacions { get; set; }

    public virtual DbSet<LogSistema> LogSistemas { get; set; }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AlertaGuardada>(entity =>
        {
            entity.HasKey(e => e.IdAlertaGuardada);

            entity.ToTable("alerta_Guardada");

            entity.HasIndex(e => new { e.IdReporte, e.IdUsuario }, "UQ_alerta_Guardada").IsUnique();

            entity.Property(e => e.IdAlertaGuardada).HasColumnName("id_Alerta_Guardada");
            entity.Property(e => e.IdReporte).HasColumnName("id_Reporte");
            entity.Property(e => e.IdUsuario).HasColumnName("id_Usuario");
            entity.Property(e => e.FechaGuardado).HasColumnType("datetime").HasColumnName("fecha_Guardado");

            entity.HasOne(d => d.IdReporteNavigation).WithMany(p => p.AlertaGuardadas)
                .HasForeignKey(d => d.IdReporte)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_alerta_Guardada_reporte_Transportador");

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
            entity.Property(e => e.IdReporte).HasColumnName("id_Reporte");
            entity.Property(e => e.Tipo).HasMaxLength(60).HasColumnName("tipo");
            entity.Property(e => e.Mensaje).HasMaxLength(600).HasColumnName("mensaje");
            entity.Property(e => e.Leida).HasColumnName("leida");
            entity.Property(e => e.PopupMostrado).HasColumnName("popup_Mostrado");
            entity.Property(e => e.Fecha).HasColumnType("datetime").HasColumnName("fecha");

            entity.HasOne(d => d.IdUsuarioNavigation).WithMany()
                .HasForeignKey(d => d.IdUsuario)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_notificacion_usuario");

            entity.HasOne<Demandum>().WithMany()
                .HasForeignKey(d => d.IdDemanda)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_notificacion_demanda");

            entity.HasOne<ReporteTransportador>().WithMany()
                .HasForeignKey(d => d.IdReporte)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_notificacion_reporte_Transportador");
        });

        modelBuilder.Entity<LogSistema>(entity =>
        {
            entity.HasKey(e => e.IdLog);

            entity.ToTable("log_Sistema");

            entity.Property(e => e.IdLog).HasColumnName("id_Log");
            entity.Property(e => e.Fecha).HasColumnType("datetime").HasColumnName("fecha");
            entity.Property(e => e.Nivel).HasMaxLength(20).HasColumnName("nivel");
            entity.Property(e => e.IdUsuario).HasColumnName("id_Usuario");
            entity.Property(e => e.Origen).HasMaxLength(120).HasColumnName("origen");
            entity.Property(e => e.Mensaje).HasMaxLength(2000).HasColumnName("mensaje");
        });
    }
}
