namespace PAW_Jueves_API.Models
{
    /// <summary>Categoría de ingreso ('I') o gasto ('G') tal como la devuelve la API.</summary>
    public class CategoriaResponse
    {
        public int IdCategoria { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Tipo { get; set; } = "G";
        public string Color { get; set; } = "#6c757d";
        public decimal? PresupuestoMensual { get; set; }
        public bool Estado { get; set; }
        public int CantidadMovimientos { get; set; }
    }
}
