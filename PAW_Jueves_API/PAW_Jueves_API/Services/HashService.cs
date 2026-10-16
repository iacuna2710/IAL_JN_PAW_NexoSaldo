using System.Security.Cryptography;
using System.Text;

namespace PAW_Jueves_API.Services
{
    /// <summary>Implementación de <see cref="IHashService"/> con la librería BCrypt.Net-Next.</summary>
    public sealed class HashService : IHashService
    {
        public string Hashear(string contrasenna) => BCrypt.Net.BCrypt.HashPassword(contrasenna);

        public ResultadoVerificacion Verificar(string contrasenna, string guardada)
        {
            if (string.IsNullOrEmpty(guardada))
                return ResultadoVerificacion.Incorrecta;

            // Todos los hashes de BCrypt empiezan con "$2" ($2a$, $2b$...).
            if (guardada.StartsWith("$2", StringComparison.Ordinal))
            {
                try
                {
                    return BCrypt.Net.BCrypt.Verify(contrasenna, guardada)
                        ? ResultadoVerificacion.Correcta
                        : ResultadoVerificacion.Incorrecta;
                }
                catch (BCrypt.Net.SaltParseException)
                {
                    // Empieza con "$2" pero no es un hash válido.
                    return ResultadoVerificacion.Incorrecta;
                }
            }

            // Usuario viejo con la contraseña en texto plano. FixedTimeEquals tarda lo mismo
            // aunque falle en la primera letra, para no dar pistas por el tiempo de respuesta.
            var coincide = CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(contrasenna),
                Encoding.UTF8.GetBytes(guardada));

            return coincide ? ResultadoVerificacion.CorrectaRequiereHash : ResultadoVerificacion.Incorrecta;
        }
    }
}
