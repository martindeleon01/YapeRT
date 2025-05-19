using Common.Data;
using Common.DTO;
using Common.Models;
using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Transactions.Updates
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
            var consumerConfig = new ConsumerConfig
            {
                BootstrapServers = _config["Kafka:BootstrapServers"],
                GroupId = "antifraud-group",
                AutoOffsetReset = AutoOffsetReset.Earliest,
                AllowAutoCreateTopics = true
            };

            using var consumer = new ConsumerBuilder<Ignore, string>(consumerConfig).Build();
            consumer.Subscribe("transaction-result");

            while (!cancelToken.IsCancellationRequested) 
            {
                try
                {
                    //var cr = consumer.Consume(cancelToken);

                    var cr = consumer.Consume(TimeSpan.FromSeconds(1));
                    if (cr == null)
                    {
                        Console.WriteLine("No hay mensajes, sigo esperando...");
                        await Task.Delay(100, cancelToken); // opcional
                        continue;
                    }
                    var dto = JsonSerializer.Deserialize<TransactionResultDTO>(cr.Message.Value);

                    if (dto == null)
                        continue;

                    using var scope = _scopeFactory.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<TransactionsDbContext>();

                    var transaction = await db.Transactions.Where(t => t.Id == dto.transactionExternalId).FirstOrDefaultAsync();

                    if (transaction == null)
                        continue;

                    transaction.Status = dto.status ?? transaction.Status;
                    transaction.UpdatedAt = DateTime.UtcNow;
                    await db.SaveChangesAsync(cancelToken);

                    await Task.Delay(100, cancelToken);
                }
                catch (ConsumeException ex)
                {
                    Console.WriteLine($"Kafka consume error: {ex.Message}");
                }
                catch (JsonException ex)
                {
                    Console.WriteLine($"JSON parse error: {ex.Message}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Unexpected error in TransactionsStatusUpdate: {ex.Message}");
                }
            }
        }
    }
}
