namespace PAW_Jueves.Models
{
    /// <summary>Una fila de la bitácora que devuelve GET /api/Usuario/Actividad.</summary>
    public class ActividadModel
    {
        public string Accion { get; set; } = string.Empty;
        public string? Detalle { get; set; }
        public string? Ip { get; set; }

        /// <summary>Fecha y hora en UTC (la vista la convierte a la hora local con FormatoFecha).</summary>
        public DateTime Fecha { get; set; }
    }
}
