namespace PAW_Jueves_API.Models
{
    /// <summary>Un mes de GET /api/reportes/tendencia.</summary>
    public class TendenciaMesResponse
    {
        public int Anno { get; set; }
        public int Mes { get; set; }
        public decimal Ingresos { get; set; }
        public decimal Gastos { get; set; }
    }
}
