using Api.Application.Interfaces;
using Contracts.Dtos;
using Dapper;
using Npgsql;

namespace Api.Infrastructure.Persistence;

public class TransactionsRepository : ITransactionsRepository
{
    private readonly IConfiguration _config;
    public TransactionsRepository(IConfiguration config) { _config = config; }

    public async Task<(IEnumerable<TransactionDto> Items, int Total)> QueryTransactionsAsync(
        string customerId, DateTime? from, DateTime? to, string? category,
        int pageNumber, int pageSize, CancellationToken ct)
    {
        await using var conn = new NpgsqlConnection(_config.GetConnectionString("ReadDb"));
        await conn.OpenAsync(ct);

        var sql = @"SELECT transaction_id AS TransactionId,
                           bank AS Source,
                           posted_at_utc AS PostedAt,
                           category, amount, currency, description
                    FROM transactions
                    WHERE customer_id = @customerId
                      AND (@from IS NULL OR posted_at_utc >= @from)
                      AND (@to IS NULL OR posted_at_utc <= @to)
                      AND (@category IS NULL OR category = @category)
                    ORDER BY posted_at_utc DESC
                    OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY";

        var countSql = @"SELECT COUNT(*) FROM transactions
                         WHERE customer_id = @customerId
                           AND (@from IS NULL OR posted_at_utc >= @from)
                           AND (@to IS NULL OR posted_at_utc <= @to)
                           AND (@category IS NULL OR category = @category)";

        var items = await conn.QueryAsync<TransactionDto>(sql, new { customerId, from, to, category, offset = (pageNumber - 1) * pageSize, pageSize });
        var total = await conn.ExecuteScalarAsync<int>(countSql, new { customerId, from, to, category });
        return (items, total);
    }
}