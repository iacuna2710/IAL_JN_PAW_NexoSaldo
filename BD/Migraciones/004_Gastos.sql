-- =============================================================================
-- Migración 004: módulo de gastos personales.
--  - Categoria: de ingreso ('I') o de gasto ('G'), con color y presupuesto mensual opcional.
--  - Movimiento: un ingreso o gasto con monto, fecha y categoría.
-- Todos los procedimientos reciben @IdUsuario (sale del token JWT en la API) y filtran por él:
-- un usuario nunca puede leer ni cambiar categorías o movimientos de otro.
-- Los montos se calculan SIEMPRE aquí (SUM en SQL); la IA de la fase 6 solo los redacta.
-- =============================================================================
USE [JN_BD]
GO

IF OBJECT_ID(N'dbo.Categoria', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Categoria
    (
        IdCategoria        INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Categoria PRIMARY KEY,
        IdUsuario          INT            NOT NULL CONSTRAINT FK_Categoria_Usuario REFERENCES dbo.Usuario (IdUsuario),
        Nombre             NVARCHAR(50)   NOT NULL,
        Tipo               CHAR(1)        NOT NULL CONSTRAINT CK_Categoria_Tipo CHECK (Tipo IN ('I', 'G')),
        Color              CHAR(7)        NOT NULL CONSTRAINT DF_Categoria_Color DEFAULT '#6c757d',
        PresupuestoMensual DECIMAL(18, 2) NULL CONSTRAINT CK_Categoria_Presupuesto CHECK (PresupuestoMensual > 0),
        Estado             BIT            NOT NULL CONSTRAINT DF_Categoria_Estado DEFAULT 1,
        CONSTRAINT UK_Categoria_Usuario_Nombre_Tipo UNIQUE (IdUsuario, Nombre, Tipo)
    )
END
GO

IF OBJECT_ID(N'dbo.Movimiento', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Movimiento
    (
        IdMovimiento  INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Movimiento PRIMARY KEY,
        IdUsuario     INT            NOT NULL CONSTRAINT FK_Movimiento_Usuario REFERENCES dbo.Usuario (IdUsuario),
        IdCategoria   INT            NOT NULL CONSTRAINT FK_Movimiento_Categoria REFERENCES dbo.Categoria (IdCategoria),
        Tipo          CHAR(1)        NOT NULL CONSTRAINT CK_Movimiento_Tipo CHECK (Tipo IN ('I', 'G')),
        Monto         DECIMAL(18, 2) NOT NULL CONSTRAINT CK_Movimiento_Monto CHECK (Monto > 0),
        Fecha         DATE           NOT NULL,
        Descripcion   NVARCHAR(250)  NULL,
        FechaRegistro DATETIME2(0)   NOT NULL CONSTRAINT DF_Movimiento_FechaRegistro DEFAULT SYSUTCDATETIME()
    )
END
GO

-- Casi todas las consultas son "movimientos de un usuario en un rango de fechas"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Movimiento_Usuario_Fecha')
    CREATE INDEX IX_Movimiento_Usuario_Fecha ON dbo.Movimiento (IdUsuario, Fecha) INCLUDE (Monto, IdCategoria, Tipo)
GO

-- ============================== Categorías ==============================

-- Categorías iniciales. Se crean la primera vez que el usuario consulta sus categorías
-- (así también las reciben los usuarios que ya existían antes de esta migración).
CREATE OR ALTER PROCEDURE dbo.sp_CrearCategoriasPorDefecto
    @IdUsuario INT
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.Categoria WHERE IdUsuario = @IdUsuario)
    BEGIN
        INSERT INTO dbo.Categoria (IdUsuario, Nombre, Tipo, Color)
        VALUES (@IdUsuario, N'Comida',          'G', '#f59f00'),
               (@IdUsuario, N'Transporte',      'G', '#4dabf7'),
               (@IdUsuario, N'Servicios',       'G', '#845ef7'),
               (@IdUsuario, N'Entretenimiento', 'G', '#f06595'),
               (@IdUsuario, N'Salud',           'G', '#20c997'),
               (@IdUsuario, N'Salario',         'I', '#40c057'),
               (@IdUsuario, N'Otros ingresos',  'I', '#94d82d')
    END
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_ConsultarCategorias
    @IdUsuario       INT,
    @IncluirInactivas BIT = 0
AS
BEGIN
    SET NOCOUNT ON;

    EXEC dbo.sp_CrearCategoriasPorDefecto @IdUsuario;

    SELECT  c.IdCategoria, c.Nombre, c.Tipo, c.Color, c.PresupuestoMensual, c.Estado,
            (SELECT COUNT(*) FROM dbo.Movimiento m WHERE m.IdCategoria = c.IdCategoria) AS CantidadMovimientos
    FROM    dbo.Categoria c
    WHERE   c.IdUsuario = @IdUsuario
        AND (@IncluirInactivas = 1 OR c.Estado = 1)
    ORDER BY c.Tipo DESC, c.Nombre
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_ConsultarCategoria
    @IdUsuario   INT,
    @IdCategoria INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT  c.IdCategoria, c.Nombre, c.Tipo, c.Color, c.PresupuestoMensual, c.Estado,
            (SELECT COUNT(*) FROM dbo.Movimiento m WHERE m.IdCategoria = c.IdCategoria) AS CantidadMovimientos
    FROM    dbo.Categoria c
    WHERE   c.IdUsuario = @IdUsuario
        AND c.IdCategoria = @IdCategoria
END
GO

-- Devuelve el Id nuevo, o -1 si ya existe una categoría con ese nombre y tipo
CREATE OR ALTER PROCEDURE dbo.sp_CrearCategoria
    @IdUsuario          INT,
    @Nombre             NVARCHAR(50),
    @Tipo               CHAR(1),
    @Color              CHAR(7),
    @PresupuestoMensual DECIMAL(18, 2) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM dbo.Categoria WHERE IdUsuario = @IdUsuario AND Nombre = @Nombre AND Tipo = @Tipo)
    BEGIN
        SELECT -1 AS IdCategoria;
        RETURN;
    END

    INSERT INTO dbo.Categoria (IdUsuario, Nombre, Tipo, Color, PresupuestoMensual)
    VALUES (@IdUsuario, @Nombre, @Tipo, @Color, @PresupuestoMensual);

    SELECT CAST(SCOPE_IDENTITY() AS INT) AS IdCategoria;
