namespace PAW_Jueves.Models
{
    /// <summary>Movimiento tal como lo devuelve GET /api/movimientos.</summary>
    public class MovimientoModel
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
