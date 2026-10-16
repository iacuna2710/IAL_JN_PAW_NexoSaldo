namespace PAW_Jueves.Models
{
    /// <summary>Categoría tal como la devuelve GET /api/categorias.</summary>
    public class CategoriaModel
    {
        public int IdCategoria { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Tipo { get; set; } = "G";
        public string Color { get; set; } = "#6c757d";
        public decimal? PresupuestoMensual { get; set; }
        public bool Estado { get; set; } = true;
        public int CantidadMovimientos { get; set; }
    }
}
