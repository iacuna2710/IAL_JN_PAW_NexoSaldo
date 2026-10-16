namespace PAW_Jueves.Models
{
    /// <summary>Una página de resultados de la API.</summary>
    public class PaginaModel<T>
    {
        public List<T> Datos { get; set; } = [];
        public int Total { get; set; }
        public int Pagina { get; set; } = 1;
        public int TamannoPagina { get; set; } = 20;
        public int TotalPaginas => Math.Max(1, (int)Math.Ceiling(Total / (double)TamannoPagina));
    }
}
