using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Transactions.Application.DTOs;
using Transactions.Infrastructure.Data;

namespace Transactions.Infrastructure.HostedServices
{
    public class TransactionsStatusUpdate : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConfiguration _config;

        public TransactionsStatusUpdate(IServiceScopeFactory scopeFactory, IConfiguration config)
        {
            _scopeFactory = scopeFactory;
            _config = config;
        }

        protected override async Task ExecuteAsync(CancellationToken cancelToken)
        {
            var config = new ConsumerConfig
            {
                BootstrapServers = _config["Kafka:BootstrapServers"],
                GroupId = "transactions-group",
                AutoOffsetReset = AutoOffsetReset.Earliest,
                AllowAutoCreateTopics = true
            };

            using var consumer = new ConsumerBuilder<Ignore, string>(config).Build();
            consumer.Subscribe("transaction-result");

            while (!cancelToken.IsCancellationRequested)
            {
                try
                {
                    var cr = consumer.Consume(TimeSpan.FromSeconds(1));
                    if (cr == null)
                    {
                        await Task.Delay(100, cancelToken);
                        continue;
                    }

                    var dto = JsonSerializer.Deserialize<TransactionResultDTO>(cr.Message.Value);
                    if (dto == null)
                        continue;

                    using var scope = _scopeFactory.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<TransactionsDbContext>();

                    var transaction = await db.Transactions.FirstOrDefaultAsync(t => t.Id == dto.TransactionExternalId, cancelToken);
                    if (transaction == null)
                        continue;

                    transaction.Status = dto.Status ?? transaction.Status;
                    transaction.UpdatedAt = DateTime.UtcNow;
                    await db.SaveChangesAsync(cancelToken);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"TransactionStatusUpdate error: {ex.Message}");
                }
            }
        }
    }
}
