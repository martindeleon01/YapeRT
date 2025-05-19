using Common.Data;
using Microsoft.EntityFrameworkCore;
using Transactions.Services;
using Common.Models;

namespace Transactions.Updates
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
                        .Take(20)
                        .ToListAsync(cancelToken);

                    foreach (var m in messages)
                    {
                        try
                        {
                            await _kafka.SendRawAsync(m.Topic, m.Payload);

                            m.Status = MessageStatus.Sent;
                            m.ProcessedAt = DateTime.UtcNow;
                            m.Error = null;
                        }
                        catch (Exception ex)
                        {
                            m.RetryCount += 1;
                            m.Status = MessageStatus.Failed;
                            m.Error = ex.Message;

                            _logger.LogWarning($"Failed to send message {m.Id}: {ex.Message}");
                        }

                        await db.SaveChangesAsync(cancelToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sending messages.");
                }

                await Task.Delay(TimeSpan.FromSeconds(5), cancelToken);
            }
        }
    }
}
