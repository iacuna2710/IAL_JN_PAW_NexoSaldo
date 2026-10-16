using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;

namespace PAW_Jueves_API.Services
{
    /// <summary>Atajos para leer los datos del usuario autenticado desde su token.</summary>
    public static class ClaimsExtensions
    {
        /// <summary>
        /// IdUsuario del token (claim "sub"). Solo se usa dentro de endpoints con [Authorize],
        /// donde el token ya fue validado; si por algún motivo no viene, lanza una excepción.
        /// </summary>
        public static int ObtenerIdUsuario(this ClaimsPrincipal usuario) =>
            int.Parse(usuario.FindFirstValue(JwtRegisteredClaimNames.Sub)
                ?? throw new InvalidOperationException("El token no trae el claim sub."));
    }
}
