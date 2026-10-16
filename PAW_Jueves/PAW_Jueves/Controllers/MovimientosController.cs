using System.Net;
using Microsoft.AspNetCore.Mvc;
using PAW_Jueves.Filters;
using PAW_Jueves.Models;
using PAW_Jueves.Services;

namespace PAW_Jueves.Controllers
{
    /// <summary>
    /// Ingresos y gastos: lista del mes con filtros y paginación, crear, editar y eliminar.
    /// Las reglas de negocio (monto, fecha, categoría, presupuesto) las valida la API; aquí solo se
    /// muestran sus mensajes. Si un gasto llega al 80 % o al 100 % del presupuesto de su categoría,
    /// la API devuelve una alerta que se muestra en amarillo.
    /// </summary>
    [Autenticado]
    public class MovimientosController(ApiClient _api) : Controller
    {
        private string Moneda => HttpContext.Session.GetString(ClavesSesion.Moneda) ?? "CRC";

        /// <summary>GET /Movimientos?periodo=2026-10&amp;tipo=G&amp;idCategoria=3&amp;pagina=2.</summary>
        [HttpGet]
        public async Task<IActionResult> Index(string? periodo, string? tipo, int? idCategoria, int pagina = 1,
            CancellationToken cancellationToken = default)
        {
            var (anno, mes) = FormatoMoneda.LeerPeriodo(periodo, FormatoMoneda.HoyCostaRica());
            tipo = tipo is "I" or "G" ? tipo : null;

            var ruta = $"movimientos?anno={anno}&mes={mes}&pagina={Math.Max(1, pagina)}&tamannoPagina=20"
                + (tipo != null ? $"&tipo={tipo}" : "")
                + (idCategoria is > 0 ? $"&idCategoria={idCategoria}" : "");

            var movimientosTarea = _api.GetAsync<PaginaModel<MovimientoModel>>(ruta, cancellationToken);
            var categoriasTarea = _api.GetAsync<List<CategoriaModel>>("categorias?incluirInactivas=true", cancellationToken);
            await Task.WhenAll(movimientosTarea, categoriasTarea);

            if (movimientosTarea.Result.Estado == HttpStatusCode.Unauthorized)
                return SesionVencida();

            return View(new MovimientosViewModel
            {
                Anno = anno,
                Mes = mes,
                Tipo = tipo,
                IdCategoria = idCategoria,
                Moneda = Moneda,
                Categorias = categoriasTarea.Result.Datos ?? [],
                Pagina = movimientosTarea.Result.Datos ?? new PaginaModel<MovimientoModel>()
            });
        }

        /// <summary>GET /Movimientos/Crear?tipo=G. Formulario vacío con la fecha de hoy.</summary>
        [HttpGet]
        public async Task<IActionResult> Crear(string? tipo, CancellationToken cancellationToken)
        {
            var formulario = new MovimientoFormModel { Tipo = tipo == "I" ? "I" : "G", Fecha = FormatoMoneda.HoyCostaRica() };
            return await MostrarFormularioAsync(formulario, cancellationToken);
        }

        /// <summary>POST /Movimientos/Crear.</summary>
        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> Crear([Bind(Prefix = "Formulario")] MovimientoFormModel model, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
                return await MostrarFormularioAsync(model, cancellationToken, MensajeValidacion());

            var respuesta = await _api.PostAsync<GuardarMovimientoResponse>("movimientos", CuerpoApi(model), cancellationToken);
            return await DespuesDeGuardarAsync(respuesta, model, cancellationToken);
        }

