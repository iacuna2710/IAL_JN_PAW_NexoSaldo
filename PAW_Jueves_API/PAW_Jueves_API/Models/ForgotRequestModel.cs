using System.ComponentModel.DataAnnotations;

namespace PAW_Jueves_API.Models
{
    /// <summary>
    /// Datos que la WEB envía a POST api/Home/Forgot para iniciar
    /// la recuperación de acceso.
    /// </summary>
    public class ForgotRequestModel
    {
        /// <summary>Correo del usuario que olvidó su contraseña.</summary>
        [Required]
        public string CorreoElectronico { get; set; } = string.Empty;
    }
}
