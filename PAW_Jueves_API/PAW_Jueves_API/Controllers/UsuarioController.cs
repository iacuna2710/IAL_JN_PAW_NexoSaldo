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
    public class UsuarioController(IConfiguration _configuration) : ControllerBase
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
    }
}
