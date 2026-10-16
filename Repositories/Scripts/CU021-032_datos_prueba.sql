/* ============================================================================
   CampesinApp - Datos de PRUEBA para los casos de uso CU-021 a CU-032
   SOLO para una base de desarrollo / de pruebas LOCAL. No ejecutar en produccion.

   - Solo carga datos si la base esta VACIA (sin usuarios ni pedidos); si ya hay datos reales no hace nada.
   - Requiere haber ejecutado antes CU021-032_esquema.sql.
   - Las contrasenas se guardan con el mismo cifrado AES que usa el inicio de sesion (EncriptacionService).

   Inicio de sesion (CU-004): se entra con la CEDULA y la contrasena Demo1234.
     100000001  Ana Admin            Administrador
     100000002  Luis Campesino       Campesino
     100000003  Marta Campesina      Campesino
     100000004  Carlos Comprador     Comprador
     100000005  Laura Compradora     Comprador
     100000006  Pedro Transporta     Transportador
     100000007  Julian Camionero     Transportador

   Pedidos:  PED-0001 entregado y recibido (con calificaciones)  |  PED-0002 en transito, acaba de llegar
             PED-0003 asignado, sin reportes  |  PED-0004 sin transporte
             PED-0005 entregado con observaciones  |  PED-0006 en transito, ultimo reporte hace 3 horas
   ============================================================================ */

SET NOCOUNT ON;

IF EXISTS (SELECT 1 FROM dbo.usuario) OR EXISTS (SELECT 1 FROM dbo.demanda)
BEGIN
    PRINT 'La base ya tiene usuarios o pedidos: no se cargan datos de prueba.';
    RETURN;
END

DECLARE @ahora DATETIME = GETDATE();
DECLARE @clave VARCHAR(100) = 'IqKo5yqhxI5mMofRZmmb7g==';   -- "Demo1234" cifrada con EncriptacionService

-- Catalogos (todas las tablas de la base tienen id IDENTITY: se fijan los ids con IDENTITY_INSERT) -----------------------
SET IDENTITY_INSERT dbo.rol ON;
INSERT dbo.rol (id_Rol, tipo_Rol) SELECT v.id, v.n FROM (VALUES (1, 'Administrador'), (2, 'Campesino'), (3, 'Comprador'), (4, 'Transportador')) v (id, n) WHERE NOT EXISTS (SELECT 1 FROM dbo.rol WHERE id_Rol = v.id);
SET IDENTITY_INSERT dbo.rol OFF;

SET IDENTITY_INSERT dbo.departamento ON;
INSERT dbo.departamento (id_Departamento, departamento) SELECT 1, 'Cundinamarca' WHERE NOT EXISTS (SELECT 1 FROM dbo.departamento WHERE id_Departamento = 1);
SET IDENTITY_INSERT dbo.departamento OFF;

SET IDENTITY_INSERT dbo.municipio ON;
INSERT dbo.municipio (id_Municipio, id_Departamento, municipio)
SELECT v.id, 1, v.n FROM (VALUES (1, 'Bogota'), (2, 'Fusagasuga'), (3, 'Chia')) v (id, n) WHERE NOT EXISTS (SELECT 1 FROM dbo.municipio WHERE id_Municipio = v.id);
SET IDENTITY_INSERT dbo.municipio OFF;

SET IDENTITY_INSERT dbo.tipo_Producto ON;
INSERT dbo.tipo_Producto (id_Tipo_Producto, producto)
SELECT v.id, v.n FROM (VALUES (1, 'Papa'), (2, 'Tomate'), (3, 'Cebolla'), (4, 'Zanahoria')) v (id, n) WHERE NOT EXISTS (SELECT 1 FROM dbo.tipo_Producto WHERE id_Tipo_Producto = v.id);
SET IDENTITY_INSERT dbo.tipo_Producto OFF;

SET IDENTITY_INSERT dbo.variedad ON;
INSERT dbo.variedad (id_Variedad, id_Tipo_Producto, variedad_Prod)
SELECT v.id, v.tipo, v.n FROM (VALUES (1, 1, 'Pastusa'), (2, 2, 'Chonto'), (3, 3, 'Larga'), (4, 4, 'Comun')) v (id, tipo, n) WHERE NOT EXISTS (SELECT 1 FROM dbo.variedad WHERE id_Variedad = v.id);
SET IDENTITY_INSERT dbo.variedad OFF;

