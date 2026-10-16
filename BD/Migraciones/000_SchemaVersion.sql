-- =============================================================================
-- Migración 000: tabla de control de versiones del esquema.
--
-- Cómo funcionan las migraciones de BD/Migraciones:
--  - Cada archivo se llama NNN_Descripcion.sql y se ejecuta en orden de nombre.
--  - db-init (docker-compose) los corre TODOS cada vez que se levanta, por eso cada
--    script debe ser idempotente: si ya se aplicó, no hace nada (IF NOT EXISTS,
--    CREATE OR ALTER PROCEDURE...).
--  - Al final, cada script registra su versión en dbo.SchemaVersion. Así se puede
--    consultar qué cambios tiene una BD: SELECT * FROM dbo.SchemaVersion
--  - Una migración ya publicada NO se edita: si hay que corregir algo, se crea otra.
-- =============================================================================
USE [JN_BD]
GO

IF OBJECT_ID(N'dbo.SchemaVersion', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SchemaVersion
    (
        Version       INT           NOT NULL CONSTRAINT PK_SchemaVersion PRIMARY KEY,
        Descripcion   NVARCHAR(200) NOT NULL,
        FechaAplicada DATETIME2(0)  NOT NULL CONSTRAINT DF_SchemaVersion_Fecha DEFAULT SYSUTCDATETIME()
    )
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.SchemaVersion WHERE Version = 0)
    INSERT INTO dbo.SchemaVersion (Version, Descripcion) VALUES (0, N'Tabla de control de migraciones')
GO
