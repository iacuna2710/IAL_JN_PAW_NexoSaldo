using System.Security.Cryptography;
using System.Text;

namespace PAW_Jueves_API.Services
{
    /// <summary>
    /// Implementación real de <see cref="IPasswordService"/>. Crea y lee los tokens del link de
    /// recuperación de acceso. Usa dos técnicas criptográficas, cada una con un propósito distinto:
    ///
    ///  1. AES-256 (cifrado simétrico): convierte el texto "idUsuario|vencimiento" en bytes
    ///     ilegibles. Sirve para que nadie pueda LEER el contenido del token. Se llama simétrico
    ///     porque la misma llave sirve para cifrar y para descifrar.
    ///  2. HMAC-SHA256 (firma): calcula una "huella" de 32 bytes a partir del token usando una
    ///     segunda llave secreta. Sirve para que nadie pueda MODIFICAR el token: si cambia aunque
    ///     sea un byte, la huella ya no coincide y el token se rechaza.
    ///
    /// Formato del token, en bytes, antes de convertirlo a texto:
    ///   | IV: 16 bytes | contenido cifrado: largo variable | firma HMAC: 32 bytes |
    /// El IV (vector de inicialización) es un valor aleatorio que AES necesita para cifrar.
    /// No es secreto: se guarda al inicio del token porque hace falta para descifrar.
    ///
    /// Ejemplo de uso:
    ///   var token = servicio.Cifrar("5", TimeSpan.FromHours(1));  // "Xy7_aB-..."
    ///   var id    = servicio.Descifrar(token);                      // "5" (o "" si venció)
    /// </summary>
    public class PasswordService : IPasswordService
    {
        // Llave secreta de 64 bytes, escrita en Base64. Se divide en dos mitades:
        //  - bytes 0 a 31 (primeros 32): llave de AES-256, para cifrar y descifrar.
        //  - bytes 32 a 63 (últimos 32): llave del HMAC, para firmar y verificar.
        // Quien tenga esta llave puede fabricar tokens válidos para cualquier usuario, por eso
        // en un sistema real no se escribe en el código: se guarda en User Secrets, en una
        // variable de entorno o en un gestor de secretos.
        private readonly byte[] Key = Convert.FromBase64String("sy+v61jJHH5tZTjth2g+PwKpfUiNXWA0ImlUWxikTKUedfPEC6zrkDSzsneTbfkTXJnaEchH7BcVBF4XkqIMDw==");

        /// <summary>
        /// Genera un token cifrado y firmado que contiene el id del usuario y su fecha de vencimiento.
        /// Cada llamada produce un token distinto, aunque sea para el mismo usuario, porque el IV
        /// se genera al azar cada vez.
        /// </summary>
        /// <param name="userId">Id del usuario como texto, por ejemplo "5".</param>
        /// <param name="vigencia">Tiempo de validez a partir de ahora, por ejemplo TimeSpan.FromHours(1).</param>
        /// <returns>Token en Base64 apto para URL: sin '=' al final, con '-' en lugar de '+' y '_' en lugar de '/'.</returns>
        public string Cifrar(string userId, TimeSpan vigencia)
        {
            // Paso 1. Texto a proteger: "id|vencimiento".
            // El vencimiento se guarda en "ticks" (cantidad de intervalos de 100 nanosegundos
            // desde el año 1) y en hora UTC, para que no dependa de la zona horaria del servidor.
            // Ejemplo: "5|639000000000000000". Luego se convierte a bytes con UTF-8.
            var payload = Encoding.UTF8.GetBytes($"{userId}|{DateTime.UtcNow.Add(vigencia).Ticks}");
            using (var aes = Aes.Create())
            using (var hmac = new HMACSHA256(Key.AsSpan(32).ToArray())) // llave HMAC = últimos 32 bytes
            {
                // Paso 2. Cifrar con AES usando los primeros 32 bytes de la llave (256 bits).
                aes.Key = Key.AsSpan(0, 32).ToArray();
                aes.GenerateIV(); // IV aleatorio de 16 bytes, nuevo en cada llamada
                var cifrado = aes.CreateEncryptor().TransformFinalBlock(payload, 0, payload.Length);

                // Paso 3. Armar un solo arreglo con espacio para: IV (16) + cifrado (n) + firma (32).
                var datos = new byte[16 + cifrado.Length + 32];
                Buffer.BlockCopy(aes.IV, 0, datos, 0, 16);                     // posiciones 0..15: IV
                Buffer.BlockCopy(cifrado, 0, datos, 16, cifrado.Length);       // luego: contenido cifrado
                // Paso 4. Firmar: el HMAC se calcula sobre IV + cifrado (todo menos el espacio de la
                // firma) y esos 32 bytes se copian en las últimas posiciones del arreglo.
                Buffer.BlockCopy(hmac.ComputeHash(datos, 0, 16 + cifrado.Length), 0, datos, 16 + cifrado.Length, 32);

                // Paso 5. Convertir los bytes a texto Base64 y adaptarlo para URL:
                //  - se quitan los '=' de relleno del final,
                //  - '+' pasa a '-' y '/' pasa a '_', porque '+' y '/' tienen significado especial en una URL.
                return Convert.ToBase64String(datos).TrimEnd('=').Replace('+', '-').Replace('/', '_');
            }
        }

