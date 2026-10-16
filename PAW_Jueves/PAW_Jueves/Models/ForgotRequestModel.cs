using System.ComponentModel.DataAnnotations;

namespace PAW_Jueves.Models
{
    /// <summary>
    /// Modelo del formulario "Olvidó su contraseña" (Views/Home/Forgot.cshtml).
    /// El ErrorMessage de [Required] es el texto que muestra el
    /// &lt;span asp-validation-for&gt; de la vista cuando el campo queda vacío.
    /// </summary>
    public class ForgotRequestModel
    {
        /// <summary>Correo del usuario que quiere recuperar el acceso.</summary>
        [Required(ErrorMessage = "El correo electrónico es obligatorio")]
        public string CorreoElectronico { get; set; } = string.Empty;
    }
}
