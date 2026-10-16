namespace PAW_Jueves_API.Models
{
    /// <summary>Fila del segundo resultado de sp_ResumenQuincenal: lo gastado en una categoría en una quincena.</summary>
    public class GastoQuincenaFila
    {
        public int Quincena { get; set; }
        public int IdCategoria { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public decimal Total { get; set; }
    }
}