END
GO

-- Resultado: 1 = actualizada, 0 = no existe (o es de otro usuario), -1 = nombre repetido
CREATE OR ALTER PROCEDURE dbo.sp_ActualizarCategoria
    @IdUsuario          INT,
    @IdCategoria        INT,
    @Nombre             NVARCHAR(50),
    @Color              CHAR(7),
    @PresupuestoMensual DECIMAL(18, 2) = NULL,
    @Estado             BIT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Tipo CHAR(1) = (SELECT Tipo FROM dbo.Categoria WHERE IdCategoria = @IdCategoria AND IdUsuario = @IdUsuario);

    IF @Tipo IS NULL
    BEGIN
        SELECT 0 AS Resultado;
        RETURN;
    END

    IF EXISTS (SELECT 1 FROM dbo.Categoria
               WHERE IdUsuario = @IdUsuario AND Nombre = @Nombre AND Tipo = @Tipo AND IdCategoria <> @IdCategoria)
    BEGIN
        SELECT -1 AS Resultado;
        RETURN;
    END

    UPDATE  dbo.Categoria
    SET     Nombre = @Nombre, Color = @Color, PresupuestoMensual = @PresupuestoMensual, Estado = @Estado
    WHERE   IdCategoria = @IdCategoria AND IdUsuario = @IdUsuario;

    SELECT 1 AS Resultado;
END
GO

-- Regla de negocio: si la categoría tiene movimientos NO se borra (se perdería el historial),
-- se desactiva. Resultado: 'E' = eliminada, 'D' = desactivada, 'N' = no existe.
CREATE OR ALTER PROCEDURE dbo.sp_EliminarCategoria
    @IdUsuario   INT,
    @IdCategoria INT
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.Categoria WHERE IdCategoria = @IdCategoria AND IdUsuario = @IdUsuario)
    BEGIN
        SELECT 'N' AS Resultado;
        RETURN;
    END

    IF EXISTS (SELECT 1 FROM dbo.Movimiento WHERE IdCategoria = @IdCategoria)
    BEGIN
        UPDATE dbo.Categoria SET Estado = 0 WHERE IdCategoria = @IdCategoria AND IdUsuario = @IdUsuario;
        SELECT 'D' AS Resultado;
        RETURN;
    END

    DELETE FROM dbo.Categoria WHERE IdCategoria = @IdCategoria AND IdUsuario = @IdUsuario;
    SELECT 'E' AS Resultado;
