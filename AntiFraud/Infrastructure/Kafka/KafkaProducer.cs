using Confluent.Kafka;
using System.Text.Json;

namespace AntiFraud.Infrastructure.Kafka
{
    public class KafkaProducer : IKafkaProducer
    {
        private readonly IProducer<Null, string> _producer;

        public KafkaProducer(IConfiguration config)
        {
            var producerConfig = new ProducerConfig
            {
                BootstrapServers = config["Kafka:BootstrapServers"],
                AllowAutoCreateTopics = true
            };
            _producer = new ProducerBuilder<Null, string>(producerConfig).Build();
        }

        public Task SendAsync<T>(string topic, T message)
        {
            var json = JsonSerializer.Serialize(message);
            return _producer.ProduceAsync(topic, new Message<Null, string> { Value = json });
        }
    }
}