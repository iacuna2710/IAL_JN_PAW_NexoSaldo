namespace PAW_Jueves_API.Models
{
    /// <summary>
    /// Respuesta al crear o editar un movimiento. Si es un gasto en una categoría con presupuesto,
    /// trae la alerta: "advertencia" (80 % o más) o "excedido" (100 % o más).
    /// </summary>
    public class GuardarMovimientoResponse : ResponseApi
    {
        public int IdMovimiento { get; set; }
        public string? Alerta { get; set; }
    }
}
