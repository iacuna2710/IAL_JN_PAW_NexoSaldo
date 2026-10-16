namespace PAW_Jueves.Models
{
    public class GastoCategoriaModel
    {
        public int IdCategoria { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public decimal? PresupuestoMensual { get; set; }
        public decimal Total { get; set; }

        /// <summary>Porcentaje usado del presupuesto (0 si no tiene).</summary>
        public decimal PorcentajeUsado => PresupuestoMensual is > 0 ? Total / PresupuestoMensual.Value * 100 : 0;
    }
}
