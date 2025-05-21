namespace Transactions.Application.DTOs
{
    public record RetrieveTransactionDTO(Guid TransactionExternalId, DateTime CreatedAt);
}
