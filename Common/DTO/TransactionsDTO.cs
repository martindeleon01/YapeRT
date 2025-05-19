namespace Common.DTO
{
    public record TransactionDto(Guid sourceAccountId, Guid targetAccountId, int transferType, int value);
}
