namespace PAW_Jueves_API.Services
{
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
