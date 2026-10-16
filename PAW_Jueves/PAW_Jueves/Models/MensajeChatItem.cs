namespace PAW_Jueves.Models
{
    /// <summary>Un mensaje del historial (Rol: "user" o "model").</summary>
    public class MensajeChatItem
    {
        public string Rol { get; set; } = string.Empty;
        public string Texto { get; set; } = string.Empty;
        public DateTime Fecha { get; set; }
    }
}