SET IDENTITY_INSERT dbo.estado_Demanda ON;
INSERT dbo.estado_Demanda (id_Estado_Deman, estado) SELECT 1, 'Activa' WHERE NOT EXISTS (SELECT 1 FROM dbo.estado_Demanda WHERE id_Estado_Deman = 1);
SET IDENTITY_INSERT dbo.estado_Demanda OFF;

-- Usuarios (usuario.id_Usuario es IDENTITY) ------------------------------------------------------------------
SET IDENTITY_INSERT dbo.usuario ON;
INSERT dbo.usuario (id_Usuario, id_Rol, id_Municipio, correo_Usu, contrasena_Usu, cedula_Usu, nombre_com, apellido_com, telelfono_Usu, estado, imagen) VALUES
    (1, 1, 1, 'caroltatianamanrique66@gmail.com', @clave, 100000001, 'Ana',    'Admin',      300000001, 1, ''),
    (2, 2, 2, 'luis@campesinapp.test',   @clave, 100000002, 'Luis',   'Campesino',  300000002, 1, ''),
    (3, 2, 2, 'marta@campesinapp.test',  @clave, 100000003, 'Marta',  'Campesina',  300000003, 1, ''),
    (4, 3, 1, 'carlos@campesinapp.test', @clave, 100000004, 'Carlos', 'Comprador',  300000004, 1, ''),
    (5, 3, 3, 'laura@campesinapp.test',  @clave, 100000005, 'Laura',  'Compradora', 300000005, 1, ''),
    (6, 4, 1, 'pedro@campesinapp.test',  @clave, 100000006, 'Pedro',  'Transporta', 300000006, 1, ''),
    (7, 4, 3, 'julian@campesinapp.test', @clave, 100000007, 'Julian', 'Camionero',  300000007, 1, '');
SET IDENTITY_INSERT dbo.usuario OFF;

-- Puntos de acopio (tabla punto_Acopio del equipo): de aqui salen las coordenadas que se dibujan en Google Maps ---------
SET IDENTITY_INSERT dbo.punto_Acopio ON;
INSERT dbo.punto_Acopio (id_Punto_Acopio, id_Municipio, nombre_Punto, direccion, telefono, horario_Atencion, latitud, longitud, capacidad, acceso_Vehicular, fecha_Registro) VALUES
    (1, 2, 'Acopio Fusagasuga Centro',   'Carrera 6 # 8-30, Fusagasuga',            '3105550101', '6:00 a. m. - 6:00 p. m.', 4.3366000, -74.3637000, 20000.00, 1, @ahora),
    (2, 1, 'Acopio Peaje Chusaca',       'Autopista Sur Km 17, Soacha',             '3105550102', '24 horas',               4.5090000, -74.2380000, 15000.00, 1, @ahora),
    (3, 1, 'Acopio Portal de Usme',      'Avenida Caracas # 130 Sur, Bogota',       '3105550103', '5:00 a. m. - 8:00 p. m.', 4.4780000, -74.1220000, 12000.00, 1, @ahora),
    (4, 1, 'Acopio Corabastos',          'Avenida Carrera 80 # 2-51, Bogota',       '3105550104', '24 horas',               4.6213000, -74.1496000, 50000.00, 1, @ahora),
    (5, 1, 'Acopio Plaza de Paloquemao', 'Carrera 25 # 19-04, Bogota',              '3105550105', '4:00 a. m. - 5:00 p. m.', 4.6120000, -74.0870000, 8000.00, 1, @ahora),
    (6, 3, 'Acopio Chia Centro',         'Carrera 11 # 11-20, Chia',                '3105550106', '6:00 a. m. - 6:00 p. m.', 4.8600000, -74.0330000, 9000.00, 1, @ahora),
    (7, 1, 'Acopio Calle 80 Bogota',     'Avenida Calle 80 # 85-10, Bogota',        '3105550107', '6:00 a. m. - 7:00 p. m.', 4.7000000, -74.0900000, 10000.00, 1, @ahora);
SET IDENTITY_INSERT dbo.punto_Acopio OFF;

INSERT dbo.usuario_Punto_Acopio (id_Usuario, id_Punto_Acopio) VALUES (2, 1), (3, 1), (2, 2), (5, 6), (4, 4);

