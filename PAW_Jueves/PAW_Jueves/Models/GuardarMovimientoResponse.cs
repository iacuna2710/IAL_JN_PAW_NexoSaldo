namespace PAW_Jueves.Models
{
    /// <summary>Respuesta de la API al guardar un movimiento (con la alerta de presupuesto, si aplica).</summary>
    public class GuardarMovimientoResponse
    {
        public int IdMovimiento { get; set; }
        public string? Alerta { get; set; }
        public string Mensaje { get; set; } = string.Empty;
    }
}