        /// <summary>
        /// Valida un token y, si todo está bien, devuelve el id del usuario que contiene.
        /// Revisa tres cosas en este orden: (1) que la firma coincida, (2) que se pueda descifrar
        /// y (3) que no haya vencido. Si cualquiera falla devuelve "" sin lanzar excepción.
        /// </summary>
        /// <param name="token">Token tal como llegó en el link (formato Base64 apto para URL).</param>
        /// <returns>El id del usuario, por ejemplo "5", o "" si el token no es válido.</returns>
        public string Descifrar(string token)
        {
            try
            {
                // Paso 1. Deshacer el formato URL: '-' vuelve a '+', '_' vuelve a '/', y se agregan
                // los '=' de relleno necesarios para que el largo sea múltiplo de 4 (requisito de Base64).
                var b64 = token.Replace('-', '+').Replace('_', '/');
                var datos = Convert.FromBase64String(b64.PadRight(b64.Length + (4 - b64.Length % 4) % 4, '='));

                // Un token válido mide como mínimo 64 bytes: IV (16) + un bloque AES (16) + firma (32).
                // Si es más corto, es imposible que sea válido.
                if (datos.Length < 16 + 16 + 32) return string.Empty;

                // Paso 2. Verificar la firma ANTES de descifrar, para no procesar datos alterados.
                int largoMac = datos.Length - 32; // posición donde empieza la firma (= bytes de IV + cifrado)
                using (var hmac = new HMACSHA256(Key.AsSpan(32).ToArray()))
                {
                    // Se vuelve a calcular la firma de IV + cifrado y se compara con la que trae el token.
                    var macEsperado = hmac.ComputeHash(datos, 0, largoMac);
                    // Comparación en tiempo constante: se revisan siempre los 32 bytes completos, aunque
                    // el primero ya sea distinto. Así el tiempo de respuesta no revela cuántos bytes
                    // acertó un atacante (ataque de temporización o "timing attack").
                    // diff termina en 0 solo si todos los bytes son iguales.
                    int diff = 0;
                    for (int i = 0; i < 32; i++) diff |= macEsperado[i] ^ datos[largoMac + i];
                    if (diff != 0) return string.Empty; // la firma no coincide: token modificado o falso
                }

                // Paso 3. Descifrar con AES: la llave son los primeros 32 bytes de Key y el IV son
                // los primeros 16 bytes del token. El contenido cifrado está entre el IV y la firma.
                using (var aes = Aes.Create())
                {
                    aes.Key = Key.AsSpan(0, 32).ToArray();
                    aes.IV = datos.AsSpan(0, 16).ToArray();
                    var plano = aes.CreateDecryptor().TransformFinalBlock(datos, 16, largoMac - 16);
                    // plano vuelve a ser "id|vencimiento"; se separa en ["id", "vencimiento"]
                    var partes = Encoding.UTF8.GetString(plano).Split('|');

                    // Paso 4. Si la hora UTC actual ya pasó el vencimiento, el token no sirve.
                    if (DateTime.UtcNow.Ticks > long.Parse(partes[1])) return string.Empty;
                    return partes[0]; // id del usuario
                }
            }
            // Cualquier excepción (texto que no es Base64, datos corruptos, formato inesperado)
            // significa que el token no es válido, así que se devuelve "" en lugar de fallar.
            catch { return string.Empty; }
        }
    }
}