-- Camiones registrados de los transportadores (alimentan la tarjeta "Fletes y Camiones" de la pantalla principal) ------
SET IDENTITY_INSERT dbo.camion ON;
INSERT dbo.camion (id_Camion, id_Usuario, placa_Cami, capacidad_Cami, modelo_Cami, estado_pase, estado_seguro, estado_tecno) VALUES
    (1, 6, 'ABC123', 5, 'NPR 2020',   1, 1, 1),
    (2, 7, 'QWE456', 3, 'Turbo 2019', 1, 1, 1);
SET IDENTITY_INSERT dbo.camion OFF;

-- Pedidos (demandas) y aportes de los campesinos ---------------------------------------------------------------
-- Esquema actual: demanda.id_Demanda es IDENTITY; el destino es un punto de acopio (id_Punto_Acopio) o una direccion_Inicial.
SET IDENTITY_INSERT dbo.demanda ON;
INSERT dbo.demanda (id_Demanda, id_Usuario, id_Tipo_Producto, id_Variedad, id_Camion, id_Estado_Deman, cantidad_Deman, precio_Deman, fecha_Entrega, lim_Tiem_Deman, descripcion_Deman, id_Punto_Acopio, direccion_Inicial) VALUES
    (1, 4, 1, 1, NULL, 1, 500, 1800, DATEADD(DAY, 2, @ahora), DATEADD(DAY, 1, @ahora), 'Papa pastusa para restaurante',        4,    NULL),
    (2, 4, 2, 2, NULL, 1, 300, 2500, DATEADD(DAY, 2, @ahora), DATEADD(DAY, 1, @ahora), 'Tomate chonto para plaza de mercado',  5,    NULL),
    (3, 5, 1, 1, NULL, 1, 200, 1750, DATEADD(DAY, 2, @ahora), DATEADD(DAY, 1, @ahora), 'Papa para tienda de barrio',           NULL, 'Avenida Pradilla # 12-30 Chia'),
    (4, 4, 1, 1, NULL, 1, 150, 1700, DATEADD(DAY, 2, @ahora), DATEADD(DAY, 1, @ahora), 'Papa para comedor comunitario',        NULL, 'Calle 80 # 20-15 Bogota'),
    (5, 5, 2, 2, NULL, 1, 250, 2400, DATEADD(DAY, 2, @ahora), DATEADD(DAY, 1, @ahora), 'Tomate para restaurante',              6,    NULL),
    (6, 5, 4, 4, NULL, 1, 400, 1200, DATEADD(DAY, 2, @ahora), DATEADD(DAY, 1, @ahora), 'Zanahoria para jugos',                 6,    NULL);
SET IDENTITY_INSERT dbo.demanda OFF;

SET IDENTITY_INSERT dbo.aporte_Camp ON;
INSERT dbo.aporte_Camp (id_Aporte, id_Usuario, id_Demanda, cant_Aporte, imagen) VALUES
    (1, 2, 1, 300, ''), (2, 3, 1, 200, ''), (3, 3, 2, 300, ''), (4, 2, 3, 200, ''),
    (5, 2, 4, 150, ''), (6, 3, 5, 250, ''), (7, 2, 6, 400, '');
SET IDENTITY_INSERT dbo.aporte_Camp OFF;

-- Precios: DANE de los ultimos 15 dias y precio estimado vigente (alimentan CU-009 y CU-028) -----------------------
DECLARE @i INT = 0, @id INT = 1;
SET IDENTITY_INSERT dbo.dato_Dane ON;
WHILE @i < 15
BEGIN
    INSERT dbo.dato_Dane (id_Dato_Dane, id_Tipo_Producto, id_Variedad, precio_Dato, fecha_Dato) VALUES
        (@id,     1, 1, 1500 + (14 - @i) * 12 + (@i % 3) * 8  - (@i % 2) * 4, CAST(DATEADD(DAY, -@i, @ahora) AS DATE)),
        (@id + 1, 2, 2, 2200 + (14 - @i) * 20 + (@i % 3) * 15 - (@i % 2) * 7, CAST(DATEADD(DAY, -@i, @ahora) AS DATE)),
        (@id + 2, 3, 3, 1900 + (14 - @i) * 10 + (@i % 3) * 12 - (@i % 2) * 6, CAST(DATEADD(DAY, -@i, @ahora) AS DATE)),
        (@id + 3, 4, 4, 1100 + (14 - @i) * 6  + (@i % 3) * 6  - (@i % 2) * 3, CAST(DATEADD(DAY, -@i, @ahora) AS DATE));
    SET @i += 1; SET @id += 4;
