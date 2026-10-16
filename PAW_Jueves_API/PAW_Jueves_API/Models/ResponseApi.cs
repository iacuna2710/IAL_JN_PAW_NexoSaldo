namespace PAW_Jueves_API.Models
{
    /// <summary>
    /// Clase base de las respuestas de la API. Toda respuesta que herede de ella
    /// incluye un Mensaje, que la WEB muestra al usuario (por ejemplo, el motivo de un error).
    /// </summary>
    public class ResponseApi
    {
        /// <summary>Mensaje informativo o de error para mostrar en pantalla.</summary>
        public string Mensaje { get; set; } = string.Empty;
    }
}
