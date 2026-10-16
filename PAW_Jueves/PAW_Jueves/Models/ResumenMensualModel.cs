namespace PAW_Jueves.Models
{
    public class ResumenMensualModel
    {
        public int Anno { get; set; }
        public int Mes { get; set; }
        public decimal Ingresos { get; set; }
        public decimal Gastos { get; set; }
        public decimal Balance { get; set; }
        public int CantidadMovimientos { get; set; }
        public List<GastoCategoriaModel> PorCategoria { get; set; } = [];
    }
}
