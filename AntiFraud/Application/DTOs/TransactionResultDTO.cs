using AntiFraud.Domain.Enums;

namespace AntiFraud.Application.DTOs
{
    public record TransactionResultDTO(Guid TransactionExternalId, DateTime CreatedAt, TransactionStatus? Status);
}
