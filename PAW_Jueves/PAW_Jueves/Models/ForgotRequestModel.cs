using System.ComponentModel.DataAnnotations;

namespace PAW_Jueves.Models
{
    /// <summary>
    /// Modelo del formulario "Olvidó su contraseña" (Views/Home/Forgot.cshtml) y también los
    /// datos que HomeController.Forgot envía como JSON a POST /api/Home/Forgot.
    ///
    /// Cómo se muestra el mensaje de validación:
    ///  1. Al recibir el POST, ASP.NET Core revisa [Required]. Si el campo está vacío, guarda el
    ///     ErrorMessage en ModelState (la lista de errores de validación de la petición).
    ///  2. En la vista, &lt;span asp-validation-for="CorreoElectronico"&gt; busca en ModelState el
    ///     error de ese campo y lo escribe en rojo debajo del input.
    /// </summary>
    public class ForgotRequestModel
    {
        /// <summary>
        /// Correo del usuario que quiere recuperar el acceso. Obligatorio; si queda vacío se
        /// muestra "El correo electrónico es obligatorio".
        /// </summary>
        [Required(ErrorMessage = "El correo electrónico es obligatorio")]
        public string CorreoElectronico { get; set; } = string.Empty;
    }
}
