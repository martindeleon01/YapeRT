namespace Transactions.Infrastructure.Kafka
{
    public interface IKafkaProducer
    {
        Task SendAsync<T>(string topic, T message);
        Task SendRawAsync(string topic, string payload);
    }
}
