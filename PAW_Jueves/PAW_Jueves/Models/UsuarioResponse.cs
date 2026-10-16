namespace PAW_Jueves.Models
{
    /// <summary>
    /// Respuesta que la WEB recibe de la API (se deserializa desde el JSON).
    /// Se usa sobre todo para leer el Mensaje cuando la API responde con error
    /// y mostrarlo en la vista con ViewBag.Mensaje.
    /// </summary>
    public class UsuarioResponse
    {
        /// <summary>Nombre completo del usuario devuelto por la API.</summary>
        public string NombreCompleto { get; set; } = string.Empty;

        /// <summary>Mensaje informativo o de error devuelto por la API.</summary>
        public string Mensaje { get; set; } = string.Empty;
    }
}
