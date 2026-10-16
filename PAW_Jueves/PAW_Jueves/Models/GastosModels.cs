using System.ComponentModel.DataAnnotations;

namespace PAW_Jueves.Models
{
    /// <summary>Categoría tal como la devuelve GET /api/categorias.</summary>
    public class CategoriaModel
    {
        public int IdCategoria { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Tipo { get; set; } = "G";
        public string Color { get; set; } = "#6c757d";
        public decimal? PresupuestoMensual { get; set; }
        public bool Estado { get; set; } = true;
        public int CantidadMovimientos { get; set; }
    }

    /// <summary>Formulario de crear o editar una categoría.</summary>
    public class CategoriaFormModel
    {
        public int IdCategoria { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 50 caracteres")]
        public string Nombre { get; set; } = string.Empty;

        [Required, RegularExpression("^[IG]$", ErrorMessage = "Tipo no válido")]
        public string Tipo { get; set; } = "G";

        [Required, RegularExpression("^#[0-9a-fA-F]{6}$", ErrorMessage = "Color no válido")]
        public string Color { get; set; } = "#4dabf7";

        [Range(0.01, 999_999_999_999, ErrorMessage = "El presupuesto debe ser mayor que 0")]
        public decimal? PresupuestoMensual { get; set; }

        public bool Estado { get; set; } = true;
    }

    /// <summary>Movimiento tal como lo devuelve GET /api/movimientos.</summary>
    public class MovimientoModel
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

    /// <summary>Formulario de crear o editar un movimiento.</summary>
    public class MovimientoFormModel
    {
        public int IdMovimiento { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Elija una categoría")]
        public int IdCategoria { get; set; }

        [Required, RegularExpression("^[IG]$", ErrorMessage = "Tipo no válido")]
        public string Tipo { get; set; } = "G";

        [Range(0.01, 999_999_999, ErrorMessage = "El monto debe ser mayor que 0")]
        public decimal Monto { get; set; }

        [Required(ErrorMessage = "La fecha es obligatoria")]
        public DateOnly Fecha { get; set; }

        [StringLength(250, ErrorMessage = "La descripción no puede tener más de 250 caracteres")]
        public string? Descripcion { get; set; }
    }

    /// <summary>Respuesta de la API al guardar un movimiento (con la alerta de presupuesto, si aplica).</summary>
    public class GuardarMovimientoResponse
    {
        public int IdMovimiento { get; set; }
        public string? Alerta { get; set; }
        public string Mensaje { get; set; } = string.Empty;
    }

    /// <summary>Una página de resultados de la API.</summary>
    public class PaginaModel<T>
    {
        public List<T> Datos { get; set; } = [];
        public int Total { get; set; }
        public int Pagina { get; set; } = 1;
        public int TamannoPagina { get; set; } = 20;
        public int TotalPaginas => Math.Max(1, (int)Math.Ceiling(Total / (double)TamannoPagina));
    }

    public class GastoCategoriaModel
    {
        public int IdCategoria { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public decimal? PresupuestoMensual { get; set; }
        public decimal Total { get; set; }

        /// <summary>Porcentaje usado del presupuesto (0 si no tiene).</summary>
        public decimal PorcentajeUsado => PresupuestoMensual is > 0 ? Total / PresupuestoMensual.Value * 100 : 0;
    }

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

    public class TendenciaMesModel
    {
        public int Anno { get; set; }
        public int Mes { get; set; }
        public decimal Ingresos { get; set; }
        public decimal Gastos { get; set; }
    }

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

    /// <summary>Pantalla de crear o editar un movimiento.</summary>
    public class MovimientoFormularioViewModel
    {
        public MovimientoFormModel Formulario { get; set; } = new();
        public List<CategoriaModel> Categorias { get; set; } = [];
        public bool EsEdicion => Formulario.IdMovimiento > 0;
        public string Moneda { get; set; } = "CRC";
    }
}
