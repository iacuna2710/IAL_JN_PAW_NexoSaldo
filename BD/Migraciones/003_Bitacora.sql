-- =============================================================================
-- Migración 003: bitácora de actividad del usuario.
-- Registra acciones de seguridad y de la cuenta (inicio de sesión, cambio de foto,
-- de contraseña, bloqueo de pantalla...). Se muestra en "Mi cuenta" y en el inicio.
-- NUNCA se guardan contraseñas ni tokens en Detalle.
-- La Fecha se guarda en UTC; la WEB la convierte a la hora local al mostrarla.
-- =============================================================================
USE [JN_BD]
GO

IF OBJECT_ID(N'dbo.Bitacora', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Bitacora
    (
        IdBitacora INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Bitacora PRIMARY KEY,
        IdUsuario  INT           NOT NULL CONSTRAINT FK_Bitacora_Usuario REFERENCES dbo.Usuario (IdUsuario),
        Accion     NVARCHAR(50)  NOT NULL,
        Detalle    NVARCHAR(250) NULL,
        Ip         NVARCHAR(45)  NULL,
        Fecha      DATETIME2(0)  NOT NULL CONSTRAINT DF_Bitacora_Fecha DEFAULT SYSUTCDATETIME()
    )
END
GO

-- La consulta siempre es "las últimas N acciones de un usuario"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Bitacora_Usuario_Fecha')
    CREATE INDEX IX_Bitacora_Usuario_Fecha ON dbo.Bitacora (IdUsuario, Fecha DESC) INCLUDE (Accion, Detalle, Ip)
GO

CREATE OR ALTER PROCEDURE dbo.sp_RegistrarBitacora
    @IdUsuario INT,
    @Accion    NVARCHAR(50),
    @Detalle   NVARCHAR(250) = NULL,
    @Ip        NVARCHAR(45)  = NULL
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.Bitacora (IdUsuario, Accion, Detalle, Ip)
    VALUES (@IdUsuario, @Accion, @Detalle, @Ip)
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_ConsultarBitacora
    @IdUsuario INT,
    @Top       INT = 10
AS
BEGIN
    SET NOCOUNT ON;

    SELECT  TOP (@Top) Accion, Detalle, Ip, Fecha
    FROM    dbo.Bitacora
    WHERE   IdUsuario = @IdUsuario
    ORDER BY Fecha DESC, IdBitacora DESC
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.SchemaVersion WHERE Version = 3)
    INSERT INTO dbo.SchemaVersion (Version, Descripcion) VALUES (3, N'Bitácora de actividad del usuario')
GO
