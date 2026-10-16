namespace PAW_Jueves_API.Services
{
    /// <summary>Genera el token JWT que la WEB manda en cada petición (Authorization: Bearer ...).</summary>
    public interface ITokenService
    {
        /// <summary>
        /// Crea un token firmado con el IdUsuario (claim "sub") y el nombre (claim "name").
        /// Los endpoints protegidos con [Authorize] leen el IdUsuario de aquí, NUNCA del cuerpo
        /// de la petición: así nadie puede leer o cambiar datos de otro usuario cambiando un número.
        /// </summary>
        string Generar(int idUsuario, string nombreCompleto);
    }
}
