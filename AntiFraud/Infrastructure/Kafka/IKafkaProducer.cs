namespace AntiFraud.Infrastructure.Kafka
{
    public interface IKafkaProducer
    {
        Task SendAsync<T>(string topic, T message);
    }
}