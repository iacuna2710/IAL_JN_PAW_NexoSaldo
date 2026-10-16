using System.Net;
using Microsoft.AspNetCore.Mvc;
using PAW_Jueves.Filters;
using PAW_Jueves.Models;
using PAW_Jueves.Services;

namespace PAW_Jueves.Controllers
{
    /// <summary>
    /// Pantallas de la cuenta del usuario: Mi cuenta (foto y datos) y Settings (contraseña,
    /// preferencias y tema). [Autenticado] en la clase protege TODAS las acciones.
    ///
    /// Los formularios siguen el patrón Post/Redirect/Get: el POST guarda, deja un mensaje en
    /// TempData ("Exito" o "Error") y redirige al GET. Así, si el usuario recarga la página, el
    /// navegador no vuelve a enviar el formulario.
    /// </summary>
    [Autenticado]
    public class CuentaController(ApiClient _api, IWebHostEnvironment _entorno, ILogger<CuentaController> _logger) : Controller
    {
        /// <summary>Carpeta pública (dentro de wwwroot) donde se guardan las fotos de perfil.</summary>
        private const string RutaWebFotos = "/uploads/perfiles/";

        private string CarpetaFotos => Path.Combine(_entorno.WebRootPath, "uploads", "perfiles");

        #region Mi cuenta

        /// <summary>GET /Cuenta/MiCuenta. Muestra la foto y los datos del usuario.</summary>
        [HttpGet]
        public async Task<IActionResult> MiCuenta(CancellationToken cancellationToken)
        {
            var respuesta = await _api.GetAsync<PerfilModel>("Usuario/Perfil", cancellationToken);

            if (respuesta.Estado == HttpStatusCode.Unauthorized)
                return SesionVencida();

            if (!respuesta.Exitosa || respuesta.Datos == null)
            {
                ViewBag.Mensaje = "No se pudieron cargar sus datos. Intente de nuevo.";
                return View(new PerfilModel());
            }

            var actividad = await _api.GetAsync<List<ActividadModel>>("Usuario/Actividad?top=10", cancellationToken);
            ViewBag.Actividad = actividad.Datos ?? [];

            return View(respuesta.Datos);
        }

        /// <summary>POST /Cuenta/ActualizarDatos. Guarda el nombre y el teléfono.</summary>
        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> ActualizarDatos(ActualizarPerfilModel model, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
                return VolverConErrores(nameof(MiCuenta));

            var respuesta = await _api.PutAsync<UsuarioResponse>("Usuario/Perfil", model, cancellationToken);

            if (respuesta.Estado == HttpStatusCode.Unauthorized)
                return SesionVencida();

            if (respuesta.Exitosa)
            {
                // El nombre de la barra superior sale de la sesión: se actualiza también ahí
                HttpContext.Session.SetString(ClavesSesion.NombreCompleto, model.NombreCompleto);
                TempData["Exito"] = "Se actualizaron sus datos";
            }
            else
            {
                TempData["Error"] = respuesta.Datos?.Mensaje is { Length: > 0 } m ? m : "No se pudieron guardar sus datos";
            }

            return RedirectToAction(nameof(MiCuenta));
        }

        /// <summary>
        /// POST /Cuenta/CambiarFoto. Pasos:
        ///  1. Valida la imagen (tipo real, extensión y tamaño) con ValidadorImagen.
        ///  2. La guarda en wwwroot/uploads/perfiles con un nombre aleatorio ({guid}.ext). Nunca se
        ///     usa el nombre original del archivo (podría traer rutas como "../../Program.cs").
        ///  3. Le pide a la API que guarde la ruta. Si la API falla, se borra el archivo nuevo.
        ///  4. Si todo salió bien, borra el archivo de la foto anterior y actualiza la sesión.
        /// RequestSizeLimit corta la petición si pesa más de 3 MB, antes de leerla completa.
        /// </summary>
        [ValidateAntiForgeryToken]
        [HttpPost]
        [RequestSizeLimit(3 * 1024 * 1024)]
        [RequestFormLimits(MultipartBodyLengthLimit = 3 * 1024 * 1024)]
        public async Task<IActionResult> CambiarFoto(IFormFile? foto, CancellationToken cancellationToken)
        {
            var validacion = await ValidadorImagen.ValidarAsync(foto, cancellationToken);
            if (!validacion.EsValida)
            {
                TempData["Error"] = validacion.Error;
                return RedirectToAction(nameof(MiCuenta));
            }

            Directory.CreateDirectory(CarpetaFotos);
            var nombreArchivo = Guid.NewGuid().ToString("N") + validacion.Extension;
            var rutaFisica = Path.Combine(CarpetaFotos, nombreArchivo);

            await using (var destino = new FileStream(rutaFisica, FileMode.CreateNew))
            {
                await foto!.CopyToAsync(destino, cancellationToken);
            }

            var respuesta = await _api.PutAsync<FotoResponse>("Usuario/Foto",
                new { FotoPerfil = RutaWebFotos + nombreArchivo }, cancellationToken);

            if (!respuesta.Exitosa)
            {
                BorrarFoto(RutaWebFotos + nombreArchivo);

                if (respuesta.Estado == HttpStatusCode.Unauthorized)
                    return SesionVencida();

                TempData["Error"] = "No se pudo guardar la foto. Intente de nuevo.";
                return RedirectToAction(nameof(MiCuenta));
            }

            BorrarFoto(respuesta.Datos?.FotoAnterior);
            HttpContext.Session.SetString(ClavesSesion.FotoPerfil, RutaWebFotos + nombreArchivo);
            TempData["Exito"] = "Se actualizó su foto de perfil";
            return RedirectToAction(nameof(MiCuenta));
        }

