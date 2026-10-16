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
    /// CRUD de ingresos y gastos del usuario autenticado (api/movimientos).
    /// Las reglas de negocio están en ReglasGastos (se prueban por separado); este controlador solo
    /// consulta la BD, aplica esas reglas y responde con el código HTTP que corresponde:
    /// 201 al crear, 204 al borrar, 404 si no existe o es de otro usuario, 422 si viola una regla.
    /// </summary>
    [ApiController]
    [Authorize]
    [Route("api/movimientos")]
    public class MovimientosController(IConfiguration _configuration, IBitacoraService _bitacora) : ControllerBase
    {
        private SqlConnection Conexion() => new(_configuration.GetValue<string>("Variables:ConnectionString"));

        /// <summary>
        /// GET /api/movimientos?anno=2026&amp;mes=10&amp;tipo=G&amp;idCategoria=3&amp;pagina=1&amp;tamannoPagina=20.
        /// Si no se indica año y mes, usa el mes actual. Máximo 100 filas por página.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Listar(
            [FromQuery] int? anno, [FromQuery] int? mes, [FromQuery] string? tipo, [FromQuery] int? idCategoria,
            [FromQuery] int pagina = 1, [FromQuery] int tamannoPagina = 20, CancellationToken cancellationToken = default)
        {
            var hoy = ReglasGastos.HoyCostaRica();
            var (a, m) = (anno ?? hoy.Year, mes ?? hoy.Month);
            if (m is < 1 or > 12 || a is < 2000 or > 2100)
                return BadRequest(new ResponseApi { Mensaje = "Año o mes no válido" });
            if (tipo is not (null or "I" or "G"))
                return BadRequest(new ResponseApi { Mensaje = "El tipo debe ser I o G" });

            pagina = Math.Max(1, pagina);
            tamannoPagina = Math.Clamp(tamannoPagina, 1, 100);

            await using var context = Conexion();
            var filas = (await context.QueryAsync<MovimientoPaginadoFila>(new CommandDefinition(
                "sp_ConsultarMovimientos",
                new { IdUsuario = User.ObtenerIdUsuario(), Anno = a, Mes = m, Tipo = tipo, IdCategoria = idCategoria, Pagina = pagina, TamannoPagina = tamannoPagina },
                commandType: CommandType.StoredProcedure, cancellationToken: cancellationToken))).ToList();

            return Ok(new PaginaResponse<MovimientoResponse>
            {
                Datos = filas,
                Total = filas.FirstOrDefault()?.TotalFilas ?? 0,
                Pagina = pagina,
                TamannoPagina = tamannoPagina
            });
        }

        /// <summary>GET /api/movimientos/{id}.</summary>
        [HttpGet("{id:int}")]
        public async Task<IActionResult> Obtener(int id, CancellationToken cancellationToken)
        {
            var movimiento = await ConsultarAsync(id, cancellationToken);
            return movimiento == null ? NotFound(new ResponseApi { Mensaje = "No se encontró el movimiento" }) : Ok(movimiento);
        }

        /// <summary>POST /api/movimientos. 201 con el Id; incluye la alerta de presupuesto si aplica.</summary>
        [HttpPost]
        public async Task<IActionResult> Crear(MovimientoRequest model, CancellationToken cancellationToken)
        {
            var categoria = await ConsultarCategoriaAsync(model.IdCategoria, cancellationToken);
            var error = ReglasGastos.ValidarMovimiento(model, categoria, ReglasGastos.HoyCostaRica());
            if (error != null)
                return UnprocessableEntity(new GuardarMovimientoResponse { Mensaje = error });

            await using var context = Conexion();
            var id = await context.QuerySingleAsync<int>(new CommandDefinition(
                "sp_CrearMovimiento",
                new { IdUsuario = User.ObtenerIdUsuario(), model.IdCategoria, model.Tipo, model.Monto, Fecha = model.Fecha.ToDateTime(TimeOnly.MinValue), model.Descripcion },
                commandType: CommandType.StoredProcedure, cancellationToken: cancellationToken));

            await _bitacora.RegistrarAsync(User.ObtenerIdUsuario(), "MovimientoCreado",
                Detalle(model.Tipo, model.Monto, categoria!.Nombre), cancellationToken);

            var respuesta = await ConAlertaAsync(new GuardarMovimientoResponse { IdMovimiento = id, Mensaje = "Se registró el movimiento" },
                model, categoria, cancellationToken);
            return CreatedAtAction(nameof(Obtener), new { id }, respuesta);
        }

        /// <summary>PUT /api/movimientos/{id}. Mismas reglas que al crear.</summary>
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Actualizar(int id, MovimientoRequest model, CancellationToken cancellationToken)
        {
            if (await ConsultarAsync(id, cancellationToken) == null)
                return NotFound(new GuardarMovimientoResponse { Mensaje = "No se encontró el movimiento" });

            var categoria = await ConsultarCategoriaAsync(model.IdCategoria, cancellationToken);
            var error = ReglasGastos.ValidarMovimiento(model, categoria, ReglasGastos.HoyCostaRica());
            if (error != null)
                return UnprocessableEntity(new GuardarMovimientoResponse { Mensaje = error });

            await using var context = Conexion();
            await context.ExecuteAsync(new CommandDefinition(
                "sp_ActualizarMovimiento",
                new { IdUsuario = User.ObtenerIdUsuario(), IdMovimiento = id, model.IdCategoria, model.Tipo, model.Monto, Fecha = model.Fecha.ToDateTime(TimeOnly.MinValue), model.Descripcion },
                commandType: CommandType.StoredProcedure, cancellationToken: cancellationToken));

            var respuesta = await ConAlertaAsync(new GuardarMovimientoResponse { IdMovimiento = id, Mensaje = "Se actualizó el movimiento" },
                model, categoria!, cancellationToken);
            return Ok(respuesta);
        }

        /// <summary>DELETE /api/movimientos/{id}. 204 si se borró; 404 si no existe o es de otro usuario.</summary>
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Eliminar(int id, CancellationToken cancellationToken)
        {
            var movimiento = await ConsultarAsync(id, cancellationToken);
            if (movimiento == null)
                return NotFound(new ResponseApi { Mensaje = "No se encontró el movimiento" });

            await using var context = Conexion();
            await context.ExecuteAsync(new CommandDefinition(
                "sp_EliminarMovimiento",
                new { IdUsuario = User.ObtenerIdUsuario(), IdMovimiento = id },
                commandType: CommandType.StoredProcedure, cancellationToken: cancellationToken));

            await _bitacora.RegistrarAsync(User.ObtenerIdUsuario(), "MovimientoEliminado",
                Detalle(movimiento.Tipo, movimiento.Monto, movimiento.Categoria), cancellationToken);

            return NoContent();
        }

        /// <summary>Si el movimiento es un gasto en una categoría con presupuesto, calcula la alerta del mes.</summary>
        private async Task<GuardarMovimientoResponse> ConAlertaAsync(GuardarMovimientoResponse respuesta, MovimientoRequest model,
            CategoriaResponse categoria, CancellationToken cancellationToken)
        {
            if (model.Tipo != "G" || categoria.PresupuestoMensual == null)
                return respuesta;

            await using var context = Conexion();
            var gastado = await context.QuerySingleAsync<decimal>(new CommandDefinition(
                "sp_GastoCategoriaMes",
                new { IdUsuario = User.ObtenerIdUsuario(), model.IdCategoria, Anno = model.Fecha.Year, Mes = model.Fecha.Month },
                commandType: CommandType.StoredProcedure, cancellationToken: cancellationToken));

            respuesta.Alerta = ReglasGastos.AlertaPresupuesto(gastado, categoria.PresupuestoMensual);
            var aviso = ReglasGastos.MensajeAlerta(respuesta.Alerta, categoria.Nombre, gastado, categoria.PresupuestoMensual);
            if (aviso != null)
                respuesta.Mensaje += ". " + aviso;

            return respuesta;
        }

        /// <summary>Texto para la bitácora. Ejemplo: "Gasto de 35 000,00 en Comida".</summary>
        private static string Detalle(string tipo, decimal monto, string categoria) =>
            string.Format(System.Globalization.CultureInfo.GetCultureInfo("es-CR"), "{0} de {1:N2} en {2}",
                tipo == "G" ? "Gasto" : "Ingreso", monto, categoria);

        private async Task<MovimientoResponse?> ConsultarAsync(int id, CancellationToken cancellationToken)
        {
            await using var context = Conexion();
            return await context.QueryFirstOrDefaultAsync<MovimientoResponse>(new CommandDefinition(
                "sp_ConsultarMovimiento",
                new { IdUsuario = User.ObtenerIdUsuario(), IdMovimiento = id },
                commandType: CommandType.StoredProcedure, cancellationToken: cancellationToken));
        }

        private async Task<CategoriaResponse?> ConsultarCategoriaAsync(int idCategoria, CancellationToken cancellationToken)
        {
            await using var context = Conexion();
            return await context.QueryFirstOrDefaultAsync<CategoriaResponse>(new CommandDefinition(
                "sp_ConsultarCategoria",
                new { IdUsuario = User.ObtenerIdUsuario(), IdCategoria = idCategoria },
                commandType: CommandType.StoredProcedure, cancellationToken: cancellationToken));
        }
    }
}
