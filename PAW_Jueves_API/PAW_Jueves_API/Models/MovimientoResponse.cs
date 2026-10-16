namespace PAW_Jueves_API.Models
{
    /// <summary>Un movimiento (ingreso o gasto) con el nombre y color de su categoría.</summary>
    public class MovimientoResponse
    {
        public int IdMovimiento { get; set; }
        public int IdCategoria { get; set; }
        public string Categoria { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public string Tipo { get; set; } = "G";
        public decimal Monto { get; set; }
        public DateTime Fecha { get; set; }
        public string? Descripcion { get; set; }
    }
}
