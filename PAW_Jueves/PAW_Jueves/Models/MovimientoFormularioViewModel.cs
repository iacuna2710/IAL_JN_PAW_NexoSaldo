namespace PAW_Jueves.Models
{
    /// <summary>Pantalla de crear o editar un movimiento.</summary>
    public class MovimientoFormularioViewModel
    {
        public MovimientoFormModel Formulario { get; set; } = new();
        public List<CategoriaModel> Categorias { get; set; } = [];
        public bool EsEdicion => Formulario.IdMovimiento > 0;
        public string Moneda { get; set; } = "CRC";
    }
}
