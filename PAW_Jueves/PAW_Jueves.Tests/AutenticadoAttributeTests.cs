using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Session;
using PAW_Jueves.Filters;
using PAW_Jueves.Models;

namespace PAW_Jueves.Tests
{
    /// <summary>Pruebas del filtro [Autenticado]: sin sesión, con sesión y con la pantalla bloqueada.</summary>
    public class AutenticadoAttributeTests
    {
        /// <summary>Sesión en memoria para las pruebas (la real necesita el middleware de ASP.NET Core).</summary>
        private sealed class SesionFalsa : ISession
        {
            private readonly Dictionary<string, byte[]> _datos = new();
            public bool IsAvailable => true;
            public string Id => "prueba";
            public IEnumerable<string> Keys => _datos.Keys;
            public void Clear() => _datos.Clear();
            public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
            public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
            public void Remove(string key) => _datos.Remove(key);
            public void Set(string key, byte[] value) => _datos[key] = value;
            public bool TryGetValue(string key, out byte[] value) => _datos.TryGetValue(key, out value!);
        }

        private static ActionExecutingContext Contexto(Action<ISession>? preparar = null)
        {
            var sesion = new SesionFalsa();
            preparar?.Invoke(sesion);

            var http = new DefaultHttpContext();
            http.Features.Set<ISessionFeature>(new SessionFeature { Session = sesion });

            var accion = new ActionContext(http, new RouteData(), new ActionDescriptor());
            return new ActionExecutingContext(accion, [], new Dictionary<string, object?>(), controller: new object());
        }

        private static void Iniciada(ISession s) => s.SetInt32(ClavesSesion.IdUsuario, 1004);

        [Fact]
        public void SinSesion_RedirigeAlLogin()
        {
            var contexto = Contexto();

            new AutenticadoAttribute().OnActionExecuting(contexto);

            var redireccion = Assert.IsType<RedirectToActionResult>(contexto.Result);
            Assert.Equal(("Login", "Home"), (redireccion.ActionName, redireccion.ControllerName));
        }

        [Fact]
        public void ConSesion_DejaPasar()
        {
            var contexto = Contexto(Iniciada);

            new AutenticadoAttribute().OnActionExecuting(contexto);

            Assert.Null(contexto.Result);
        }

        [Fact]
        public void PantallaBloqueada_RedirigeAlLockScreen()
        {
            var contexto = Contexto(s => { Iniciada(s); s.SetInt32(ClavesSesion.Bloqueado, 1); });

            new AutenticadoAttribute().OnActionExecuting(contexto);

            var redireccion = Assert.IsType<RedirectToActionResult>(contexto.Result);
            Assert.Equal(("LockScreen", "Bloqueo"), (redireccion.ActionName, redireccion.ControllerName));
        }

        [Fact]
        public void PantallaBloqueada_EnElPropioLockScreen_DejaPasar()
        {
            var contexto = Contexto(s => { Iniciada(s); s.SetInt32(ClavesSesion.Bloqueado, 1); });

            new AutenticadoAttribute { PermitirBloqueado = true }.OnActionExecuting(contexto);

            Assert.Null(contexto.Result);
        }

        [Fact]
        public void PermitirBloqueado_SinSesion_IgualRedirigeAlLogin()
        {
            var contexto = Contexto();

            new AutenticadoAttribute { PermitirBloqueado = true }.OnActionExecuting(contexto);

            Assert.Equal("Login", Assert.IsType<RedirectToActionResult>(contexto.Result).ActionName);
        }
    }
}
