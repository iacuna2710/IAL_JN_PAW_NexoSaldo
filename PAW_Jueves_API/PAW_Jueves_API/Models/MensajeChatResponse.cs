namespace PAW_Jueves_API.Models
{
    /// <summary>Un mensaje del historial del chat.</summary>
    public class MensajeChatResponse
    {
        /// <summary>"user" o "model".</summary>
        public string Rol { get; set; } = string.Empty;
        public string Texto { get; set; } = string.Empty;
        public DateTime Fecha { get; set; }
    }
}
