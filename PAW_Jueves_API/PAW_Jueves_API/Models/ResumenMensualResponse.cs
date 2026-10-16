namespace PAW_Jueves_API.Models
{
    /// <summary>Respuesta de GET /api/reportes/resumen-mensual.</summary>
    public class ResumenMensualResponse
    {
        public int Anno { get; set; }
        public int Mes { get; set; }
        public decimal Ingresos { get; set; }
        public decimal Gastos { get; set; }
        public decimal Balance => Ingresos - Gastos;
        public int CantidadMovimientos { get; set; }
        public IEnumerable<GastoCategoriaResponse> PorCategoria { get; set; } = [];
    }
}
