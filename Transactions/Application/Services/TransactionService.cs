using Common.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Transactions.Application.DTOs;
using Transactions.Domain.Entities;
using Transactions.Domain.Enums;
using Transactions.Infrastructure.Data;
using Transactions.Infrastructure.Kafka;

namespace Transactions.Application.Services
{
    public class TransactionService : ITransactionService
    {
        private readonly TransactionsDbContext _context;
        private readonly IKafkaProducer _kafka;

        public TransactionService(TransactionsDbContext context, IKafkaProducer kafka)
        {
            _context = context;
            _kafka = kafka;
        }

        public async Task CreateTransactionAsync(TransactionDto dto)
        {
            var transaction = new Transaction
            {
                Id = Guid.NewGuid(),
                SourceAccountId = dto.SourceAccountId,
                TargetAccountId = dto.TargetAccountId,
                Value = dto.Value,
                TransferTypeId = dto.TransferType,
                Status = TransactionStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Transactions.Add(transaction);

            var sentMessage = new SentMessages
            {
                Topic = "transaction-validate",
                Payload = JsonSerializer.Serialize(transaction)
            };

            _context.SentMessages.Add(sentMessage);

            await _context.SaveChangesAsync();
        }

        public async Task<Transaction?> RetrieveTransactionAsync(Guid id, DateTime createdAt)
        {
            return await _context.Transactions
                .FirstOrDefaultAsync(t => t.Id == id && t.CreatedAt.Date == createdAt.Date);
        }
    }
}