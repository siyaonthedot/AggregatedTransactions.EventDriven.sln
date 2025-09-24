namespace Contracts.Events
{
    public record TransactionCreatedV1(
        string TransactionId,
        string CustomerId,
        string Bank,
        DateTime PostedAtUtc,
        string Category,
        decimal Amount,
        string Currency,
        string Description
    );

    public static class Topics
    {
        public const string Transactions = "transactions.created.v1";
    }
}
