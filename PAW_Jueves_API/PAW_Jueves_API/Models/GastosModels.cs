using System.ComponentModel.DataAnnotations;

namespace PAW_Jueves_API.Models
{
    /// <summary>Categoría de ingreso ('I') o gasto ('G') tal como la devuelve la API.</summary>
    public class CategoriaResponse
    {
        public int IdCategoria { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Tipo { get; set; } = "G";
        public string Color { get; set; } = "#6c757d";
        public decimal? PresupuestoMensual { get; set; }
        public bool Estado { get; set; }
        public int CantidadMovimientos { get; set; }
    }

    /// <summary>Cuerpo de POST /api/categorias.</summary>
    public class CrearCategoriaRequest
    {
        [Required, StringLength(50, MinimumLength = 2)]
        public string Nombre { get; set; } = string.Empty;

        [Required, RegularExpression("^[IG]$")]
        public string Tipo { get; set; } = "G";

        [Required, RegularExpression("^#[0-9a-fA-F]{6}$")]
        public string Color { get; set; } = "#6c757d";

        [Range(0.01, 999_999_999_999)]
        public decimal? PresupuestoMensual { get; set; }
    }

    /// <summary>Cuerpo de PUT /api/categorias/{id}. El tipo no se puede cambiar.</summary>
    public class ActualizarCategoriaRequest
    {
        [Required, StringLength(50, MinimumLength = 2)]
        public string Nombre { get; set; } = string.Empty;

        [Required, RegularExpression("^#[0-9a-fA-F]{6}$")]
        public string Color { get; set; } = "#6c757d";

        [Range(0.01, 999_999_999_999)]
        public decimal? PresupuestoMensual { get; set; }

        public bool Estado { get; set; } = true;
    }

    /// <summary>Un movimiento (ingreso o gasto) con el nombre y color de su categoría.</summary>
    public class MovimientoResponse
    {
        public int IdMovimiento { get; set; }
        public int IdCategoria { get; set; }
        public string Categoria { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public string Tipo { get; set; } = "G";
        public decimal Monto { get; set; }
        public DateTime Fecha { get; set; }
        public string? Descripcion { get; set; }
    }

    /// <summary>Fila de sp_ConsultarMovimientos: el movimiento más el total de filas del filtro.</summary>
    public class MovimientoPaginadoFila : MovimientoResponse
    {
        public int TotalFilas { get; set; }
    }

    /// <summary>Respuesta de GET /api/movimientos: una página y los datos para paginar.</summary>
    public class PaginaResponse<T>
    {
        public IEnumerable<T> Datos { get; set; } = [];
        public int Total { get; set; }
        public int Pagina { get; set; }
        public int TamannoPagina { get; set; }
    }

    /// <summary>Cuerpo de POST /api/movimientos y PUT /api/movimientos/{id}.</summary>
    public class MovimientoRequest
    {
        [Range(1, int.MaxValue)]
        public int IdCategoria { get; set; }

        [Required, RegularExpression("^[IG]$")]
        public string Tipo { get; set; } = "G";

        public decimal Monto { get; set; }

        public DateOnly Fecha { get; set; }

        [StringLength(250)]
        public string? Descripcion { get; set; }
    }

    /// <summary>
    /// Respuesta al crear o editar un movimiento. Si es un gasto en una categoría con presupuesto,
    /// trae la alerta: "advertencia" (80 % o más) o "excedido" (100 % o más).
    /// </summary>
    public class GuardarMovimientoResponse : ResponseApi
    {
        public int IdMovimiento { get; set; }
        public string? Alerta { get; set; }
    }

    /// <summary>Gasto de una categoría en el mes, con su presupuesto (si tiene).</summary>
    public class GastoCategoriaResponse
    {
        public int IdCategoria { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public decimal? PresupuestoMensual { get; set; }
        public decimal Total { get; set; }
    }

    /// <summary>Totales del mes (primer resultado de sp_ResumenMensual).</summary>
    public class TotalesMes
    {
        public decimal Ingresos { get; set; }
        public decimal Gastos { get; set; }
        public int CantidadMovimientos { get; set; }
    }

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

    /// <summary>Un mes de GET /api/reportes/tendencia.</summary>
    public class TendenciaMesResponse
    {
        public int Anno { get; set; }
        public int Mes { get; set; }
        public decimal Ingresos { get; set; }
        public decimal Gastos { get; set; }
    }
}
