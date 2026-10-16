using System.ComponentModel.DataAnnotations;

namespace PAW_Jueves_API.Models
{
    /// <summary>
    /// Datos que la WEB envía en el cuerpo JSON de POST /api/Home/Forgot.
    /// Ejemplo del JSON recibido: { "correoElectronico": "ana@correo.com" }
    /// Si el correo llega vacío, la API responde 400 Bad Request automáticamente
    /// (por [ApiController]) y no se consulta la BD.
    /// </summary>
    public class ForgotRequestModel
    {
        /// <summary>
        /// Correo del usuario que olvidó su contraseña. Se envía a sp_ConsultarCorreo para
        /// buscar al usuario.
        /// </summary>
        [Required]
        public string CorreoElectronico { get; set; } = string.Empty;
    }
}
