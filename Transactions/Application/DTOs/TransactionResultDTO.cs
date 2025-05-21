using Transactions.Domain.Enums;

namespace Transactions.Application.DTOs
{
    public record TransactionResultDTO(Guid TransactionExternalId, DateTime CreatedAt, TransactionStatus? Status);
}