        #endregion

        #region Settings

        /// <summary>GET /Cuenta/Settings. Cambiar contraseña, moneda preferida y tema.</summary>
        [HttpGet]
        public async Task<IActionResult> Settings(CancellationToken cancellationToken)
        {
            var respuesta = await _api.GetAsync<PerfilModel>("Usuario/Perfil", cancellationToken);

            if (respuesta.Estado == HttpStatusCode.Unauthorized)
                return SesionVencida();

            return View(new PreferenciasModel { Moneda = respuesta.Datos?.Moneda ?? "CRC" });
        }

        /// <summary>POST /Cuenta/CambiarContrasenna. La API verifica la contraseña actual antes de cambiarla.</summary>
        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> CambiarContrasenna(CambiarContrasennaModel model, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
                return VolverConErrores(nameof(Settings));

            var respuesta = await _api.PutAsync<UsuarioResponse>("Usuario/Contrasenna",
                new { model.ContrasennaActual, model.ContrasennaNueva }, cancellationToken);

            if (respuesta.Estado == HttpStatusCode.Unauthorized)
                return SesionVencida();

            if (respuesta.Exitosa)
                TempData["Exito"] = "Se cambió su contraseña";
            else
                TempData["Error"] = respuesta.Datos?.Mensaje is { Length: > 0 } m ? m : "No se pudo cambiar la contraseña";

            return RedirectToAction(nameof(Settings));
        }

        /// <summary>POST /Cuenta/Preferencias. Guarda la moneda preferida.</summary>
        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> Preferencias(PreferenciasModel model, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
                return VolverConErrores(nameof(Settings));

            var respuesta = await _api.PutAsync<UsuarioResponse>("Usuario/Preferencias", model, cancellationToken);

            if (respuesta.Estado == HttpStatusCode.Unauthorized)
                return SesionVencida();

            if (respuesta.Exitosa)
            {
                HttpContext.Session.SetString(ClavesSesion.Moneda, model.Moneda);
                TempData["Exito"] = "Se guardaron sus preferencias";
            }
            else
                TempData["Error"] = "No se pudieron guardar sus preferencias";

            return RedirectToAction(nameof(Settings));
        }

        #endregion

        /// <summary>
        /// El token venció o no es válido (la API respondió 401): se cierra la sesión de la WEB
        /// y se manda al Login para que vuelva a entrar.
        /// </summary>
        private IActionResult SesionVencida()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login", "Home");
        }

        /// <summary>Junta los errores de validación del formulario en TempData y vuelve a la pantalla.</summary>
        private IActionResult VolverConErrores(string accion)
        {
            TempData["Error"] = string.Join(" ", ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .Where(m => !string.IsNullOrWhiteSpace(m))
                .Distinct());
            return RedirectToAction(accion);
        }

        /// <summary>
        /// Borra el archivo de una foto de perfil. Solo acepta rutas de la carpeta de fotos y usa
        /// Path.GetFileName para que una ruta manipulada no pueda borrar archivos de otra carpeta.
        /// </summary>
        private void BorrarFoto(string? rutaWeb)
        {
            if (string.IsNullOrEmpty(rutaWeb) || !rutaWeb.StartsWith(RutaWebFotos, StringComparison.Ordinal))
                return;

            var archivo = Path.Combine(CarpetaFotos, Path.GetFileName(rutaWeb));
            try
            {
                if (System.IO.File.Exists(archivo))
                    System.IO.File.Delete(archivo);
            }
            catch (IOException ex)
            {
                // No es grave: la foto ya se cambió, solo queda un archivo viejo sin usar
                _logger.LogWarning(ex, "No se pudo borrar la foto anterior {Archivo}", archivo);
            }
        }
    }
}
