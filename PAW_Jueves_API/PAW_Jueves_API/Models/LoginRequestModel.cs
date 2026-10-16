using System.ComponentModel.DataAnnotations;

namespace PAW_Jueves_API.Models
{
    /// <summary>
    /// Datos que la WEB envía a POST api/Home/Login.
    /// Con [ApiController], si falta un campo [Required] la API responde 400
    /// automáticamente, sin entrar al método.
    /// </summary>
    public class LoginRequestModel
    {
        /// <summary>Correo con el que el usuario inicia sesión.</summary>
        [Required]
        public string CorreoElectronico { get; set; } = string.Empty;

        /// <summary>Contraseña escrita por el usuario.</summary>
        [Required]
        public string Contrasenna { get; set; } = string.Empty;
    }
}
