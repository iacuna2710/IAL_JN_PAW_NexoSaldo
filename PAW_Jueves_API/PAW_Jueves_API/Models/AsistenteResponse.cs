namespace PAW_Jueves_API.Models
{
    /// <summary>Respuesta del chat y del resumen.</summary>
    public class AsistenteResponse : ResponseApi
    {
        public string? Texto { get; set; }

        /// <summary>True si el resumen salió de la caché (no se llamó a Gemini).</summary>
        public bool DesdeCache { get; set; }
    }
}
