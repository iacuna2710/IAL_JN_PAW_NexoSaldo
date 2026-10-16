namespace PAW_Jueves.Models
{
    /// <summary>Todo lo que muestra el dashboard (Home/Index).</summary>
    public class DashboardViewModel
    {
        public int Anno { get; set; }
        public int Mes { get; set; }
        public string Moneda { get; set; } = "CRC";
        public ResumenMensualModel Resumen { get; set; } = new();
        public List<TendenciaMesModel> Tendencia { get; set; } = [];
        public List<ActividadModel> Actividad { get; set; } = [];
    }
}
