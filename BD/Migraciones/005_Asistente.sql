-- =============================================================================
-- Migración 005: asistente con IA (Gemini).
--  - ChatMensaje: historial del chat de cada usuario ('user' = el usuario, 'model' = la IA).
--  - ResumenIA: caché del resumen del dashboard por usuario y mes. Hash identifica los
--    números con los que se generó; si los números no cambian, no se vuelve a llamar a
--    Gemini (así se cuida la cuota gratuita).
-- =============================================================================
USE [JN_BD]
GO

IF OBJECT_ID(N'dbo.ChatMensaje', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ChatMensaje
    (
        IdMensaje INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ChatMensaje PRIMARY KEY,
        IdUsuario INT            NOT NULL CONSTRAINT FK_ChatMensaje_Usuario REFERENCES dbo.Usuario (IdUsuario),
        Rol       VARCHAR(5)     NOT NULL CONSTRAINT CK_ChatMensaje_Rol CHECK (Rol IN ('user', 'model')),
        Texto     NVARCHAR(4000) NOT NULL,
        Fecha     DATETIME2(0)   NOT NULL CONSTRAINT DF_ChatMensaje_Fecha DEFAULT SYSUTCDATETIME()
    )
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ChatMensaje_Usuario')
    CREATE INDEX IX_ChatMensaje_Usuario ON dbo.ChatMensaje (IdUsuario, IdMensaje DESC)
GO

IF OBJECT_ID(N'dbo.ResumenIA', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ResumenIA
    (
        IdUsuario INT            NOT NULL CONSTRAINT FK_ResumenIA_Usuario REFERENCES dbo.Usuario (IdUsuario),
        Anno      INT            NOT NULL,
        Mes       INT            NOT NULL,
        Hash      CHAR(64)       NOT NULL,
        Texto     NVARCHAR(2000) NOT NULL,
        Fecha     DATETIME2(0)   NOT NULL CONSTRAINT DF_ResumenIA_Fecha DEFAULT SYSUTCDATETIME(),
        CONSTRAINT PK_ResumenIA PRIMARY KEY (IdUsuario, Anno, Mes)
    )
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_GuardarMensajeChat
    @IdUsuario INT,
    @Rol       VARCHAR(5),
    @Texto     NVARCHAR(4000)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.ChatMensaje (IdUsuario, Rol, Texto) VALUES (@IdUsuario, @Rol, @Texto)
END
GO

-- Últimos @Top mensajes, devueltos en orden cronológico (el más viejo primero)
CREATE OR ALTER PROCEDURE dbo.sp_ConsultarChat
    @IdUsuario INT,
    @Top       INT = 50
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Rol, Texto, Fecha
    FROM (SELECT TOP (@Top) IdMensaje, Rol, Texto, Fecha
          FROM   dbo.ChatMensaje
          WHERE  IdUsuario = @IdUsuario
          ORDER BY IdMensaje DESC) AS Ultimos
    ORDER BY IdMensaje
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_LimpiarChat
    @IdUsuario INT
AS
BEGIN
    DELETE FROM dbo.ChatMensaje WHERE IdUsuario = @IdUsuario
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_ConsultarResumenIA
    @IdUsuario INT,
    @Anno      INT,
    @Mes       INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Hash, Texto, Fecha FROM dbo.ResumenIA WHERE IdUsuario = @IdUsuario AND Anno = @Anno AND Mes = @Mes
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_GuardarResumenIA
    @IdUsuario INT,
    @Anno      INT,
    @Mes       INT,
    @Hash      CHAR(64),
    @Texto     NVARCHAR(2000)
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

IF NOT EXISTS (SELECT 1 FROM dbo.SchemaVersion WHERE Version = 5)
    INSERT INTO dbo.SchemaVersion (Version, Descripcion) VALUES (5, N'Asistente con IA: historial de chat y caché del resumen')
GO
