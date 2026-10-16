-- =============================================================================
-- Migración 006: resumen por quincenas para el "Resumen IA" del dashboard.
--  - sp_ResumenQuincenal: totales de la primera (1 al 15) y la segunda quincena
--    (16 a fin de mes) y el gasto por categoría en cada una. Los montos se calculan
--    aquí; Gemini solo los redacta.
--  - ResumenIA.Texto pasa de 2000 a 4000 caracteres: el resumen ahora es más detallado.
-- =============================================================================
USE [JN_BD]
GO

IF EXISTS (SELECT 1 FROM sys.columns
           WHERE object_id = OBJECT_ID(N'dbo.ResumenIA') AND name = 'Texto' AND max_length < 8000)
    ALTER TABLE dbo.ResumenIA ALTER COLUMN Texto NVARCHAR(4000) NOT NULL;
GO

CREATE OR ALTER PROCEDURE dbo.sp_GuardarResumenIA
    @IdUsuario INT,
    @Anno      INT,
    @Mes       INT,
    @Hash      CHAR(64),
    @Texto     NVARCHAR(4000)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.ResumenIA
    SET    Hash = @Hash, Texto = @Texto, Fecha = SYSUTCDATETIME()
    WHERE  IdUsuario = @IdUsuario AND Anno = @Anno AND Mes = @Mes;

    IF @@ROWCOUNT = 0
        INSERT INTO dbo.ResumenIA (IdUsuario, Anno, Mes, Hash, Texto) VALUES (@IdUsuario, @Anno, @Mes, @Hash, @Texto);
END
GO

-- Dos resultados:
--  1) Una fila por quincena (siempre 2): Quincena (1 o 2), Desde, Hasta, Ingresos, Gastos, CantidadMovimientos.
--  2) Gasto por categoría en cada quincena: Quincena, IdCategoria, Nombre, Color, Total.
CREATE OR ALTER PROCEDURE dbo.sp_ResumenQuincenal
    @IdUsuario INT,
    @Anno      INT,
    @Mes       INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Inicio DATE = DATEFROMPARTS(@Anno, @Mes, 1);
    DECLARE @Dia16  DATE = DATEFROMPARTS(@Anno, @Mes, 16);
    DECLARE @Fin    DATE = EOMONTH(@Inicio);

    WITH Quincenas AS
    (
        SELECT 1 AS Quincena, @Inicio AS Desde, DATEADD(DAY, -1, @Dia16) AS Hasta
        UNION ALL
        SELECT 2, @Dia16, @Fin
    )
    SELECT  q.Quincena, q.Desde, q.Hasta,
            ISNULL(SUM(CASE WHEN m.Tipo = 'I' THEN m.Monto END), 0) AS Ingresos,
            ISNULL(SUM(CASE WHEN m.Tipo = 'G' THEN m.Monto END), 0) AS Gastos,
            COUNT(m.IdMovimiento) AS CantidadMovimientos
    FROM    Quincenas q
            LEFT JOIN dbo.Movimiento m
                   ON m.IdUsuario = @IdUsuario AND m.Fecha BETWEEN q.Desde AND q.Hasta
    GROUP BY q.Quincena, q.Desde, q.Hasta
    ORDER BY q.Quincena;

    SELECT  CASE WHEN m.Fecha < @Dia16 THEN 1 ELSE 2 END AS Quincena,
            c.IdCategoria, c.Nombre, c.Color, SUM(m.Monto) AS Total
    FROM    dbo.Movimiento m
            INNER JOIN dbo.Categoria c ON c.IdCategoria = m.IdCategoria
    WHERE   m.IdUsuario = @IdUsuario
        AND m.Tipo = 'G'
        AND m.Fecha BETWEEN @Inicio AND @Fin
    GROUP BY CASE WHEN m.Fecha < @Dia16 THEN 1 ELSE 2 END, c.IdCategoria, c.Nombre, c.Color
    ORDER BY Quincena, Total DESC;
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.SchemaVersion WHERE Version = 6)
    INSERT INTO dbo.SchemaVersion (Version, Descripcion) VALUES (6, N'Resumen por quincenas y resumen IA más largo')
GO
