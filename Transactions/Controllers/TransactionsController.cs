using Common.Data;
using Common.DTO;
using Common.Models;
using Confluent.Kafka;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Transactions.Services;

namespace Transactions.Controllers
{
    [ApiController]
    [Route("api/transactions")]
    public class TransactionsController : ControllerBase
    {
        private readonly TransactionsDbContext _context;
        private readonly IKafkaProducer _kafka;

        public TransactionsController(TransactionsDbContext context, IKafkaProducer kafka)
        {
            _context = context;
            _kafka = kafka;
        }

        [HttpPost]
        public async Task<IActionResult> Create(TransactionDto dto)
        {
            var topic = "transaction-validate";

            var transaction = new Transaction
            {
                Id = Guid.NewGuid(),
                SourceAccountId = dto.sourceAccountId,
                TargetAccountId = dto.targetAccountId,
                Value = dto.value,
                TransferTypeId = 1,
                Status = TransactionStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Transactions.Add(transaction);

            var sentMessage = new SentMessages
            {
                Topic = topic,
                Payload = JsonSerializer.Serialize(transaction)
            };

            _context.SentMessages.Add(sentMessage);

            await _context.SaveChangesAsync();

            return Accepted();
        }

        [HttpPost("retrieveTransaction")]
        public async Task<Transaction> RetrieveTransaction(RetrieveTransactionDTO result)
        {
            var transaction = await _context.Transactions.Where(t => t.Id == result.transactionExternalId && t.CreatedAt.Date == result.createdAt.Date).FirstOrDefaultAsync();
            return transaction ?? new Transaction { };
        }

    }
}
