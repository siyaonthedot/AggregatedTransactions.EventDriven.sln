using Contracts.Dtos;

namespace Api.Application.Interfaces;

public interface ITransactionsRepository
{
    Task<(IEnumerable<TransactionDto> Items, int Total)> QueryTransactionsAsync(
        string customerId, DateTime? from, DateTime? to, string? category,
        int pageNumber, int pageSize, CancellationToken ct);
}