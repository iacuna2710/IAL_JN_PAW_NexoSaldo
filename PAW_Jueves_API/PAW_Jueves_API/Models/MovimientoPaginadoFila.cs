namespace PAW_Jueves_API.Models
{
    /// <summary>Fila de sp_ConsultarMovimientos: el movimiento más el total de filas del filtro.</summary>
    public class MovimientoPaginadoFila : MovimientoResponse
    {
        public int TotalFilas { get; set; }
    }
}
