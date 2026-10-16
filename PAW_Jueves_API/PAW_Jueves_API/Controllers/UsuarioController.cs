using System.Data;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using PAW_Jueves_API.Models;
using PAW_Jueves_API.Services;

namespace PAW_Jueves_API.Controllers
{
    /// <summary>
    /// Endpoints de la cuenta del usuario que YA inició sesión. [Authorize] hace que todos exijan
    /// el header "Authorization: Bearer {token}"; sin token válido la API responde 401 sola.
    ///
    /// Regla de seguridad: el IdUsuario se toma del token (User.ObtenerIdUsuario()), nunca de la
    /// URL ni del cuerpo. Así un usuario no puede ver ni cambiar datos de otro.
    /// </summary>
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class UsuarioController(IConfiguration _configuration, IHashService _hash) : ControllerBase
    {
        /// <summary>GET /api/Usuario/Perfil. Datos básicos del usuario autenticado.</summary>
        /// <returns>200 con el perfil; 404 si el usuario ya no existe o está inactivo; 401 sin token.</returns>
        [HttpGet("Perfil")]
        public async Task<IActionResult> Perfil(CancellationToken cancellationToken)
        {
            await using var context = new SqlConnection(_configuration.GetValue<string>("Variables:ConnectionString"));

            var comando = new CommandDefinition(
                "sp_ConsultarUsuario",
                new { IdUsuario = User.ObtenerIdUsuario() },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken);

            var perfil = await context.QueryFirstOrDefaultAsync<PerfilResponse>(comando);

            if (perfil == null)
                return NotFound(new ResponseApi { Mensaje = "No se encontró el usuario" });

            return Ok(perfil);
        }

        /// <summary>PUT /api/Usuario/Perfil. Cambia el nombre y el teléfono del usuario autenticado.</summary>
        /// <returns>200 si se guardó; 400 si los datos no cumplen las validaciones; 404 si el usuario no existe.</returns>
        [HttpPut("Perfil")]
        public async Task<IActionResult> ActualizarPerfil(ActualizarPerfilRequest model, CancellationToken cancellationToken)
        {
            var filas = await EjecutarAsync("sp_ActualizarPerfil",
                new { IdUsuario = User.ObtenerIdUsuario(), model.NombreCompleto, model.Telefono }, cancellationToken);

            return filas > 0
                ? Ok(new ResponseApi { Mensaje = "Se actualizaron sus datos" })
                : NotFound(new ResponseApi { Mensaje = "No se encontró el usuario" });
        }

        /// <summary>PUT /api/Usuario/Preferencias. Cambia la moneda preferida (CRC, USD o EUR).</summary>
        [HttpPut("Preferencias")]
        public async Task<IActionResult> ActualizarPreferencias(PreferenciasRequest model, CancellationToken cancellationToken)
        {
            var filas = await EjecutarAsync("sp_ActualizarPreferencias",
                new { IdUsuario = User.ObtenerIdUsuario(), model.Moneda }, cancellationToken);

            return filas > 0
                ? Ok(new ResponseApi { Mensaje = "Se guardaron sus preferencias" })
                : NotFound(new ResponseApi { Mensaje = "No se encontró el usuario" });
        }

        /// <summary>
        /// PUT /api/Usuario/Foto. Guarda la ruta de la foto nueva (el archivo ya lo guardó la WEB)
        /// y devuelve la ruta de la anterior para que la WEB borre ese archivo.
        /// </summary>
        [HttpPut("Foto")]
        public async Task<IActionResult> ActualizarFoto(ActualizarFotoRequest model, CancellationToken cancellationToken)
        {
            await using var context = new SqlConnection(_configuration.GetValue<string>("Variables:ConnectionString"));

            // OUTPUT deleted.FotoPerfil en el procedimiento devuelve una fila con la foto anterior;
            // si no hay fila, el usuario no existe o está inactivo.
            var fila = await context.QueryFirstOrDefaultAsync<FotoResponse>(new CommandDefinition(
                "sp_ActualizarFoto",
                new { IdUsuario = User.ObtenerIdUsuario(), model.FotoPerfil },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));

            if (fila == null)
                return NotFound(new FotoResponse { Mensaje = "No se encontró el usuario" });

            fila.Mensaje = "Se actualizó su foto de perfil";
            return Ok(fila);
        }

        /// <summary>
        /// PUT /api/Usuario/Contrasenna. Cambia la contraseña: primero verifica la actual con BCrypt
        /// y después guarda el hash de la nueva.
        /// </summary>
        /// <returns>
        /// 200 si se cambió; 422 si la contraseña actual no es correcta o la nueva es igual a la actual;
        /// 400 si la nueva tiene menos de 8 caracteres.
        /// </returns>
        [HttpPut("Contrasenna")]
        public async Task<IActionResult> CambiarContrasenna(CambiarContrasennaRequest model, CancellationToken cancellationToken)
        {
            var idUsuario = User.ObtenerIdUsuario();
            await using var context = new SqlConnection(_configuration.GetValue<string>("Variables:ConnectionString"));

            var guardada = await context.QueryFirstOrDefaultAsync<string>(new CommandDefinition(
                "sp_ConsultarContrasenna",
                new { IdUsuario = idUsuario },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));

            if (guardada == null)
                return NotFound(new ResponseApi { Mensaje = "No se encontró el usuario" });

            if (_hash.Verificar(model.ContrasennaActual, guardada) == ResultadoVerificacion.Incorrecta)
                return UnprocessableEntity(new ResponseApi { Mensaje = "La contraseña actual no es correcta" });

            if (model.ContrasennaNueva == model.ContrasennaActual)
                return UnprocessableEntity(new ResponseApi { Mensaje = "La contraseña nueva debe ser distinta de la actual" });

            await context.ExecuteAsync(new CommandDefinition(
                "sp_ActualizarContrasenna",
                new { IdUsuario = idUsuario, Contrasenna = _hash.Hashear(model.ContrasennaNueva) },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));

            return Ok(new ResponseApi { Mensaje = "Se cambió su contraseña" });
        }

        /// <summary>Ejecuta un procedimiento que no devuelve filas y retorna cuántas filas cambió.</summary>
        private async Task<int> EjecutarAsync(string procedimiento, object parametros, CancellationToken cancellationToken)
        {
            await using var context = new SqlConnection(_configuration.GetValue<string>("Variables:ConnectionString"));

            return await context.ExecuteAsync(new CommandDefinition(
                procedimiento, parametros,
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));
        }
    }
}