END
GO

-- ============================== Movimientos ==============================

-- Lista paginada. TotalFilas (COUNT(*) OVER()) trae el total sin hacer otra consulta.
CREATE OR ALTER PROCEDURE dbo.sp_ConsultarMovimientos
    @IdUsuario     INT,
    @Anno          INT,
    @Mes           INT,
    @Tipo          CHAR(1) = NULL,
    @IdCategoria   INT     = NULL,
    @Pagina        INT     = 1,
    @TamannoPagina INT     = 20
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Desde DATE = DATEFROMPARTS(@Anno, @Mes, 1);
    DECLARE @Hasta DATE = DATEADD(MONTH, 1, @Desde);

    SELECT  m.IdMovimiento, m.IdCategoria, c.Nombre AS Categoria, c.Color, m.Tipo, m.Monto, m.Fecha, m.Descripcion,
            COUNT(*) OVER () AS TotalFilas
    FROM    dbo.Movimiento m
            INNER JOIN dbo.Categoria c ON c.IdCategoria = m.IdCategoria
    WHERE   m.IdUsuario = @IdUsuario
        AND m.Fecha >= @Desde AND m.Fecha < @Hasta
        AND (@Tipo IS NULL OR m.Tipo = @Tipo)
        AND (@IdCategoria IS NULL OR m.IdCategoria = @IdCategoria)
    ORDER BY m.Fecha DESC, m.IdMovimiento DESC
    OFFSET (@Pagina - 1) * @TamannoPagina ROWS FETCH NEXT @TamannoPagina ROWS ONLY
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_ConsultarMovimiento
    @IdUsuario    INT,
    @IdMovimiento INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT  m.IdMovimiento, m.IdCategoria, c.Nombre AS Categoria, c.Color, m.Tipo, m.Monto, m.Fecha, m.Descripcion
    FROM    dbo.Movimiento m
            INNER JOIN dbo.Categoria c ON c.IdCategoria = m.IdCategoria
    WHERE   m.IdUsuario = @IdUsuario
        AND m.IdMovimiento = @IdMovimiento
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_CrearMovimiento
    @IdUsuario   INT,
    @IdCategoria INT,
    @Tipo        CHAR(1),
    @Monto       DECIMAL(18, 2),
    @Fecha       DATE,
    @Descripcion NVARCHAR(250) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.Movimiento (IdUsuario, IdCategoria, Tipo, Monto, Fecha, Descripcion)
    VALUES (@IdUsuario, @IdCategoria, @Tipo, @Monto, @Fecha, NULLIF(LTRIM(RTRIM(@Descripcion)), ''));

    SELECT CAST(SCOPE_IDENTITY() AS INT) AS IdMovimiento;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_ActualizarMovimiento
    @IdUsuario    INT,
    @IdMovimiento INT,
    @IdCategoria  INT,
    @Tipo         CHAR(1),
    @Monto        DECIMAL(18, 2),
    @Fecha        DATE,
    @Descripcion  NVARCHAR(250) = NULL
AS
BEGIN
    UPDATE  dbo.Movimiento
    SET     IdCategoria = @IdCategoria, Tipo = @Tipo, Monto = @Monto, Fecha = @Fecha,
            Descripcion = NULLIF(LTRIM(RTRIM(@Descripcion)), '')
    WHERE   IdMovimiento = @IdMovimiento
        AND IdUsuario = @IdUsuario
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_EliminarMovimiento
    @IdUsuario    INT,
    @IdMovimiento INT
AS
BEGIN
    DELETE FROM dbo.Movimiento
    WHERE  IdMovimiento = @IdMovimiento
       AND IdUsuario = @IdUsuario
END
GO

