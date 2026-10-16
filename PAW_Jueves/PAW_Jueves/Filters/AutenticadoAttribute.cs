using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using PAW_Jueves.Models;

namespace PAW_Jueves.Filters
{
    /// <summary>
    /// Filtro para las pantallas que requieren haber iniciado sesión. Se pone encima de una acción
    /// o de un controlador completo: [Autenticado].
    ///
    /// Se ejecuta ANTES de la acción:
    ///  - Si en la sesión no hay IdUsuario (nunca inició sesión, cerró sesión o la sesión venció
    ///    por inactividad), redirige al Login.
    ///  - Si la pantalla está bloqueada (Lock Screen), redirige a la pantalla de desbloqueo, salvo en
    ///    las acciones marcadas con [Autenticado(PermitirBloqueado = true)] (las del propio Lock Screen).
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public sealed class AutenticadoAttribute : ActionFilterAttribute
    {
        /// <summary>True solo para las acciones del Lock Screen, que deben funcionar con la pantalla bloqueada.</summary>
        public bool PermitirBloqueado { get; set; }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var sesion = context.HttpContext.Session;

            if (sesion.GetInt32(ClavesSesion.IdUsuario) == null)
            {
                context.Result = new RedirectToActionResult("Login", "Home", null);
                return;
            }

            if (!PermitirBloqueado && sesion.GetInt32(ClavesSesion.Bloqueado) == 1)
                context.Result = new RedirectToActionResult("LockScreen", "Bloqueo", null);
        }
    }
}
