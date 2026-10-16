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
}
