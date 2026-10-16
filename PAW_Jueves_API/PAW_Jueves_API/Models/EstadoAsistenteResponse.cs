namespace PAW_Jueves_API.Models
{
    /// <summary>GET /api/asistente/estado.</summary>
    public class EstadoAsistenteResponse
    {
        public bool Habilitado { get; set; }
        public string Modelo { get; set; } = string.Empty;
    }
}
