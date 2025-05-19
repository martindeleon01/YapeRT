using AntiFraud.Actions;
using AntiFraud.Services;
using Common.Data;
using Common.DTO;
using Common.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace AntiFraud.Tests
{
    public class AntiFraudActionsMock : AntiFraudActions
    {
        private readonly string _testMessage;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConfiguration _config;
        private readonly IKafkaProducer _kafkaProducer;

        public AntiFraudActionsMock(IServiceScopeFactory scopeFactory, IConfiguration config, IKafkaProducer kafkaProducer, string testMessage)
            : base(scopeFactory, config, kafkaProducer)
        {
            _testMessage = testMessage;
            _scopeFactory = scopeFactory;
            _config = config;
            _kafkaProducer = kafkaProducer;
        }

        protected override async Task ExecuteAsync(CancellationToken cancelToken)
        {
            var dto = JsonSerializer.Deserialize<Transaction>(_testMessage);
            if (dto == null) return;

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TransactionsDbContext>();

            var totalSourceAcc = await db.Transactions
                .Where(t => t.SourceAccountId == dto.SourceAccountId && t.CreatedAt.Date == dto.CreatedAt.Date && t.Status == TransactionStatus.Approved)
                .SumAsync(t => t.Value, cancellationToken: cancelToken);

            var totalTargetAcc = await db.Transactions
                .Where(t => t.TargetAccountId == dto.TargetAccountId && t.CreatedAt.Date == dto.CreatedAt.Date && t.Status == TransactionStatus.Approved)
                .SumAsync(t => t.Value, cancellationToken: cancelToken);

            var status = (dto.Value > 2000 || ((totalSourceAcc + dto.Value > 20000) || (totalTargetAcc + dto.Value > 20000))
                ? TransactionStatus.Rejected : TransactionStatus.Approved);

            var result = new TransactionResultDTO(dto.Id, DateTime.UtcNow, status);

            var sentMessage = new SentMessages
            {
                Topic = "transaction-result",
                Payload = JsonSerializer.Serialize(result)
            };

            db.SentMessages.Add(sentMessage);
            await db.SaveChangesAsync(cancelToken);
        }
    }
}
