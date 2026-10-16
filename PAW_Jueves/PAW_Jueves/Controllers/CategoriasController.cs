using System.Net;
using Microsoft.AspNetCore.Mvc;
using PAW_Jueves.Filters;
using PAW_Jueves.Models;
using PAW_Jueves.Services;

namespace PAW_Jueves.Controllers
{
    /// <summary>
    /// Categorías de ingresos y gastos, con su color y presupuesto mensual (solo gastos).
    /// Los formularios siguen Post/Redirect/Get con mensajes en TempData (ver _Mensajes).
    /// </summary>
    [Autenticado]
    public class CategoriasController(ApiClient _api) : Controller
    {
        /// <summary>GET /Categorias. Lista (incluye las desactivadas) y formulario de nueva categoría.</summary>
        [HttpGet]
        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var respuesta = await _api.GetAsync<List<CategoriaModel>>("categorias?incluirInactivas=true", cancellationToken);
            if (respuesta.Estado == HttpStatusCode.Unauthorized)
                return SesionVencida();

            ViewBag.Moneda = HttpContext.Session.GetString(ClavesSesion.Moneda) ?? "CRC";
            return View(respuesta.Datos ?? []);
        }

        /// <summary>POST /Categorias/Crear.</summary>
        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> Crear(CategoriaFormModel model, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
                return VolverConErrores(nameof(Index));

            // Las categorías de ingreso no tienen presupuesto
            var presupuesto = model.Tipo == "G" ? model.PresupuestoMensual : null;
            var respuesta = await _api.PostAsync<UsuarioResponse>("categorias",
                new { model.Nombre, model.Tipo, model.Color, PresupuestoMensual = presupuesto }, cancellationToken);

            if (respuesta.Estado == HttpStatusCode.Unauthorized)
                return SesionVencida();

            if (respuesta.Exitosa)
                TempData["Exito"] = $"Se creó la categoría {model.Nombre}";
            else
                TempData["Error"] = respuesta.Datos?.Mensaje is { Length: > 0 } m ? m : "No se pudo crear la categoría";

            return RedirectToAction(nameof(Index));
        }

        /// <summary>GET /Categorias/Editar/5.</summary>
        [HttpGet]
        public async Task<IActionResult> Editar(int id, CancellationToken cancellationToken)
        {
            var respuesta = await _api.GetAsync<CategoriaModel>($"categorias/{id}", cancellationToken);
            if (respuesta.Estado == HttpStatusCode.Unauthorized)
                return SesionVencida();

            if (!respuesta.Exitosa || respuesta.Datos == null)
            {
                TempData["Error"] = "No se encontró la categoría";
                return RedirectToAction(nameof(Index));
            }

            var c = respuesta.Datos;
            return View(new CategoriaFormModel
            {
                IdCategoria = c.IdCategoria, Nombre = c.Nombre, Tipo = c.Tipo, Color = c.Color,
                PresupuestoMensual = c.PresupuestoMensual, Estado = c.Estado
            });
        }

        /// <summary>POST /Categorias/Editar/5.</summary>
        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> Editar(int id, CategoriaFormModel model, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                VolverConErrores(nameof(Index));
                return RedirectToAction(nameof(Editar), new { id });
            }

            var presupuesto = model.Tipo == "G" ? model.PresupuestoMensual : null;
            var respuesta = await _api.PutAsync<UsuarioResponse>($"categorias/{id}",
                new { model.Nombre, model.Color, PresupuestoMensual = presupuesto, model.Estado }, cancellationToken);

            if (respuesta.Estado == HttpStatusCode.Unauthorized)
                return SesionVencida();

            if (!respuesta.Exitosa)
            {
                TempData["Error"] = respuesta.Datos?.Mensaje is { Length: > 0 } m ? m : "No se pudo guardar la categoría";
                return RedirectToAction(nameof(Editar), new { id });
            }

            TempData["Exito"] = "Se actualizó la categoría";
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// POST /Categorias/Eliminar/5. Si tiene movimientos, la API la desactiva en lugar de borrarla
        /// y el mensaje lo explica.
        /// </summary>
        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> Eliminar(int id, CancellationToken cancellationToken)
        {
            var respuesta = await _api.DeleteAsync<UsuarioResponse>($"categorias/{id}", cancellationToken);
            if (respuesta.Estado == HttpStatusCode.Unauthorized)
                return SesionVencida();

            if (respuesta.Exitosa)
                TempData["Exito"] = respuesta.Datos?.Mensaje ?? "Listo";
            else
                TempData["Error"] = respuesta.Datos?.Mensaje is { Length: > 0 } m ? m : "No se pudo eliminar la categoría";

            return RedirectToAction(nameof(Index));
        }

        private IActionResult SesionVencida()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login", "Home");
        }

        private IActionResult VolverConErrores(string accion)
        {
            TempData["Error"] = string.Join(" ", ModelState.Values.SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage).Where(m => !string.IsNullOrWhiteSpace(m)).Distinct());
            return RedirectToAction(accion);
        }
    }
}
