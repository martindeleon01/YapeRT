using AntiFraud.Domain.Enums;

namespace AntiFraud.Domain.Entities
{
    public class SentMessages
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Topic { get; set; } = string.Empty;
        public string Payload { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ProcessedAt { get; set; }
        public string? Error { get; set; }
        public int RetryCount { get; set; } = 0;
        public MessageStatus Status { get; set; } = MessageStatus.Pending;
    }
}
