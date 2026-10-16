using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using PAW_Jueves_API.Models;

namespace PAW_Jueves_API.Services
{
    /// <summary>
    /// Consulta los reportes del mes. Lo usan ReportesController (dashboard) y AsistenteController
    /// (para darle a Gemini los mismos números que ve el usuario).
    /// </summary>
    public interface IReportesService
    {
        Task<ResumenMensualResponse> ResumenMensualAsync(int idUsuario, int anno, int mes, CancellationToken cancellationToken);

        Task<IReadOnlyList<TendenciaMesResponse>> TendenciaAsync(int idUsuario, int anno, int mes, int meses, CancellationToken cancellationToken);

        /// <summary>Totales de las dos quincenas del mes (1 al 15 y 16 a fin de mes) con su gasto por categoría.</summary>
        Task<IReadOnlyList<QuincenaResponse>> QuincenasAsync(int idUsuario, int anno, int mes, CancellationToken cancellationToken);

        Task<string> MonedaAsync(int idUsuario, CancellationToken cancellationToken);
    }
}
