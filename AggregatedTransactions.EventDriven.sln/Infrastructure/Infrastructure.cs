using System.Diagnostics.Contracts;
using Contracts.Dtos;
using Dapper;
using Microsoft.Data.SqlClient;
using StackExchange.Redis;
using System.Text.Json;

namespace Api.Infrastructure
{


    public record ApiKeyOptions(string HeaderName, string ApiKey);

    public interface ITransactionsRepository
    {
        Task<(IEnumerable<TransactionDto> Items, int Total)> QueryTransactionsAsync(
            string customerId, DateTime? from, DateTime? to, string? category, int pageNumber, int pageSize, CancellationToken ct);
    }

    public class TransactionsRepository(SqlConnection writeConn, SqlConnection readConn) : ITransactionsRepository
    {
        public async Task<(IEnumerable<TransactionDto> Items, int Total)> QueryTransactionsAsync(
            string customerId, DateTime? from, DateTime? to, string? category, int pageNumber, int pageSize, CancellationToken ct)
        {
            // Read from READ connection to scale reads
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("SELECT t.TransactionId, t.Bank AS [Source], t.PostedAtUtc AS PostedAt, t.Category, t.Amount, t.Currency, t.Description");
            sb.AppendLine("FROM dbo.Transactions t WITH (NOLOCK)");
            sb.AppendLine("WHERE t.CustomerId = @customerId");
            if (from is not null) sb.AppendLine("AND t.PostedAtUtc >= @from");
            if (to is not null) sb.AppendLine("AND t.PostedAtUtc <= @to");
            if (!string.IsNullOrWhiteSpace(category)) sb.AppendLine("AND t.Category = @category");
            sb.AppendLine("ORDER BY t.PostedAtUtc DESC");
            sb.AppendLine("OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY;");
            var sql = sb.ToString();

            var countSql = @"SELECT COUNT(*) FROM dbo.Transactions t WITH (NOLOCK)
                         WHERE t.CustomerId=@customerId
                         AND (@from IS NULL OR t.PostedAtUtc >= @from)
                         AND (@to IS NULL OR t.PostedAtUtc <= @to)
                         AND (@category IS NULL OR t.Category = @category)";

            using var conn = readConn;
            if (conn.State != System.Data.ConnectionState.Open) await conn.OpenAsync(ct);

            var items = await conn.QueryAsync<TransactionDto>(new CommandDefinition(sql, new
            {
                customerId,
                from,
                to,
                category,
                offset = (pageNumber - 1) * pageSize,
                pageSize
            }, cancellationToken: ct));

            var total = await conn.ExecuteScalarAsync<int>(new CommandDefinition(countSql, new { customerId, from, to, category }, cancellationToken: ct));
            return (items, total);
        }
    }

}
