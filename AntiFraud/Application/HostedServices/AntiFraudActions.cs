using AntiFraud.Application.DTOs;
using AntiFraud.Domain.Entities;
using AntiFraud.Domain.Enums;
using AntiFraud.Infrastructure.Data;
using AntiFraud.Infrastructure.Kafka;
using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace AntiFraud.Application.HostedServices
{
    public class AntiFraudActions : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConfiguration _config;
        private readonly IKafkaProducer _kafka;

        public AntiFraudActions(IServiceScopeFactory scopeFactory, IConfiguration config, IKafkaProducer kafka)
        {
            _scopeFactory = scopeFactory;
            _config = config;
            _kafka = kafka;
        }

        protected override async Task ExecuteAsync(CancellationToken cancelToken)
        {
            var topic = "transaction-result";

            var consumerConfig = new ConsumerConfig
            {
                BootstrapServers = _config["Kafka:BootstrapServers"],
                GroupId = "antifraud-group",
                AutoOffsetReset = AutoOffsetReset.Earliest,
                AllowAutoCreateTopics = true
            };

            using var consumer = new ConsumerBuilder<Ignore, string>(consumerConfig).Build();
            consumer.Subscribe("transaction-validate");

            while (!cancelToken.IsCancellationRequested)
            {
                try
                {
                    var cr = consumer.Consume(TimeSpan.FromSeconds(1));
                    if (cr == null)
                    {
                        Console.WriteLine("No hay mensajes, sigo esperando...");
                        await Task.Delay(100, cancelToken); // opcional
                        continue;
                    }

                    var dto = JsonSerializer.Deserialize<Transaction>(cr.Message.Value);

                    if (dto == null)
                        continue;

                    using var scope = _scopeFactory.CreateScope();
                    Console.WriteLine("Resolviendo DB");
                    var db = scope.ServiceProvider.GetRequiredService<TransactionsDbContext>();

                    var totalSourceAcc = await db.Transactions
                        .Where(t => t.SourceAccountId == dto.SourceAccountId && t.CreatedAt.Date == dto.CreatedAt.Date && t.Status == TransactionStatus.Approved)
                        .SumAsync(t => t.Value, cancellationToken: cancelToken);

                    var totalTargetAcc = await db.Transactions
                        .Where(t => t.TargetAccountId == dto.TargetAccountId && t.CreatedAt.Date == dto.CreatedAt.Date && t.Status == TransactionStatus.Approved)
                        .SumAsync(t => t.Value,cancellationToken: cancelToken);

                    var status = dto.Value > 2000 || totalSourceAcc + dto.Value > 20000 || totalTargetAcc + dto.Value > 20000 ? TransactionStatus.Rejected : TransactionStatus.Approved;

                    var result = new TransactionResultDTO(dto.Id, DateTime.UtcNow, status);

                    var sentMessage = new SentMessages
                    {
                        Topic = topic,
                        Payload = JsonSerializer.Serialize(result)
                    };

                    db.SentMessages.Add(sentMessage);
                    await db.SaveChangesAsync(cancelToken);
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
