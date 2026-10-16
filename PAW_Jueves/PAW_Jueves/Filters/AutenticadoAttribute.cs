using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using PAW_Jueves.Models;

namespace PAW_Jueves.Filters
{
    /// <summary>
    /// Filtro para las pantallas que requieren haber iniciado sesión. Se pone encima de una acción
    /// o de un controlador completo: [Autenticado].
    ///
    /// Se ejecuta ANTES de la acción: si en la sesión no hay IdUsuario (nunca inició sesión, cerró
    /// sesión o la sesión venció por inactividad), redirige al Login y la acción no se ejecuta.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public sealed class AutenticadoAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var idUsuario = context.HttpContext.Session.GetInt32(ClavesSesion.IdUsuario);

            if (idUsuario == null)
                context.Result = new RedirectToActionResult("Login", "Home", null);
        }
    }
}
