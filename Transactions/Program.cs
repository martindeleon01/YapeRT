using Microsoft.EntityFrameworkCore;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Transactions.Infrastructure.Kafka;
using Transactions.Infrastructure.HostedServices;
using Transactions.Application.Services;
using Transactions.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<TransactionsDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<ITransactionService, TransactionService>();
builder.Services.AddSingleton<IKafkaProducer, KafkaProducer>();
builder.Services.AddHostedService<TransactionsStatusUpdate>();
builder.Services.AddHostedService<SendMessageService>();

await CrearKafkaTopicsAsync(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

static async Task CrearKafkaTopicsAsync(IConfiguration config)
{
    var kafkaConfig = new AdminClientConfig
    {
        BootstrapServers = config["Kafka:BootstrapServers"]
    };

    using var adminClient = new AdminClientBuilder(kafkaConfig).Build();
    var topics = new List<TopicSpecification>
    {
        new TopicSpecification {Name = "transaction-result", NumPartitions = 1, ReplicationFactor = 1},
        new TopicSpecification {Name = "transaction-validate", NumPartitions = 1, ReplicationFactor = 1}
    };

    try
    {
        await adminClient.CreateTopicsAsync(topics);
    }
    catch (CreateTopicsException e)
    {
        foreach (var result in e.Results)
        {
            if (result.Error.Code == ErrorCode.TopicAlreadyExists)
                Console.WriteLine($"El topic ya existe: {result.Topic}");
            else
                Console.WriteLine($"Error creando el topic: {result.Topic}: { result.Error.Reason}");
        }
    }
}
