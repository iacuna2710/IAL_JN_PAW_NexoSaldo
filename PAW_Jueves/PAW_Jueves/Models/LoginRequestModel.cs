using System.ComponentModel.DataAnnotations;

namespace PAW_Jueves.Models
{
    /// <summary>
    /// Modelo del formulario de inicio de sesión (Views/Home/Login.cshtml).
    /// Las validaciones se declaran aquí con Data Annotations y la vista las muestra
    /// con &lt;span asp-validation-for="..."&gt; debajo de cada campo.
    /// </summary>
    public class LoginRequestModel
    {
        /// <summary>Correo con el que el usuario inicia sesión.</summary>
        [Required(ErrorMessage = "El correo electrónico es obligatorio")]
        public string CorreoElectronico { get; set; } = string.Empty;

        /// <summary>Contraseña del usuario.</summary>
        [Required(ErrorMessage = "La contraseña es obligatoria")]
        public string Contrasenna { get; set; } = string.Empty;
    }
}
