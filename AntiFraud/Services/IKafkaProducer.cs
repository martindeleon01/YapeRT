namespace AntiFraud.Services
{
    public interface IKafkaProducer
    {
        Task SendAsync<T>(string topic, T message);
    }
}
