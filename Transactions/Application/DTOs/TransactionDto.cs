namespace Transactions.Application.DTOs
{
    public record TransactionDto(Guid SourceAccountId, Guid TargetAccountId, int TransferType, int Value);
}
