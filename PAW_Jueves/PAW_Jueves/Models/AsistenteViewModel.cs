namespace PAW_Jueves.Models
{
    /// <summary>Pantalla del asistente.</summary>
    public class AsistenteViewModel
    {
        public bool Habilitado { get; set; }
        public string Modelo { get; set; } = string.Empty;
        public List<MensajeChatItem> Mensajes { get; set; } = [];
    }
}
