-- =============================================================================
-- Migración 001: procedimientos para el login con contraseñas hasheadas (BCrypt).
--
-- Antes, sp_IniciarSesion comparaba la contraseña en SQL (texto plano). Ahora:
--  1. La API pide los datos del usuario por correo (sp_ConsultarCredenciales),
--     incluida la contraseña guardada (hash de BCrypt o, para usuarios viejos,
--     texto plano).
--  2. La API verifica la contraseña en C# con BCrypt.
--  3. Si el usuario todavía tenía la contraseña en texto plano, la API la
--     reemplaza por el hash en ese mismo momento (sp_ActualizarContrasenna).
-- sp_IniciarSesion se deja tal cual por compatibilidad, pero la API ya no lo usa.
-- =============================================================================
USE [JN_BD]
GO

CREATE OR ALTER PROCEDURE dbo.sp_ConsultarCredenciales
    @CorreoElectronico NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT  IdUsuario, NombreCompleto, CorreoElectronico, Contrasenna
    FROM    dbo.Usuario
    WHERE   CorreoElectronico = @CorreoElectronico
        AND Estado = 1
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_ActualizarContrasenna
    @IdUsuario   INT,
    @Contrasenna NVARCHAR(100)
AS
BEGIN
    UPDATE  dbo.Usuario
    SET     Contrasenna = @Contrasenna
    WHERE   IdUsuario = @IdUsuario
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_ConsultarUsuario
    @IdUsuario INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT  IdUsuario, Identificacion, NombreCompleto, CorreoElectronico
    FROM    dbo.Usuario
    WHERE   IdUsuario = @IdUsuario
        AND Estado = 1
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.SchemaVersion WHERE Version = 1)
    INSERT INTO dbo.SchemaVersion (Version, Descripcion) VALUES (1, N'Login con BCrypt y consulta de usuario')
GO
