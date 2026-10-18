/* ============================================================================
   OPCIONAL - Alinear usuario.contrasena_Usu con el modelo compartido (Campesinapp3Context: 255 caracteres)

   El inicio de sesion y el registro guardan la contrasena cifrada (AES + Base64, 24 caracteres o mas), pero en las
   bases creadas con el esquema original la columna es VARCHAR(10) y no cabe. Solo ensancha la columna: no se pierde
   ningun dato. Ejecutelo unicamente si su base todavia tiene VARCHAR(10); NO forma parte de los scripts de CU-021 a CU-032.
   ============================================================================ */
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.usuario') AND name = N'contrasena_Usu' AND max_length < 255)
    ALTER TABLE dbo.usuario ALTER COLUMN contrasena_Usu VARCHAR(255) NOT NULL;
GO
