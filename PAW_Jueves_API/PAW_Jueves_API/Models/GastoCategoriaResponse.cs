namespace PAW_Jueves_API.Models
{
    /// <summary>Gasto de una categoría en el mes, con su presupuesto (si tiene).</summary>
    public class GastoCategoriaResponse
    {
        public int IdCategoria { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public decimal? PresupuestoMensual { get; set; }
        public decimal Total { get; set; }
    }
}
