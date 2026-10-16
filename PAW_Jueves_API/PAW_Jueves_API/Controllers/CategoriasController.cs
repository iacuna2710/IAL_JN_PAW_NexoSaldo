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
    /// CRUD de categorías del usuario autenticado (api/categorias). Rutas REST: el recurso va en
    /// plural y la acción la da el verbo HTTP (GET lista, POST crea, PUT edita, DELETE borra).
    /// El IdUsuario sale del token: un usuario nunca ve ni toca categorías de otro (404).
    /// </summary>
    [ApiController]
    [Authorize]
    [Route("api/categorias")]
    public class CategoriasController(IConfiguration _configuration) : ControllerBase
    {
        private SqlConnection Conexion() => new(_configuration.GetValue<string>("Variables:ConnectionString"));

        /// <summary>GET /api/categorias?incluirInactivas=false. La primera vez crea las categorías por defecto.</summary>
        [HttpGet]
        public async Task<IActionResult> Listar([FromQuery] bool incluirInactivas = false, CancellationToken cancellationToken = default)
        {
            await using var context = Conexion();
            var categorias = await context.QueryAsync<CategoriaResponse>(new CommandDefinition(
                "sp_ConsultarCategorias",
                new { IdUsuario = User.ObtenerIdUsuario(), IncluirInactivas = incluirInactivas },
                commandType: CommandType.StoredProcedure, cancellationToken: cancellationToken));

            return Ok(categorias);
        }

        /// <summary>GET /api/categorias/{id}. 404 si no existe o es de otro usuario.</summary>
        [HttpGet("{id:int}")]
        public async Task<IActionResult> Obtener(int id, CancellationToken cancellationToken)
        {
            var categoria = await ConsultarAsync(id, cancellationToken);
            return categoria == null ? NotFound(new ResponseApi { Mensaje = "No se encontró la categoría" }) : Ok(categoria);
        }

        /// <summary>POST /api/categorias. 201 con la categoría creada; 409 si el nombre ya existe para ese tipo.</summary>
        [HttpPost]
        public async Task<IActionResult> Crear(CrearCategoriaRequest model, CancellationToken cancellationToken)
        {
            await using var context = Conexion();
            var id = await context.QuerySingleAsync<int>(new CommandDefinition(
                "sp_CrearCategoria",
                new { IdUsuario = User.ObtenerIdUsuario(), Nombre = model.Nombre.Trim(), model.Tipo, model.Color, model.PresupuestoMensual },
                commandType: CommandType.StoredProcedure, cancellationToken: cancellationToken));

            if (id < 0)
                return Conflict(new ResponseApi { Mensaje = $"Ya existe una categoría llamada {model.Nombre.Trim()}" });

            return CreatedAtAction(nameof(Obtener), new { id }, await ConsultarAsync(id, cancellationToken));
        }

        /// <summary>PUT /api/categorias/{id}. Cambia nombre, color, presupuesto y si está activa.</summary>
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Actualizar(int id, ActualizarCategoriaRequest model, CancellationToken cancellationToken)
        {
            await using var context = Conexion();
            var resultado = await context.QuerySingleAsync<int>(new CommandDefinition(
                "sp_ActualizarCategoria",
                new { IdUsuario = User.ObtenerIdUsuario(), IdCategoria = id, Nombre = model.Nombre.Trim(), model.Color, model.PresupuestoMensual, model.Estado },
                commandType: CommandType.StoredProcedure, cancellationToken: cancellationToken));

            return resultado switch
            {
                0 => NotFound(new ResponseApi { Mensaje = "No se encontró la categoría" }),
                -1 => Conflict(new ResponseApi { Mensaje = $"Ya existe una categoría llamada {model.Nombre.Trim()}" }),
                _ => Ok(new ResponseApi { Mensaje = "Se actualizó la categoría" })
            };
        }

        /// <summary>
        /// DELETE /api/categorias/{id}. Si tiene movimientos se DESACTIVA en lugar de borrarse, para no
        /// perder el historial; el mensaje lo indica.
        /// </summary>
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Eliminar(int id, CancellationToken cancellationToken)
        {
            await using var context = Conexion();
            var resultado = await context.QuerySingleAsync<string>(new CommandDefinition(
                "sp_EliminarCategoria",
                new { IdUsuario = User.ObtenerIdUsuario(), IdCategoria = id },
                commandType: CommandType.StoredProcedure, cancellationToken: cancellationToken));

            return resultado switch
            {
                "N" => NotFound(new ResponseApi { Mensaje = "No se encontró la categoría" }),
                "D" => Ok(new ResponseApi { Mensaje = "La categoría tiene movimientos, así que se desactivó en lugar de eliminarse" }),
                _ => Ok(new ResponseApi { Mensaje = "Se eliminó la categoría" })
            };
        }

        private async Task<CategoriaResponse?> ConsultarAsync(int id, CancellationToken cancellationToken)
        {
            await using var context = Conexion();
            return await context.QueryFirstOrDefaultAsync<CategoriaResponse>(new CommandDefinition(
                "sp_ConsultarCategoria",
                new { IdUsuario = User.ObtenerIdUsuario(), IdCategoria = id },
                commandType: CommandType.StoredProcedure, cancellationToken: cancellationToken));
        }
    }
}