-- Total gastado en una categoría en un mes (para la alerta de presupuesto)
CREATE OR ALTER PROCEDURE dbo.sp_GastoCategoriaMes
    @IdUsuario   INT,
    @IdCategoria INT,
    @Anno        INT,
    @Mes         INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Desde DATE = DATEFROMPARTS(@Anno, @Mes, 1);

    SELECT  ISNULL(SUM(Monto), 0) AS Total
    FROM    dbo.Movimiento
    WHERE   IdUsuario = @IdUsuario
        AND IdCategoria = @IdCategoria
        AND Tipo = 'G'
        AND Fecha >= @Desde AND Fecha < DATEADD(MONTH, 1, @Desde)
END
GO

-- ============================== Reportes ==============================

-- Dos resultados: 1) totales del mes; 2) gasto por categoría con su presupuesto
CREATE OR ALTER PROCEDURE dbo.sp_ResumenMensual
    @IdUsuario INT,
    @Anno      INT,
    @Mes       INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Desde DATE = DATEFROMPARTS(@Anno, @Mes, 1);
    DECLARE @Hasta DATE = DATEADD(MONTH, 1, @Desde);

    SELECT  ISNULL(SUM(CASE WHEN Tipo = 'I' THEN Monto END), 0) AS Ingresos,
            ISNULL(SUM(CASE WHEN Tipo = 'G' THEN Monto END), 0) AS Gastos,
            COUNT(*) AS CantidadMovimientos
    FROM    dbo.Movimiento
    WHERE   IdUsuario = @IdUsuario AND Fecha >= @Desde AND Fecha < @Hasta;

    -- Categorías de gasto con movimientos en el mes o con presupuesto definido
    SELECT  c.IdCategoria, c.Nombre, c.Color, c.PresupuestoMensual,
            ISNULL(SUM(m.Monto), 0) AS Total
    FROM    dbo.Categoria c
            LEFT JOIN dbo.Movimiento m
                   ON m.IdCategoria = c.IdCategoria AND m.Fecha >= @Desde AND m.Fecha < @Hasta
    WHERE   c.IdUsuario = @IdUsuario
        AND c.Tipo = 'G'
    GROUP BY c.IdCategoria, c.Nombre, c.Color, c.PresupuestoMensual
    HAVING  SUM(m.Monto) > 0 OR c.PresupuestoMensual IS NOT NULL
    ORDER BY Total DESC
END
GO

-- Ingresos y gastos de los últimos @Meses meses (incluye los meses sin movimientos, en 0)
CREATE OR ALTER PROCEDURE dbo.sp_TendenciaMensual
    @IdUsuario INT,
    @Anno      INT,
    @Mes       INT,
    @Meses     INT = 6
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Fin DATE = DATEADD(MONTH, 1, DATEFROMPARTS(@Anno, @Mes, 1));

    WITH Meses AS
    (
        SELECT 1 AS N, DATEADD(MONTH, -1, @Fin) AS Inicio
        UNION ALL
        SELECT N + 1, DATEADD(MONTH, -1, Inicio) FROM Meses WHERE N < @Meses
    )
    SELECT  YEAR(ms.Inicio) AS Anno, MONTH(ms.Inicio) AS Mes,
            ISNULL(SUM(CASE WHEN m.Tipo = 'I' THEN m.Monto END), 0) AS Ingresos,
            ISNULL(SUM(CASE WHEN m.Tipo = 'G' THEN m.Monto END), 0) AS Gastos
    FROM    Meses ms
            LEFT JOIN dbo.Movimiento m
                   ON m.IdUsuario = @IdUsuario
                  AND m.Fecha >= ms.Inicio AND m.Fecha < DATEADD(MONTH, 1, ms.Inicio)
    GROUP BY ms.Inicio
    ORDER BY ms.Inicio
END
GO

-- La moneda preferida se agrega al login para formatear montos sin consultar el perfil
CREATE OR ALTER PROCEDURE dbo.sp_ConsultarCredenciales
    @CorreoElectronico NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT  IdUsuario, NombreCompleto, CorreoElectronico, Contrasenna, FotoPerfil, Moneda
    FROM    dbo.Usuario
    WHERE   CorreoElectronico = @CorreoElectronico
        AND Estado = 1
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.SchemaVersion WHERE Version = 4)
    INSERT INTO dbo.SchemaVersion (Version, Descripcion) VALUES (4, N'Gastos personales: categorías, movimientos y reportes')
GO
