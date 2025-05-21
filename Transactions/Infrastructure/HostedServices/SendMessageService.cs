using Common.Models;
using Microsoft.EntityFrameworkCore;
using Transactions.Infrastructure.Data;
using Transactions.Infrastructure.Kafka;

namespace Transactions.Infrastructure.HostedServices
{
    public class SendMessageService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IKafkaProducer _kafka;
        private readonly ILogger<SendMessageService> _logger;

        public SendMessageService(IServiceScopeFactory scopeFactory, IKafkaProducer kafka, ILogger<SendMessageService> logger)
        {
            _scopeFactory = scopeFactory;
            _kafka = kafka;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken cancelToken)
        {
            while (!cancelToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<TransactionsDbContext>();

                    var messages = await db.SentMessages
                        .Where(m => m.Status == MessageStatus.Pending || (m.Status == MessageStatus.Failed && m.RetryCount < 5))
                        .OrderBy(m => m.CreatedAt)
                        .Take(10)
                        .ToListAsync(cancelToken);

                    foreach (var message in messages)
                    {
                        try
                        {
                            await _kafka.SendRawAsync(message.Topic, message.Payload);
                            message.Status = MessageStatus.Sent;
                            message.ProcessedAt = DateTime.UtcNow;
                            _logger.LogInformation($"Sent message {message.Id} to topic {message.Topic}");
                        }
                        catch (Exception ex)
                        {
                            message.Status = MessageStatus.Failed;
                            message.Error = ex.Message;
                            message.RetryCount++;
                            _logger.LogWarning($"Failed to send message {message.Id}: {ex.Message}");
                        }
                    }

                    await db.SaveChangesAsync(cancelToken);
                    await Task.Delay(1000, cancelToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"SendMessageService error: {ex.Message}");
                    await Task.Delay(3000, cancelToken);
                }
            }
        }
    }
}
