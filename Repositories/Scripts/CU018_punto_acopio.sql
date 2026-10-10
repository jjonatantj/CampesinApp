/* ============================================================================
   CampesinApp - Esquema para Caso de Uso CU-018: Registro de Puntos de Acopio
   Base de datos: CAMPESINAPP_3 (SQL Server)
   ============================================================================ */

IF OBJECT_ID(N'dbo.punto_Acopio', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.punto_Acopio (
        id_Punto_Acopio   INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_punto_Acopio PRIMARY KEY,
        id_Usuario        INT            NOT NULL,
        nombre_Punto      NVARCHAR(100)  NOT NULL,
        id_Departamento   INT            NOT NULL,
        id_Municipio      INT            NOT NULL,
        direccion         NVARCHAR(150)  NOT NULL,
        latitud           DECIMAL(10, 7) NOT NULL,
        longitud          DECIMAL(10, 7) NOT NULL,
        capacidad         DECIMAL(10, 2) NOT NULL,
        acceso_Vehicular  BIT            NOT NULL CONSTRAINT DF_punto_acopio_acceso DEFAULT 1,
        fecha_Registro    DATETIME       NOT NULL CONSTRAINT DF_punto_acopio_fecha DEFAULT GETDATE(),

        CONSTRAINT FK_punto_acopio_usuario FOREIGN KEY (id_Usuario) REFERENCES dbo.usuario (id_Usuario),
        CONSTRAINT FK_punto_acopio_departamento FOREIGN KEY (id_Departamento) REFERENCES dbo.departamento (id_Departamento),
        CONSTRAINT FK_punto_acopio_municipio FOREIGN KEY (id_Municipio) REFERENCES dbo.municipio (id_Municipio)
    );

    CREATE NONCLUSTERED INDEX IX_punto_acopio_direccion ON dbo.punto_Acopio (direccion);
    CREATE NONCLUSTERED INDEX IX_punto_acopio_coordenadas ON dbo.punto_Acopio (latitud, longitud);
END
GO

