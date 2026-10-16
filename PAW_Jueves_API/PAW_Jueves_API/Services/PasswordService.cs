using System.Security.Cryptography;
using System.Text;

namespace PAW_Jueves_API.Services
{
    /// <summary>
    /// Implementación de <see cref="IPasswordService"/>. Genera tokens para el link
    /// de recuperación de acceso usando dos algoritmos:
    ///
    ///  - AES-256 (cifrado): oculta el contenido del token (id del usuario + vencimiento),
    ///    así nadie puede leer a qué usuario pertenece el link.
    ///  - HMAC-SHA256 (firma): garantiza que el token no fue modificado. Si alguien
    ///    cambia un solo carácter, la firma ya no coincide y el token se rechaza.
    ///
    /// Estructura del token (antes de pasarlo a Base64):
    ///   [ IV (16 bytes) ][ datos cifrados (n bytes) ][ firma HMAC (32 bytes) ]
    /// </summary>
    public class PasswordService : IPasswordService
    {
        // Llave maestra de 64 bytes en Base64:
        //  - los primeros 32 bytes son la llave de AES-256 (cifrar/descifrar)
        //  - los últimos 32 bytes son la llave del HMAC (firmar/verificar)
        // Nota: en un proyecto real esta llave no debería estar en el código,
        // sino en appsettings/User Secrets o en un gestor de secretos.
        private readonly byte[] Key = Convert.FromBase64String("sy+v61jJHH5tZTjth2g+PwKpfUiNXWA0ImlUWxikTKUedfPEC6zrkDSzsneTbfkTXJnaEchH7BcVBF4XkqIMDw==");

        /// <summary>
        /// Genera el token cifrado y firmado para el usuario indicado.
        /// </summary>
        /// <param name="userId">Id del usuario.</param>
        /// <param name="vigencia">Tiempo que el token será válido a partir de ahora.</param>
        /// <returns>Token en Base64 URL-safe (sin '=', '+' ni '/').</returns>
        public string Cifrar(string userId, TimeSpan vigencia)
        {
            // Contenido del token: "idUsuario|ticksDeVencimiento" (en UTC)
            var payload = Encoding.UTF8.GetBytes($"{userId}|{DateTime.UtcNow.Add(vigencia).Ticks}");
            using (var aes = Aes.Create())
            using (var hmac = new HMACSHA256(Key.AsSpan(32).ToArray()))
            {
                // Se cifra con AES usando la primera mitad de la llave.
                // El IV (vector de inicialización) es aleatorio en cada llamada, por eso
                // el mismo usuario recibe un token distinto cada vez que lo pide.
                aes.Key = Key.AsSpan(0, 32).ToArray();
                aes.GenerateIV();
                var cifrado = aes.CreateEncryptor().TransformFinalBlock(payload, 0, payload.Length);

                // Se arma el arreglo final: IV + cifrado + firma
                var datos = new byte[16 + cifrado.Length + 32];
                Buffer.BlockCopy(aes.IV, 0, datos, 0, 16);
                Buffer.BlockCopy(cifrado, 0, datos, 16, cifrado.Length);
                // La firma HMAC se calcula sobre IV + cifrado y se pega al final
                Buffer.BlockCopy(hmac.ComputeHash(datos, 0, 16 + cifrado.Length), 0, datos, 16 + cifrado.Length, 32);

                // Base64 "URL-safe": se quitan los '=' y se cambian '+' y '/'
                // para que el token se pueda poner en una URL sin romperla
                return Convert.ToBase64String(datos).TrimEnd('=').Replace('+', '-').Replace('/', '_');
            }
        }

        /// <summary>
        /// Verifica la firma del token, lo descifra y revisa que no haya vencido.
        /// </summary>
        /// <param name="token">Token recibido en el link de recuperación.</param>
        /// <returns>El id del usuario, o string vacío si el token no es válido.</returns>
        public string Descifrar(string token)
        {
            try
            {
                // Se revierte el Base64 URL-safe a Base64 normal y se reponen los '='
                var b64 = token.Replace('-', '+').Replace('_', '/');
                var datos = Convert.FromBase64String(b64.PadRight(b64.Length + (4 - b64.Length % 4) % 4, '='));

                // Tamaño mínimo: IV (16) + al menos un bloque AES (16) + firma (32)
                if (datos.Length < 16 + 16 + 32) return string.Empty;

                // 1) Verificar la firma ANTES de descifrar
                int largoMac = datos.Length - 32;
                using (var hmac = new HMACSHA256(Key.AsSpan(32).ToArray()))
                {
                    var macEsperado = hmac.ComputeHash(datos, 0, largoMac);
                    // Comparación en tiempo constante: se recorren los 32 bytes siempre,
                    // para no dar pistas por tiempo de respuesta (ataque de timing)
                    int diff = 0;
                    for (int i = 0; i < 32; i++) diff |= macEsperado[i] ^ datos[largoMac + i];
                    if (diff != 0) return string.Empty;
                }

                // 2) Descifrar con AES usando el IV guardado al inicio del token
                using (var aes = Aes.Create())
                {
                    aes.Key = Key.AsSpan(0, 32).ToArray();
                    aes.IV = datos.AsSpan(0, 16).ToArray();
                    var plano = aes.CreateDecryptor().TransformFinalBlock(datos, 16, largoMac - 16);
                    var partes = Encoding.UTF8.GetString(plano).Split('|');

                    // 3) Revisar el vencimiento: si ya pasó, el link no sirve
                    if (DateTime.UtcNow.Ticks > long.Parse(partes[1])) return string.Empty;
                    return partes[0];
                }
            }
            // Cualquier error (Base64 inválido, datos corruptos, etc.) = token inválido
            catch { return string.Empty; }
        }
    }
}
