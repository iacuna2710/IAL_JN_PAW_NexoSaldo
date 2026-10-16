using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using PAW_Jueves_API.Options;
using PAW_Jueves_API.Services;

namespace PAW_Jueves_API.Tests
{
    /// <summary>Pruebas del token JWT que se entrega al iniciar sesión.</summary>
    public class TokenServiceTests
    {
        private static readonly JwtOptions Opciones = new()
        {
            Emisor = "PAW_Jueves_API",
            Audiencia = "PAW_Jueves",
            Clave = "clave-de-pruebas-con-mas-de-32-caracteres",
            MinutosExpiracion = 60
        };

        private static TokenValidationParameters Parametros(string clave) => new()
        {
            ValidIssuer = Opciones.Emisor,
            ValidAudience = Opciones.Audiencia,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(clave))
        };

        [Fact]
        public async Task Generar_TokenValido_TraeIdUsuarioYNombre()
        {
            var token = new TokenService(Microsoft.Extensions.Options.Options.Create(Opciones)).Generar(1004, "Isaac");

            var resultado = await new JsonWebTokenHandler().ValidateTokenAsync(token, Parametros(Opciones.Clave));

            Assert.True(resultado.IsValid);
            Assert.Equal("1004", resultado.Claims[JwtRegisteredClaimNames.Sub]);
            Assert.Equal("Isaac", resultado.Claims[JwtRegisteredClaimNames.Name]);
        }

        [Fact]
        public async Task Generar_FirmadoConOtraClave_NoEsValido()
        {
            var token = new TokenService(Microsoft.Extensions.Options.Options.Create(Opciones)).Generar(1, "Ana");

            var resultado = await new JsonWebTokenHandler().ValidateTokenAsync(
                token, Parametros("otra-clave-distinta-tambien-de-32-caracteres"));

            Assert.False(resultado.IsValid);
        }
    }
}
