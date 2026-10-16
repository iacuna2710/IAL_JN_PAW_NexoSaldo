using System.ComponentModel.DataAnnotations;

namespace PAW_Jueves.Models
{
    /// <summary>
    /// Modelo del formulario de registro (Views/Home/Register.cshtml).
    /// Cada [Required] trae su propio mensaje, que aparece debajo del campo
    /// correspondiente si el usuario lo deja vacío.
    /// </summary>
    public class RegisterRequestModel
    {
        /// <summary>Correo del nuevo usuario.</summary>
        [Required(ErrorMessage = "El correo electrónico es obligatorio")]
        public string CorreoElectronico { get; set; } = string.Empty;

        /// <summary>Contraseña del nuevo usuario.</summary>
        [Required(ErrorMessage = "La contraseña es obligatoria")]
        public string Contrasenna { get; set; } = string.Empty;

        /// <summary>Número de identificación (cédula).</summary>
        [Required(ErrorMessage = "La identificación es obligatoria")]
        public string Identificacion { get; set; } = string.Empty;

        /// <summary>Nombre completo del usuario.</summary>
        [Required(ErrorMessage = "El nombre completo es obligatorio")]
        public string NombreCompleto { get; set; } = string.Empty;
    }
}
