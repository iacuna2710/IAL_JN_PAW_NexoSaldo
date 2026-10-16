using System.ComponentModel.DataAnnotations;

namespace PAW_Jueves_API.Models
{
    /// <summary>
    /// Datos que la WEB envía en el cuerpo JSON de POST /api/Home/Register para crear un usuario.
    /// Los cuatro campos son obligatorios: si falta alguno, la API responde 400 Bad Request
    /// automáticamente (por [ApiController]) y no se intenta insertar en la BD.
    /// Cada propiedad se envía al parámetro del mismo nombre de sp_RegistrarUsuario.
    /// </summary>
    public class RegisterRequestModel
    {
        /// <summary>Correo del usuario nuevo. No puede estar repetido en la tabla Usuario.</summary>
        [Required]
        public string CorreoElectronico { get; set; } = string.Empty;

        /// <summary>Contraseña del usuario nuevo. Se guarda tal cual en la columna Contrasenna.</summary>
        [Required]
        public string Contrasenna { get; set; } = string.Empty;

        /// <summary>Número de identificación (cédula). No puede estar repetido en la tabla Usuario.</summary>
        [Required]
        public string Identificacion { get; set; } = string.Empty;

        /// <summary>Nombre completo del usuario nuevo.</summary>
        [Required]
        public string NombreCompleto { get; set; } = string.Empty;
    }
}
