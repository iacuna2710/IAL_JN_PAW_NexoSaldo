namespace PAW_Jueves_API.Models
{
    /// <summary>Totales del mes (primer resultado de sp_ResumenMensual).</summary>
    public class TotalesMes
    {
        public decimal Ingresos { get; set; }
        public decimal Gastos { get; set; }
        public int CantidadMovimientos { get; set; }
    }
}