        /// <summary>GET /Movimientos/Editar/5.</summary>
        [HttpGet]
        public async Task<IActionResult> Editar(int id, CancellationToken cancellationToken)
        {
            var respuesta = await _api.GetAsync<MovimientoModel>($"movimientos/{id}", cancellationToken);
            if (respuesta.Estado == HttpStatusCode.Unauthorized)
                return SesionVencida();

            if (!respuesta.Exitosa || respuesta.Datos == null)
            {
                TempData["Error"] = "No se encontró el movimiento";
                return RedirectToAction(nameof(Index));
            }

            var m = respuesta.Datos;
            return await MostrarFormularioAsync(new MovimientoFormModel
            {
                IdMovimiento = m.IdMovimiento, IdCategoria = m.IdCategoria, Tipo = m.Tipo, Monto = m.Monto,
                Fecha = DateOnly.FromDateTime(m.Fecha), Descripcion = m.Descripcion
            }, cancellationToken);
        }

        /// <summary>POST /Movimientos/Editar/5.</summary>
        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> Editar(int id, [Bind(Prefix = "Formulario")] MovimientoFormModel model, CancellationToken cancellationToken)
        {
            model.IdMovimiento = id;
            if (!ModelState.IsValid)
                return await MostrarFormularioAsync(model, cancellationToken, MensajeValidacion());

            var respuesta = await _api.PutAsync<GuardarMovimientoResponse>($"movimientos/{id}", CuerpoApi(model), cancellationToken);
            return await DespuesDeGuardarAsync(respuesta, model, cancellationToken);
        }

        /// <summary>POST /Movimientos/Eliminar/5. Vuelve a la lista del mismo mes.</summary>
        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> Eliminar(int id, string? periodo, CancellationToken cancellationToken)
        {
            var respuesta = await _api.DeleteAsync<UsuarioResponse>($"movimientos/{id}", cancellationToken);
            if (respuesta.Estado == HttpStatusCode.Unauthorized)
                return SesionVencida();

            if (respuesta.Exitosa)
                TempData["Exito"] = "Se eliminó el movimiento";
            else
                TempData["Error"] = respuesta.Datos?.Mensaje is { Length: > 0 } m ? m : "No se pudo eliminar el movimiento";

            return RedirectToAction(nameof(Index), new { periodo });
        }

        private static object CuerpoApi(MovimientoFormModel model) =>
            new { model.IdCategoria, model.Tipo, model.Monto, model.Fecha, model.Descripcion };

        /// <summary>
        /// Después de crear o editar: si la API rechazó el movimiento (422), vuelve al formulario con
        /// el mensaje; si lo guardó, vuelve a la lista del mes del movimiento (con la alerta, si hay).
        /// </summary>
        private async Task<IActionResult> DespuesDeGuardarAsync(RespuestaApi<GuardarMovimientoResponse> respuesta,
            MovimientoFormModel model, CancellationToken cancellationToken)
        {
            if (respuesta.Estado == HttpStatusCode.Unauthorized)
                return SesionVencida();

            if (!respuesta.Exitosa)
                return await MostrarFormularioAsync(model, cancellationToken,
                    respuesta.Datos?.Mensaje is { Length: > 0 } m ? m : "No se pudo guardar el movimiento");

            if (respuesta.Datos?.Alerta != null)
                TempData["Advertencia"] = respuesta.Datos.Mensaje;
            else
                TempData["Exito"] = respuesta.Datos?.Mensaje ?? "Se guardó el movimiento";

            return RedirectToAction(nameof(Index), new { periodo = model.Fecha.ToString("yyyy-MM") });
        }

        private async Task<IActionResult> MostrarFormularioAsync(MovimientoFormModel formulario, CancellationToken cancellationToken, string? error = null)
        {
            var categorias = await _api.GetAsync<List<CategoriaModel>>("categorias", cancellationToken);
            if (categorias.Estado == HttpStatusCode.Unauthorized)
                return SesionVencida();

            ViewBag.Mensaje = error;
            return View("Formulario", new MovimientoFormularioViewModel
            {
                Formulario = formulario,
                Categorias = categorias.Datos ?? [],
                Moneda = Moneda
            });
        }

        private string MensajeValidacion() => string.Join(" ", ModelState.Values.SelectMany(v => v.Errors)
            .Select(e => e.ErrorMessage).Where(m => !string.IsNullOrWhiteSpace(m)).Distinct());

        private IActionResult SesionVencida()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login", "Home");
        }
    }
}
