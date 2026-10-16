namespace PAW_Jueves_API.Models
{
    /// <summary>
    /// Clase base (padre) de las respuestas de la API. Su única propiedad es Mensaje.
    ///
    /// Las clases que heredan de ella, como UsuarioResponse, reciben también la propiedad
    /// Mensaje sin tener que declararla otra vez. Así todas las respuestas tienen el mismo
    /// campo "mensaje" en el JSON, y la WEB siempre sabe dónde buscar el texto a mostrar.
    /// </summary>
    public class ResponseApi
    {
        /// <summary>
        /// Texto para el usuario final. En respuestas de error explica qué falló, por ejemplo
        /// "No se validó la información correctamente". Si no se asigna, queda como "" (vacío).
        /// </summary>
        public string Mensaje { get; set; } = string.Empty;
    }
}
