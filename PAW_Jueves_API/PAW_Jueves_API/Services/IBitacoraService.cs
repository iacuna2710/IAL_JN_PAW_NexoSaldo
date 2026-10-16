namespace PAW_Jueves_API.Services
{
    /// <summary>
    /// Nombres de las acciones que se guardan en la bitácora. La WEB los traduce a un texto con
    /// ícono (ver PAW_Jueves/Services/AccionesBitacora.cs), así que si se agrega una acción aquí
    /// también hay que agregarla allá.
    /// </summary>
    public static class AccionesBitacora
    {
        public const string InicioSesion = "InicioSesion";
        public const string InicioSesionFallido = "InicioSesionFallido";
        public const string CambioPerfil = "CambioPerfil";
        public const string CambioFoto = "CambioFoto";
        public const string CambioPreferencias = "CambioPreferencias";
        public const string CambioContrasenna = "CambioContrasenna";
        public const string CambioContrasennaFallido = "CambioContrasennaFallido";
        public const string Bloqueo = "Bloqueo";
        public const string Desbloqueo = "Desbloqueo";
        public const string DesbloqueoFallido = "DesbloqueoFallido";
    }

    /// <summary>Guarda acciones del usuario en la tabla dbo.Bitacora.</summary>
    public interface IBitacoraService
    {
        /// <summary>
        /// Registra una acción. Si la bitácora falla (por ejemplo, la tabla no existe), el error se
        /// escribe en el log y NO se propaga: no tiene sentido que un cambio de foto falle solo
        /// porque no se pudo anotar en la bitácora.
        /// </summary>
        Task RegistrarAsync(int idUsuario, string accion, string? detalle = null, CancellationToken cancellationToken = default);
    }
}
