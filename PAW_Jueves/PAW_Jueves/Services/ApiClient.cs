using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using PAW_Jueves.Models;

namespace PAW_Jueves.Services
{
    /// <summary>
    /// Resultado de una llamada a la API: el código HTTP y el cuerpo ya convertido al tipo pedido.
    /// Datos es null si la API no mandó cuerpo o no era JSON.
    /// </summary>
    public sealed record RespuestaApi<T>(HttpStatusCode Estado, T? Datos)
    {
        public bool Exitosa => (int)Estado is >= 200 and <= 299;
    }

    /// <summary>
    /// Cliente para llamar a la API desde los controladores de la WEB. Reemplaza el patrón anterior
    /// (HttpClient + armar la URL a mano + .Result en cada acción):
    ///  - La dirección base sale de Variables:ApiBaseUrl (se configura una vez en Program.cs).
    ///  - Si hay sesión, agrega solo el header "Authorization: Bearer {token}".
    ///  - Es asíncrono (await), así no bloquea hilos del servidor mientras espera a la API.
    /// Se registra con AddHttpClient&lt;ApiClient&gt;, que administra las conexiones correctamente.
    /// </summary>
    public sealed class ApiClient(HttpClient _http, IHttpContextAccessor _accesor)
    {
        /// <summary>POST con cuerpo JSON. Ejemplo: PostAsync&lt;UsuarioResponse&gt;("Home/Login", model).</summary>
        public Task<RespuestaApi<T>> PostAsync<T>(string ruta, object cuerpo, CancellationToken cancellationToken = default)
        {
            var peticion = new HttpRequestMessage(HttpMethod.Post, ruta) { Content = JsonContent.Create(cuerpo) };
            return EnviarAsync<T>(peticion, cancellationToken);
        }

        /// <summary>PUT con cuerpo JSON. Ejemplo: PutAsync&lt;UsuarioResponse&gt;("Usuario/Perfil", model).</summary>
        public Task<RespuestaApi<T>> PutAsync<T>(string ruta, object cuerpo, CancellationToken cancellationToken = default)
        {
            var peticion = new HttpRequestMessage(HttpMethod.Put, ruta) { Content = JsonContent.Create(cuerpo) };
            return EnviarAsync<T>(peticion, cancellationToken);
        }

        /// <summary>GET. Ejemplo: GetAsync&lt;PerfilResponse&gt;("Usuario/Perfil").</summary>
        public Task<RespuestaApi<T>> GetAsync<T>(string ruta, CancellationToken cancellationToken = default) =>
            EnviarAsync<T>(new HttpRequestMessage(HttpMethod.Get, ruta), cancellationToken);

        private async Task<RespuestaApi<T>> EnviarAsync<T>(HttpRequestMessage peticion, CancellationToken cancellationToken)
        {
            using (peticion)
            {
                var token = _accesor.HttpContext?.Session.GetString(ClavesSesion.Token);
                if (!string.IsNullOrEmpty(token))
                    peticion.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                // Si la API está apagada, SendAsync lanza HttpRequestException y el usuario termina
                // en ErrorController, que muestra "No se pudo conectar con el servidor (API)".
                using var respuesta = await _http.SendAsync(peticion, cancellationToken);
                return new RespuestaApi<T>(respuesta.StatusCode, await LeerAsync<T>(respuesta, cancellationToken));
            }
        }

        private static async Task<T?> LeerAsync<T>(HttpResponseMessage respuesta, CancellationToken cancellationToken)
        {
            if (respuesta.Content.Headers.ContentLength == 0)
                return default;

            try
            {
                return await respuesta.Content.ReadFromJsonAsync<T>(cancellationToken);
            }
            catch (Exception ex) when (ex is JsonException or NotSupportedException)
            {
                // La API respondió algo que no es JSON (por ejemplo, texto plano)
                return default;
            }
        }
    }
}
