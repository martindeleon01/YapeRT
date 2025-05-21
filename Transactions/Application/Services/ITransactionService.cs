using Transactions.Application.DTOs;
using Transactions.Domain.Entities;

namespace Transactions.Application.Services
{
    public interface ITransactionService
    {
        Task CreateTransactionAsync(TransactionDto dto);
        Task<Transaction?> RetrieveTransactionAsync(Guid id, DateTime createdAt);
    }
}
