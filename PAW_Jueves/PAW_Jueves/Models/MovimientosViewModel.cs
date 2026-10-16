namespace PAW_Jueves.Models
{
    /// <summary>Pantalla de movimientos: filtros, categorías para el filtro y la página de resultados.</summary>
    public class MovimientosViewModel
    {
        public int Anno { get; set; }
        public int Mes { get; set; }
        public string? Tipo { get; set; }
        public int? IdCategoria { get; set; }
        public string Moneda { get; set; } = "CRC";
        public List<CategoriaModel> Categorias { get; set; } = [];
        public PaginaModel<MovimientoModel> Pagina { get; set; } = new();
    }
}
