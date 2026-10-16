using System.ComponentModel.DataAnnotations;

namespace PAW_Jueves_API.Models
{
    /// <summary>
    /// Datos que la WEB envía a POST api/Home/Register para crear un usuario.
    /// Todos los campos son obligatorios.
    /// </summary>
    public class RegisterRequestModel
    {
        /// <summary>Correo del nuevo usuario (lo usará para iniciar sesión).</summary>
        [Required]
        public string CorreoElectronico { get; set; } = string.Empty;

        /// <summary>Contraseña del nuevo usuario.</summary>
        [Required]
        public string Contrasenna { get; set; } = string.Empty;

        /// <summary>Número de identificación (cédula).</summary>
        [Required]
        public string Identificacion { get; set; } = string.Empty;

        /// <summary>Nombre completo del usuario.</summary>
        [Required]
        public string NombreCompleto { get; set; } = string.Empty;
    }
}
