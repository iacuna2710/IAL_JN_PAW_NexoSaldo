using System.ComponentModel.DataAnnotations;

namespace PAW_Jueves.Models
{
    /// <summary>
    /// Modelo del formulario de inicio de sesión (Views/Home/Login.cshtml) y también los datos
    /// que HomeController.Login envía como JSON a POST /api/Home/Login.
    ///
    /// Los nombres de las propiedades deben coincidir con los asp-for de la vista
    /// (asp-for="CorreoElectronico") para que ASP.NET Core copie lo escrito en cada input a la
    /// propiedad correcta, y con los del LoginRequestModel de la API para que el JSON se entienda.
    ///
    /// Cada [Required] trae un ErrorMessage: es el texto exacto que aparece en rojo debajo del
    /// campo (mediante &lt;span asp-validation-for&gt;) cuando el usuario lo deja vacío.
    /// </summary>
    public class LoginRequestModel
    {
        /// <summary>
        /// Correo con el que el usuario se registró. Obligatorio; si queda vacío se muestra
        /// "El correo electrónico es obligatorio".
        /// </summary>
        [Required(ErrorMessage = "El correo electrónico es obligatorio")]
        public string CorreoElectronico { get; set; } = string.Empty;

        /// <summary>
        /// Contraseña del usuario. Obligatoria; si queda vacía se muestra
        /// "La contraseña es obligatoria".
        /// </summary>
        [Required(ErrorMessage = "La contraseña es obligatoria")]
        public string Contrasenna { get; set; } = string.Empty;
    }
}
