using System.Text.Json;
using Confluent.Kafka;

namespace Transactions.Infrastructure.Kafka
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

        public async Task SendAsync<T>(string topic, T message)
        {
            var json = JsonSerializer.Serialize(message);
            Console.WriteLine($"Sending to Kafka: topic={topic}, message={json}");

            try
            {
                var deliveryResult = await _producer.ProduceAsync(topic, new Message<Null, string> { Value = json });
                Console.WriteLine($"Message delivered to {deliveryResult.TopicPartitionOffset}");
            }
            catch (ProduceException<Null, string> ex)
            {
                Console.WriteLine($"Kafka delivery failed: {ex.Error.Reason}");
            }
        }

        public async Task SendRawAsync(string topic, string payload)
        {
            var message = new Message<Null, string> { Value = payload };
            await _producer.ProduceAsync(topic, message);
        }
    }
}
