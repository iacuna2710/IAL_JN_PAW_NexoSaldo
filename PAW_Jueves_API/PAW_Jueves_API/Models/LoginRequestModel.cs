using System.ComponentModel.DataAnnotations;

namespace PAW_Jueves_API.Models
{
    /// <summary>
    /// Datos que la WEB envía en el cuerpo JSON de POST /api/Home/Login.
    /// Ejemplo del JSON recibido: { "correoElectronico": "ana@correo.com", "contrasenna": "123" }
    ///
    /// [Required] marca un campo como obligatorio. Como HomeController tiene [ApiController],
    /// si alguno llega vacío o no llega, la API responde 400 Bad Request automáticamente y el
    /// método Login no se ejecuta (no se consulta la BD).
    /// </summary>
    public class LoginRequestModel
    {
        /// <summary>Correo con el que el usuario se registró. Se envía a sp_IniciarSesion.</summary>
        [Required]
        public string CorreoElectronico { get; set; } = string.Empty;

        /// <summary>Contraseña escrita por el usuario. Se envía a sp_IniciarSesion.</summary>
        [Required]
        public string Contrasenna { get; set; } = string.Empty;
    }
}
