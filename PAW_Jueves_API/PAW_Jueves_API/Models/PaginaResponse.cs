namespace PAW_Jueves_API.Models
{
    /// <summary>Respuesta de GET /api/movimientos: una página y los datos para paginar.</summary>
    public class PaginaResponse<T>
    {
        public IEnumerable<T> Datos { get; set; } = [];
        public int Total { get; set; }
        public int Pagina { get; set; }
        public int TamannoPagina { get; set; }
    }
}
