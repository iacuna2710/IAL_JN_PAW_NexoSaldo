namespace PAW_Jueves.Models
{
    /// <summary>
    /// Representa el JSON que responde la API. HomeController lo usa SOLO cuando la API responde
    /// con error, para sacar el Mensaje y mostrarlo en la vista mediante ViewBag.Mensaje.
    ///
    /// Al convertir el JSON, cada campo se copia a la propiedad del mismo nombre sin importar
    /// mayúsculas ("mensaje" → Mensaje). Los campos del JSON que no tienen propiedad aquí
    /// (por ejemplo "idUsuario") se ignoran.
    ///
    /// Si la API responde 400 por campos vacíos, ese JSON no trae "mensaje", así que Mensaje
    /// queda vacío ("") y la vista no muestra la alerta.
    /// </summary>
    public class UsuarioResponse
    {
        /// <summary>Token JWT que entrega la API al iniciar sesión. En las demás respuestas viene vacío.</summary>
        public string Token { get; set; } = string.Empty;

        /// <summary>Id del usuario; la API lo envía al iniciar sesión. En errores vale 0.</summary>
        public int IdUsuario { get; set; }

        /// <summary>Nombre completo del usuario, si la API lo envía. Si no, queda "".</summary>
        public string NombreCompleto { get; set; } = string.Empty;

        /// <summary>
        /// Texto de error que envía la API, por ejemplo "No se validó la información correctamente".
        /// Es lo que se muestra en la alerta de la vista. Si la API no lo envía, queda "".
        /// </summary>
        public string Mensaje { get; set; } = string.Empty;
    }
}
