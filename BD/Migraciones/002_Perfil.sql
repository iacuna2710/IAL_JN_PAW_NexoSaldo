-- =============================================================================
-- Migración 002: datos de perfil del usuario (My Account y Settings).
--  - Columnas nuevas en Usuario: FotoPerfil (ruta del archivo en la WEB), Telefono y
--    Moneda (preferencia para el módulo de gastos: CRC, USD o EUR).
--  - Procedimientos para consultar y actualizar el perfil, la foto, las preferencias
--    y la contraseña desde la API (siempre por IdUsuario, que sale del token JWT).
-- =============================================================================
USE [JN_BD]
GO

IF COL_LENGTH('dbo.Usuario', 'FotoPerfil') IS NULL
    ALTER TABLE dbo.Usuario ADD FotoPerfil NVARCHAR(255) NULL;

IF COL_LENGTH('dbo.Usuario', 'Telefono') IS NULL
    ALTER TABLE dbo.Usuario ADD Telefono NVARCHAR(20) NULL;

IF COL_LENGTH('dbo.Usuario', 'Moneda') IS NULL
    ALTER TABLE dbo.Usuario ADD Moneda CHAR(3) NOT NULL CONSTRAINT DF_Usuario_Moneda DEFAULT 'CRC';
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_Usuario_Moneda')
    ALTER TABLE dbo.Usuario ADD CONSTRAINT CK_Usuario_Moneda CHECK (Moneda IN ('CRC', 'USD', 'EUR'));
GO

-- Se agrega FotoPerfil para que la WEB muestre la foto en la barra superior al iniciar sesión
CREATE OR ALTER PROCEDURE dbo.sp_ConsultarCredenciales
    @CorreoElectronico NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT  IdUsuario, NombreCompleto, CorreoElectronico, Contrasenna, FotoPerfil
    FROM    dbo.Usuario
    WHERE   CorreoElectronico = @CorreoElectronico
        AND Estado = 1
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_ConsultarUsuario
    @IdUsuario INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT  IdUsuario, Identificacion, NombreCompleto, CorreoElectronico,
            Telefono, Moneda, FotoPerfil
    FROM    dbo.Usuario
    WHERE   IdUsuario = @IdUsuario
        AND Estado = 1
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_ActualizarPerfil
    @IdUsuario      INT,
    @NombreCompleto NVARCHAR(250),
    @Telefono       NVARCHAR(20)
AS
BEGIN
    UPDATE  dbo.Usuario
    SET     NombreCompleto = @NombreCompleto,
            Telefono       = NULLIF(LTRIM(RTRIM(@Telefono)), '')
    WHERE   IdUsuario = @IdUsuario
        AND Estado = 1
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_ActualizarPreferencias
    @IdUsuario INT,
    @Moneda    CHAR(3)
AS
BEGIN
    UPDATE  dbo.Usuario
    SET     Moneda = @Moneda
    WHERE   IdUsuario = @IdUsuario
        AND Estado = 1
END
GO

-- Devuelve la foto ANTERIOR para que la WEB borre ese archivo después de guardar la nueva
CREATE OR ALTER PROCEDURE dbo.sp_ActualizarFoto
    @IdUsuario  INT,
    @FotoPerfil NVARCHAR(255)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE  dbo.Usuario
    SET     FotoPerfil = @FotoPerfil
    OUTPUT  deleted.FotoPerfil AS FotoAnterior
    WHERE   IdUsuario = @IdUsuario
        AND Estado = 1
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_ConsultarContrasenna
    @IdUsuario INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT  Contrasenna
    FROM    dbo.Usuario
    WHERE   IdUsuario = @IdUsuario
        AND Estado = 1
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.SchemaVersion WHERE Version = 2)
    INSERT INTO dbo.SchemaVersion (Version, Descripcion) VALUES (2, N'Perfil: foto, teléfono, moneda y cambio de contraseña')
GO
