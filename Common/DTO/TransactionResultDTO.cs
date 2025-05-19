using Common.Models;

namespace Common.DTO
{
    public record TransactionResultDTO(Guid transactionExternalId, DateTime createdAt, TransactionStatus? status);
}
