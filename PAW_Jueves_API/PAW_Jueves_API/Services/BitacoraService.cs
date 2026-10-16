using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;

namespace PAW_Jueves_API.Services
{
    /// <summary>Implementación de <see cref="IBitacoraService"/> con Dapper y sp_RegistrarBitacora.</summary>
    public sealed class BitacoraService(
        IConfiguration _configuration,
        IHttpContextAccessor _accesor,
        ILogger<BitacoraService> _logger) : IBitacoraService
    {
        /// <summary>
        /// Header con la IP del navegador del usuario. La API normalmente recibe las peticiones de la
        /// WEB (no del navegador), así que la WEB manda aquí la IP real. Es solo informativa: un
        /// cliente que llame a la API directamente podría poner cualquier valor.
        /// </summary>
        public const string HeaderIpCliente = "X-Cliente-Ip";

        public async Task RegistrarAsync(int idUsuario, string accion, string? detalle = null, CancellationToken cancellationToken = default)
        {
            try
            {
                await using var context = new SqlConnection(_configuration.GetValue<string>("Variables:ConnectionString"));

                await context.ExecuteAsync(new CommandDefinition(
                    "sp_RegistrarBitacora",
                    new { IdUsuario = idUsuario, Accion = accion, Detalle = Recortar(detalle, 250), Ip = Recortar(ObtenerIp(), 45) },
                    commandType: CommandType.StoredProcedure,
                    cancellationToken: cancellationToken));
            }
            catch (Exception ex) when (ex is SqlException or InvalidOperationException)
            {
                _logger.LogWarning(ex, "No se pudo registrar {Accion} en la bitácora del usuario {IdUsuario}", accion, idUsuario);
            }
        }

        private string? ObtenerIp()
        {
            var http = _accesor.HttpContext;
            if (http == null)
                return null;

            var desdeWeb = http.Request.Headers[HeaderIpCliente].ToString();
            return string.IsNullOrWhiteSpace(desdeWeb) ? http.Connection.RemoteIpAddress?.ToString() : desdeWeb;
        }

        private static string? Recortar(string? texto, int maximo) =>
            texto == null || texto.Length <= maximo ? texto : texto[..maximo];
    }
}