END
SET IDENTITY_INSERT dbo.dato_Dane OFF;

SET IDENTITY_INSERT dbo.precio_Estimado ON;
INSERT dbo.precio_Estimado (id_Precio_Estimado, id_Tipo_Producto, id_Variedad, precio_Esti, fecha_Preci) VALUES
    (1, 1, 1, 1750, CAST(@ahora AS DATE)),
    (2, 2, 2, 2450, CAST(@ahora AS DATE)),
    (3, 3, 3, 2050, CAST(@ahora AS DATE)),
    (4, 4, 4, 1180, CAST(@ahora AS DATE));
SET IDENTITY_INSERT dbo.precio_Estimado OFF;

-- Viajes (CU-020 los asigna el administrador; aqui ya hay cinco en distintas etapas) ---------------------------------
INSERT dbo.viaje (id_Demanda, id_Usuario_Transportador, id_Usuario_Admin, nombre_Transportador, telefono_Transportador, placa, modelo, capacidad, estado_Envio, fecha_Asignacion,
                  llegada_Notificada, fecha_Llegada, transportador_Confirma, comprador_Confirma, comprador_Con_Observaciones, observacion_Comprador, visible_Transportador, fecha_Entrega) VALUES
    (1, 6, 1, 'Pedro Transporta',  '300000006', 'ABC123', 'NPR 2020',   5000, 'Entregado y Recibido Correctamente', DATEADD(DAY, -7, @ahora),  1, DATEADD(DAY, -5, @ahora),       1, 1, 0, NULL, 0, DATEADD(MINUTE, 20, DATEADD(DAY, -5, @ahora))),
    (2, 6, 1, 'Pedro Transporta',  '300000006', 'XYZ789', 'Hino 2018',  4000, N'En tr' + NCHAR(225) + N'nsito',     DATEADD(DAY, -1, @ahora),  1, DATEADD(MINUTE, -10, @ahora),  0, 0, 0, NULL, 1, NULL),
    (3, 7, 1, 'Julian Camionero',  '300000007', 'QWE456', 'Turbo 2019', 3000, 'Asignado',                           DATEADD(HOUR, -3, @ahora), 0, NULL,                           0, 0, 0, NULL, 1, NULL),
    (5, 7, 1, 'Julian Camionero',  '300000007', 'HNO321', 'NHR 2021',   3500, 'Entregado con Observaciones',        DATEADD(DAY, -4, @ahora),  1, DATEADD(DAY, -2, @ahora),       1, 1, 1, 'Dos canastillas llegaron danadas', 0, DATEADD(MINUTE, 30, DATEADD(DAY, -2, @ahora))),
    (6, 7, 1, 'Julian Camionero',  '300000007', 'JKL654', 'Dyna 2020',  2500, N'En tr' + NCHAR(225) + N'nsito',     DATEADD(DAY, -1, @ahora),  0, NULL,                           0, 0, 0, NULL, 1, NULL);

-- Reportes de ubicacion (CU-021): el lugar es un punto de acopio registrado (sus coordenadas salen de punto_Acopio) --------
INSERT dbo.reporte_Ubicacion (id_Viaje, id_Demanda, id_Usuario, fecha_Inicial, fecha_Final, region, punto_Origen, punto_Destino, lugar, hito, fecha_Hora)
SELECT v.id_Viaje, r.dem, r.usr, CAST(r.cuando AS DATE), DATEADD(DAY, 1, CAST(r.cuando AS DATE)), 'Andina', r.origen, r.destino, r.lugar, r.hito, r.cuando
FROM (VALUES
    (1, 6, 'Fusagasuga', 'Bogota', 'Acopio Fusagasuga Centro',   'Salida',         DATEADD(HOUR, -3,  DATEADD(DAY, -5, @ahora))),
    (1, 6, 'Fusagasuga', 'Bogota', 'Acopio Peaje Chusaca',       'Avance 2 horas', DATEADD(MINUTE, -90, DATEADD(DAY, -5, @ahora))),
    (1, 6, 'Fusagasuga', 'Bogota', 'Acopio Corabastos',          'Destino Final',  DATEADD(DAY, -5, @ahora)),
    (2, 6, 'Fusagasuga', 'Bogota', 'Acopio Fusagasuga Centro',   'Salida',         DATEADD(HOUR, -4,  @ahora)),
    (2, 6, 'Fusagasuga', 'Bogota', 'Acopio Peaje Chusaca',       'Avance 2 horas', DATEADD(MINUTE, -150, @ahora)),
    (2, 6, 'Fusagasuga', 'Bogota', 'Acopio Plaza de Paloquemao', 'Destino Final',  DATEADD(MINUTE, -10, @ahora)),
    (5, 7, 'Fusagasuga', 'Chia',   'Acopio Fusagasuga Centro',   'Salida',         DATEADD(HOUR, -4,  DATEADD(DAY, -2, @ahora))),
    (5, 7, 'Fusagasuga', 'Chia',   'Acopio Chia Centro',         'Destino Final',  DATEADD(DAY, -2, @ahora)),
    (6, 7, 'Fusagasuga', 'Chia',   'Acopio Fusagasuga Centro',   'Salida',         DATEADD(HOUR, -5,  @ahora)),
    (6, 7, 'Fusagasuga', 'Chia',   'Acopio Portal de Usme',      'Avance 2 horas', DATEADD(HOUR, -3,  @ahora))
) AS r (dem, usr, origen, destino, lugar, hito, cuando)
JOIN dbo.viaje v ON v.id_Demanda = r.dem;

