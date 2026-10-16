using System.ComponentModel.DataAnnotations;

namespace PAW_Jueves.Models
{
    /// <summary>
    /// Modelo del formulario de registro (Views/Home/Register.cshtml) y también los datos que
    /// HomeController.Register envía como JSON a POST /api/Home/Register.
    ///
    /// Los cuatro campos son obligatorios. Cada [Required] trae su propio ErrorMessage, que es el
    /// texto exacto que aparece en rojo debajo del campo correspondiente cuando queda vacío
    /// (mediante &lt;span asp-validation-for&gt; en la vista).
    /// </summary>
    public class RegisterRequestModel
    {
        /// <summary>
        /// Correo del usuario nuevo; será su usuario para iniciar sesión. Si queda vacío se
        /// muestra "El correo electrónico es obligatorio".
        /// </summary>
        [Required(ErrorMessage = "El correo electrónico es obligatorio")]
        public string CorreoElectronico { get; set; } = string.Empty;

        /// <summary>
        /// Contraseña del usuario nuevo. Si queda vacía se muestra "La contraseña es obligatoria".
        /// </summary>
        [Required(ErrorMessage = "La contraseña es obligatoria")]
        public string Contrasenna { get; set; } = string.Empty;

        /// <summary>
        /// Número de identificación (cédula). Si queda vacío se muestra
        /// "La identificación es obligatoria".
        /// </summary>
        [Required(ErrorMessage = "La identificación es obligatoria")]
        public string Identificacion { get; set; } = string.Empty;

        /// <summary>
        /// Nombre completo del usuario nuevo. Si queda vacío se muestra
        /// "El nombre completo es obligatorio".
        /// </summary>
        [Required(ErrorMessage = "El nombre completo es obligatorio")]
        public string NombreCompleto { get; set; } = string.Empty;
    }
}
