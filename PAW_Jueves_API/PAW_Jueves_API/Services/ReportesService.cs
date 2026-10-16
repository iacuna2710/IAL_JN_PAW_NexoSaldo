using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using PAW_Jueves_API.Models;

namespace PAW_Jueves_API.Services
{
    public sealed class ReportesService(IConfiguration _configuration) : IReportesService
    {
        private SqlConnection Conexion() => new(_configuration.GetValue<string>("Variables:ConnectionString"));

        public async Task<ResumenMensualResponse> ResumenMensualAsync(int idUsuario, int anno, int mes, CancellationToken cancellationToken)
        {
            await using var context = Conexion();
            using var resultados = await context.QueryMultipleAsync(new CommandDefinition(
                "sp_ResumenMensual",
                new { IdUsuario = idUsuario, Anno = anno, Mes = mes },
                commandType: CommandType.StoredProcedure, cancellationToken: cancellationToken));

            var totales = await resultados.ReadSingleAsync<TotalesMes>();
            var porCategoria = (await resultados.ReadAsync<GastoCategoriaResponse>()).ToList();

            return new ResumenMensualResponse
            {
                Anno = anno,
                Mes = mes,
                Ingresos = totales.Ingresos,
                Gastos = totales.Gastos,
                CantidadMovimientos = totales.CantidadMovimientos,
                PorCategoria = porCategoria
            };
        }

        public async Task<IReadOnlyList<TendenciaMesResponse>> TendenciaAsync(int idUsuario, int anno, int mes, int meses, CancellationToken cancellationToken)
        {
            await using var context = Conexion();
            var datos = await context.QueryAsync<TendenciaMesResponse>(new CommandDefinition(
                "sp_TendenciaMensual",
                new { IdUsuario = idUsuario, Anno = anno, Mes = mes, Meses = Math.Clamp(meses, 1, 24) },
                commandType: CommandType.StoredProcedure, cancellationToken: cancellationToken));

            return datos.ToList();
        }

        public async Task<IReadOnlyList<QuincenaResponse>> QuincenasAsync(int idUsuario, int anno, int mes, CancellationToken cancellationToken)
        {
            await using var context = Conexion();
            using var resultados = await context.QueryMultipleAsync(new CommandDefinition(
                "sp_ResumenQuincenal",
                new { IdUsuario = idUsuario, Anno = anno, Mes = mes },
                commandType: CommandType.StoredProcedure, cancellationToken: cancellationToken));

            var quincenas = (await resultados.ReadAsync<QuincenaResponse>()).ToList();
            var gastos = (await resultados.ReadAsync<GastoQuincenaFila>()).ToList();

            foreach (var quincena in quincenas)
                quincena.PorCategoria = gastos.Where(g => g.Quincena == quincena.Quincena).ToList();

            return quincenas;
        }

        public async Task<string> MonedaAsync(int idUsuario, CancellationToken cancellationToken)
        {
            await using var context = Conexion();
            var perfil = await context.QueryFirstOrDefaultAsync<PerfilResponse>(new CommandDefinition(
                "sp_ConsultarUsuario",
                new { IdUsuario = idUsuario },
                commandType: CommandType.StoredProcedure, cancellationToken: cancellationToken));

            return perfil?.Moneda ?? "CRC";
        }
    }
}
