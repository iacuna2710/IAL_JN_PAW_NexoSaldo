using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using PAW_Jueves_API.Options;

namespace PAW_Jueves_API.Services
{
    /// <summary>Implementación de <see cref="ITokenService"/> usando la sección "Jwt" de appsettings.</summary>
    public sealed class TokenService(IOptions<JwtOptions> opciones) : ITokenService
    {
        private readonly JwtOptions _jwt = opciones.Value;

        public string Generar(int idUsuario, string nombreCompleto)
        {
            var credenciales = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Clave)),
                SecurityAlgorithms.HmacSha256);

            var descriptor = new SecurityTokenDescriptor
            {
                Issuer = _jwt.Emisor,
                Audience = _jwt.Audiencia,
                Expires = DateTime.UtcNow.AddMinutes(_jwt.MinutosExpiracion),
                SigningCredentials = credenciales,
                Subject = new ClaimsIdentity(
                [
                    new Claim(JwtRegisteredClaimNames.Sub, idUsuario.ToString()),
                    new Claim(JwtRegisteredClaimNames.Name, nombreCompleto)
                ])
            };

            return new JsonWebTokenHandler().CreateToken(descriptor);
        }
    }
}
