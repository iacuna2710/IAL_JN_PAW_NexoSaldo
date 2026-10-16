using System.Data;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
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
    public class UsuarioController(IConfiguration _configuration, IHashService _hash, IBitacoraService _bitacora) : ControllerBase
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

            if (filas > 0)
                await _bitacora.RegistrarAsync(User.ObtenerIdUsuario(), AccionesBitacora.CambioPerfil, cancellationToken: cancellationToken);

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

            if (filas > 0)
                await _bitacora.RegistrarAsync(User.ObtenerIdUsuario(), AccionesBitacora.CambioPreferencias, $"Moneda: {model.Moneda}", cancellationToken);

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

            await _bitacora.RegistrarAsync(User.ObtenerIdUsuario(), AccionesBitacora.CambioFoto, cancellationToken: cancellationToken);

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
            {
                await _bitacora.RegistrarAsync(idUsuario, AccionesBitacora.CambioContrasennaFallido, "Contraseña actual incorrecta", cancellationToken);
                return UnprocessableEntity(new ResponseApi { Mensaje = "La contraseña actual no es correcta" });
            }

            if (model.ContrasennaNueva == model.ContrasennaActual)
                return UnprocessableEntity(new ResponseApi { Mensaje = "La contraseña nueva debe ser distinta de la actual" });

            await context.ExecuteAsync(new CommandDefinition(
                "sp_ActualizarContrasenna",
                new { IdUsuario = idUsuario, Contrasenna = _hash.Hashear(model.ContrasennaNueva) },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));

            await _bitacora.RegistrarAsync(idUsuario, AccionesBitacora.CambioContrasenna, cancellationToken: cancellationToken);

            return Ok(new ResponseApi { Mensaje = "Se cambió su contraseña" });
        }

        /// <summary>GET /api/Usuario/Actividad?top=10. Últimas acciones del usuario (más recientes primero).</summary>
        /// <param name="top">Cuántas filas devolver, entre 1 y 50.</param>
        [HttpGet("Actividad")]
        public async Task<IActionResult> Actividad([FromQuery] int top = 10, CancellationToken cancellationToken = default)
        {
            await using var context = new SqlConnection(_configuration.GetValue<string>("Variables:ConnectionString"));

            var filas = await context.QueryAsync<ActividadResponse>(new CommandDefinition(
                "sp_ConsultarBitacora",
                new { IdUsuario = User.ObtenerIdUsuario(), Top = Math.Clamp(top, 1, 50) },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));

            // SQL Server devuelve la fecha sin zona horaria; se marca como UTC para que el JSON lleve la "Z"
            foreach (var fila in filas)
                fila.Fecha = DateTime.SpecifyKind(fila.Fecha, DateTimeKind.Utc);

            return Ok(filas);
        }

        /// <summary>
        /// POST /api/Usuario/Bloquear. La WEB avisa que el usuario bloqueó la pantalla; aquí solo se
        /// anota en la bitácora (el bloqueo en sí vive en la sesión de la WEB).
        /// </summary>
        [HttpPost("Bloquear")]
        public async Task<IActionResult> Bloquear(CancellationToken cancellationToken)
        {
            await _bitacora.RegistrarAsync(User.ObtenerIdUsuario(), AccionesBitacora.Bloqueo, cancellationToken: cancellationToken);
            return NoContent();
        }

        /// <summary>
        /// POST /api/Usuario/VerificarContrasenna. Se usa para desbloquear la pantalla: confirma que
        /// quien está frente al computador conoce la contraseña del usuario de la sesión.
        /// </summary>
        /// <returns>200 si la contraseña es correcta; 422 si no.</returns>
        [HttpPost("VerificarContrasenna")]
        [EnableRateLimiting("credenciales")]
        public async Task<IActionResult> VerificarContrasenna(VerificarContrasennaRequest model, CancellationToken cancellationToken)
        {
            var idUsuario = User.ObtenerIdUsuario();
            await using var context = new SqlConnection(_configuration.GetValue<string>("Variables:ConnectionString"));

            var guardada = await context.QueryFirstOrDefaultAsync<string>(new CommandDefinition(
                "sp_ConsultarContrasenna",
                new { IdUsuario = idUsuario },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));

            if (guardada == null || _hash.Verificar(model.Contrasenna, guardada) == ResultadoVerificacion.Incorrecta)
            {
                await _bitacora.RegistrarAsync(idUsuario, AccionesBitacora.DesbloqueoFallido, cancellationToken: cancellationToken);
                return UnprocessableEntity(new ResponseApi { Mensaje = "La contraseña no es correcta" });
            }

            await _bitacora.RegistrarAsync(idUsuario, AccionesBitacora.Desbloqueo, cancellationToken: cancellationToken);
            return Ok(new ResponseApi { Mensaje = "Pantalla desbloqueada" });
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
