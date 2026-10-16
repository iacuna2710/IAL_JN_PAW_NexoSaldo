namespace PAW_Jueves_API.Models
{
    /// <summary>
    /// Totales de una quincena del mes: la 1 va del día 1 al 15 y la 2 del 16 al último día.
    /// Lo devuelve GET /api/reportes/quincenas y lo usa el "Resumen IA".
    /// </summary>
    public class QuincenaResponse
    {
        /// <summary>1 = del 1 al 15; 2 = del 16 a fin de mes.</summary>
        public int Quincena { get; set; }
        public DateTime Desde { get; set; }
        public DateTime Hasta { get; set; }
        public decimal Ingresos { get; set; }
        public decimal Gastos { get; set; }
        public decimal Balance => Ingresos - Gastos;
        public int CantidadMovimientos { get; set; }

        /// <summary>Gasto por categoría dentro de la quincena, de mayor a menor.</summary>
        public List<GastoQuincenaFila> PorCategoria { get; set; } = [];
    }
}