-- Calificaciones del pedido 1 (quedan pendientes las de Luis, Marta y Pedro hacia Carlos) ----------------------------
INSERT dbo.calificacion (id_Demanda, id_Usuario_Evalua, id_Usuario_Evaluado, estrellas, comentario, fecha) VALUES
    (1, 4, 2, 5, 'Excelente papa, muy puntual', DATEADD(DAY, -4, @ahora)),
    (1, 4, 3, 4, 'Buen producto',               DATEADD(HOUR, 1, DATEADD(DAY, -4, @ahora))),
    (1, 4, 6, 3, N'Lleg' + NCHAR(243) + N' un poco tarde', DATEADD(HOUR, 2, DATEADD(DAY, -4, @ahora))),
    (1, 6, 4, 5, 'Cliente amable',              DATEADD(HOUR, 3, DATEADD(DAY, -4, @ahora)));

-- Avisos pendientes (campana y ventana emergente) ---------------------------------------------------------------------
INSERT dbo.notificacion (id_Usuario, id_Demanda, tipo, mensaje, fecha) VALUES
    (4, 2, 'Ubicacion', N'Nueva actualizaci' + NCHAR(243) + N'n de ubicaci' + NCHAR(243) + N'n recibida: Acopio Plaza de Paloquemao (Destino Final) - pedido PED-0002.', DATEADD(MINUTE, -10, @ahora)),
    (3, 2, 'Ubicacion', N'Nueva actualizaci' + NCHAR(243) + N'n de ubicaci' + NCHAR(243) + N'n recibida: Acopio Plaza de Paloquemao (Destino Final) - pedido PED-0002.', DATEADD(MINUTE, -10, @ahora)),
    (4, 2, 'Llegada',   N'Su pedido ha arribado al punto de entrega. Por favor verifique la mercanc' + NCHAR(237) + N'a.', DATEADD(MINUTE, -10, @ahora)),
    (1, 5, 'Novedad',   'Novedad en la entrega del pedido PED-0005: Dos canastillas llegaron danadas', DATEADD(MINUTE, 30, DATEADD(DAY, -2, @ahora)));

-- Bitacora (CU-030) -------------------------------------------------------------------------------------------------------
INSERT dbo.log_Sistema (fecha, nivel, id_Usuario, origen, mensaje) VALUES
    (DATEADD(DAY, -5, @ahora),                      'INFO', 6, 'CU-021 Reporte ubicacion', 'Reporte ''Destino Final'' en ''Acopio Corabastos'' del pedido PED-0001.'),
    (DATEADD(MINUTE, 25, DATEADD(DAY, -5, @ahora)), 'INFO', 4, 'CU-025 Recepcion',         'Comprador registro recepcion ''SinObservaciones'' del pedido PED-0001. Estado: Entregado y Recibido Correctamente.'),
    (DATEADD(MINUTE, 35, DATEADD(DAY, -2, @ahora)), 'WARN', 5, 'CU-025 Recepcion',         'Comprador registro recepcion ''ConObservaciones'' del pedido PED-0005. Estado: Entregado con Observaciones.');

PRINT 'Datos de prueba cargados.';
GO
