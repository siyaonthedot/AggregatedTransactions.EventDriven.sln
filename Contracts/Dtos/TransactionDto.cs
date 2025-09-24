using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.Dtos
{
    public record TransactionDto(
        string TransactionId,
        string Source,
        DateTime PostedAt,
        string Category,
        decimal Amount,
        string Currency,
        string Description
    );

    public record PagedResponse<T>(IEnumerable<T> Data, PaginationMeta Meta);
    public record PaginationMeta(int PageNumber, int PageSize, int TotalPages, long TotalCount);
}
