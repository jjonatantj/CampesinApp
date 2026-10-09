/* ============================================================================
   CampesinApp - Esquema para los casos de uso CU-021 a CU-032
   Base de datos: CAMPESINAPP_3 (SQL Server)

   - Solo CREA tablas nuevas; no modifica ni elimina ninguna tabla existente.
   - Es idempotente: se puede ejecutar varias veces.
   - Las tablas existentes que usa (usuario, demanda, aporte_Camp, tipo_Producto,
     variedad, dato_Dane...) deben estar creadas previamente.
   ============================================================================ */

-- Compatibilidad con CU-004 / CU-003: el inicio de sesion y el registro guardan la contrasena CIFRADA (AES + Base64,
-- 24 caracteres o mas), pero el esquema original definia usuario.contrasena_Usu como VARCHAR(10).
-- Solo se ensancha la columna (no se pierde ningun dato) y unicamente si aun es mas corta de lo necesario.
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.usuario') AND name = N'contrasena_Usu' AND max_length < 100)
    ALTER TABLE dbo.usuario ALTER COLUMN contrasena_Usu VARCHAR(100) NOT NULL;
GO

-- CU-024 / CU-023 / CU-025: servicio de transporte asignado a un pedido y su estado de envio
IF OBJECT_ID(N'dbo.viaje', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.viaje (
        id_Viaje                     INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_viaje PRIMARY KEY,
        id_Demanda                   INT           NOT NULL,
        id_Usuario_Transportador     INT           NOT NULL,
        id_Usuario_Admin             INT           NOT NULL,
        nombre_Transportador         NVARCHAR(60)  NOT NULL,
        telefono_Transportador       NVARCHAR(20)  NOT NULL,
        placa                        NVARCHAR(8)   NOT NULL,
        modelo                       NVARCHAR(20)  NOT NULL,
        capacidad                    INT           NOT NULL,
        estado_Envio                 NVARCHAR(40)  NOT NULL,
        fecha_Asignacion             DATETIME      NOT NULL,
        llegada_Notificada           BIT           NOT NULL CONSTRAINT DF_viaje_llegada DEFAULT 0,
        fecha_Llegada                DATETIME      NULL,
        transportador_Confirma       BIT           NOT NULL CONSTRAINT DF_viaje_transp_conf DEFAULT 0,
        comprador_Confirma           BIT           NOT NULL CONSTRAINT DF_viaje_comp_conf DEFAULT 0,
        comprador_Con_Observaciones  BIT           NOT NULL CONSTRAINT DF_viaje_comp_obs DEFAULT 0,
        observacion_Comprador        NVARCHAR(500) NULL,
        imagen_Evidencia             NVARCHAR(100) NULL,
        visible_Transportador        BIT           NOT NULL CONSTRAINT DF_viaje_visible DEFAULT 1,
        fecha_Entrega                DATETIME      NULL,
        CONSTRAINT UQ_viaje_demanda UNIQUE (id_Demanda),
        CONSTRAINT FK_viaje_demanda FOREIGN KEY (id_Demanda) REFERENCES dbo.demanda (id_Demanda),
        CONSTRAINT FK_viaje_usuario_transportador FOREIGN KEY (id_Usuario_Transportador) REFERENCES dbo.usuario (id_Usuario)
    );
END
GO

-- CU-021: reportes manuales de ubicacion del transportador
IF OBJECT_ID(N'dbo.reporte_Ubicacion', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.reporte_Ubicacion (
        id_Reporte_Ubicacion INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_reporte_Ubicacion PRIMARY KEY,
        id_Viaje             INT           NOT NULL,
        id_Demanda           INT           NOT NULL,
        id_Usuario           INT           NOT NULL,
        fecha_Inicial        DATE          NOT NULL,
        fecha_Final          DATE          NOT NULL,
        region               NVARCHAR(40)  NOT NULL,
        punto_Origen         NVARCHAR(100) NOT NULL,
        punto_Destino        NVARCHAR(100) NOT NULL,
        lugar                NVARCHAR(100) NOT NULL,
        hito                 NVARCHAR(30)  NOT NULL,
        fecha_Hora           DATETIME      NOT NULL,
        CONSTRAINT FK_reporte_Ubicacion_viaje FOREIGN KEY (id_Viaje) REFERENCES dbo.viaje (id_Viaje),
        CONSTRAINT FK_reporte_Ubicacion_usuario FOREIGN KEY (id_Usuario) REFERENCES dbo.usuario (id_Usuario)
    );
    CREATE INDEX IX_reporte_Ubicacion_demanda ON dbo.reporte_Ubicacion (id_Demanda, fecha_Hora DESC);
END
GO

-- CU-022: alertas de ubicacion que el campesino / comprador guarda con "Guardar reporte"
IF OBJECT_ID(N'dbo.alerta_Guardada', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.alerta_Guardada (
        id_Alerta_Guardada   INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_alerta_Guardada PRIMARY KEY,
        id_Reporte_Ubicacion INT      NOT NULL,
        id_Usuario           INT      NOT NULL,
        fecha_Guardado       DATETIME NOT NULL,
        CONSTRAINT UQ_alerta_Guardada UNIQUE (id_Reporte_Ubicacion, id_Usuario),
        CONSTRAINT FK_alerta_Guardada_reporte_Ubicacion FOREIGN KEY (id_Reporte_Ubicacion) REFERENCES dbo.reporte_Ubicacion (id_Reporte_Ubicacion),
        CONSTRAINT FK_alerta_Guardada_usuario FOREIGN KEY (id_Usuario) REFERENCES dbo.usuario (id_Usuario)
    );
END
GO

-- CU-021 / CU-025: notificaciones (nueva ubicacion, llegada del pedido, entrega completada, novedades)
IF OBJECT_ID(N'dbo.notificacion', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.notificacion (
        id_Notificacion      INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_notificacion PRIMARY KEY,
        id_Usuario           INT           NOT NULL,
        id_Demanda           INT           NULL,
        id_Reporte_Ubicacion INT           NULL,
        tipo                 NVARCHAR(30)  NOT NULL,
        mensaje              NVARCHAR(300) NOT NULL,
        leida                BIT           NOT NULL CONSTRAINT DF_notificacion_leida DEFAULT 0,
        popup_Mostrado       BIT           NOT NULL CONSTRAINT DF_notificacion_popup DEFAULT 0,
        fecha                DATETIME      NOT NULL,
        CONSTRAINT FK_notificacion_usuario FOREIGN KEY (id_Usuario) REFERENCES dbo.usuario (id_Usuario)
    );
    CREATE INDEX IX_notificacion_usuario ON dbo.notificacion (id_Usuario, leida, fecha DESC);
END
GO

-- CU-026: calificaciones y comentarios entre participantes de un pedido
IF OBJECT_ID(N'dbo.calificacion', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.calificacion (
        id_Calificacion      INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_calificacion PRIMARY KEY,
        id_Demanda           INT           NOT NULL,
        id_Usuario_Evalua    INT           NOT NULL,
        id_Usuario_Evaluado  INT           NOT NULL,
        estrellas            INT           NOT NULL CONSTRAINT CK_calificacion_estrellas CHECK (estrellas BETWEEN 1 AND 5),
        comentario           NVARCHAR(500) NOT NULL,
        fecha                DATETIME      NOT NULL,
        CONSTRAINT UQ_calificacion UNIQUE (id_Demanda, id_Usuario_Evalua, id_Usuario_Evaluado),
        CONSTRAINT FK_calificacion_demanda FOREIGN KEY (id_Demanda) REFERENCES dbo.demanda (id_Demanda),
        CONSTRAINT FK_calificacion_usuario_evalua FOREIGN KEY (id_Usuario_Evalua) REFERENCES dbo.usuario (id_Usuario),
        CONSTRAINT FK_calificacion_usuario_evaluado FOREIGN KEY (id_Usuario_Evaluado) REFERENCES dbo.usuario (id_Usuario)
    );
END
GO

-- CU-030: bitacora de eventos del sistema
IF OBJECT_ID(N'dbo.log_Sistema', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.log_Sistema (
        id_Log     INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_log_Sistema PRIMARY KEY,
        fecha      DATETIME       NOT NULL,
        nivel      NVARCHAR(10)   NOT NULL,
        id_Usuario INT            NULL,
        origen     NVARCHAR(60)   NOT NULL,
        mensaje    NVARCHAR(1000) NOT NULL
    );
    CREATE INDEX IX_log_Sistema_fecha ON dbo.log_Sistema (fecha DESC);
END
GO
