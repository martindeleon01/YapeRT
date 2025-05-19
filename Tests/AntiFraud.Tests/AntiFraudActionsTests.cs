using AntiFraud.Services;
using Common.Data;
using Common.DTO;
using Common.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace AntiFraud.Tests
{
    public class AntiFraudActionsTests
    {
        private readonly DbContextOptions<TransactionsDbContext> _dbContextOptions;

        public AntiFraudActionsTests() 
        {
            _dbContextOptions = new DbContextOptionsBuilder<TransactionsDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
        }

        [Fact]
        public async Task ExecuteAsync_ProcessesKafkaMessageAndSaveResults()
        {
            var transaction = new Transaction
            { 
                Id = Guid.NewGuid(),
                SourceAccountId = Guid.NewGuid(),
                TargetAccountId = Guid.NewGuid(),
                Status = TransactionStatus.Pending,
                Value = 1000,
                TransferTypeId = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var transactionJson = JsonSerializer.Serialize(transaction);

            using var context = new TransactionsDbContext(_dbContextOptions);
            context.Transactions.Add(new Transaction 
            {
                Id = Guid.NewGuid(), 
                SourceAccountId = Guid.NewGuid(), 
                TargetAccountId = Guid.NewGuid(), 
                Status = TransactionStatus.Pending,
                Value = 2500,
                TransferTypeId = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            context.SaveChanges();

            var scopeMock = new Mock<IServiceScope>();
            scopeMock.Setup(s => s.ServiceProvider.GetService(typeof(TransactionsDbContext))).Returns(context);

            var scopeFactoryMock = new Mock<IServiceScopeFactory>();
            scopeFactoryMock.Setup(x => x.CreateScope()).Returns(scopeMock.Object);

            var configMock = new Mock<IConfiguration>();
            configMock.Setup(c => c["Kafka:BootstrapServers"]).Returns("dummy:9092");

            var kafkaProducerMock = new Mock<IKafkaProducer>();

            var backgroundService = new AntiFraudActionsMock(
                scopeFactoryMock.Object,
                configMock.Object,
                kafkaProducerMock.Object,
                transactionJson
            );

            var cancellationTokenSource = new CancellationTokenSource();

            // Act
            await backgroundService.StartAsync(cancellationTokenSource.Token);

            // Assert
            var result = context.SentMessages.FirstOrDefault();
            Assert.NotNull(result);
            Assert.Contains("transaction-result", result.Topic);
        }

        [Fact]
        public async Task ExecuteAsync_RejectsTransaction_WhenDailyTotalExceeds20000()
        {
            // Arrange
            var today = DateTime.UtcNow.Date;
            var sourceId = Guid.NewGuid();

            var dbContext = GetInMemoryDbContext();
            dbContext.Transactions.Add(new Transaction
            {
                Id = Guid.NewGuid(),
                SourceAccountId = sourceId,
                TargetAccountId = Guid.NewGuid(),
                Value = 19500,
                CreatedAt = today,
                Status = TransactionStatus.Approved
            });
            dbContext.SaveChanges();

            var transaction = new Transaction
            {
                Id = Guid.NewGuid(),
                SourceAccountId = sourceId,
                TargetAccountId = Guid.NewGuid(),
                Value = 1000,
                CreatedAt = today,
                Status = TransactionStatus.Pending
            };

            var backgroundService = GetTestableService(dbContext, transaction);

            // Act
            await backgroundService.StartAsync(CancellationToken.None);

            // Assert
            var sentMessage = dbContext.SentMessages.FirstOrDefault();
            Assert.NotNull(sentMessage);
            var result = JsonSerializer.Deserialize<TransactionResultDTO>(sentMessage.Payload);
            Assert.Equal(TransactionStatus.Rejected, result.status);
        }

        [Fact]
        public async Task ExecuteAsync_RejectsTransaction_WhenValueExceedsLimit()
        {
            // Arrange
            var transaction = new Transaction
            {
                Id = Guid.NewGuid(),
                SourceAccountId = Guid.NewGuid(),
                TargetAccountId = Guid.NewGuid(),
                Value = 3000,
                CreatedAt = DateTime.UtcNow,
                Status = TransactionStatus.Pending
            };

            var dbContext = GetInMemoryDbContext();
            var backgroundService = GetTestableService(dbContext, transaction);

            // Act
            await backgroundService.StartAsync(CancellationToken.None);

            // Assert
            var sentMessage = dbContext.SentMessages.FirstOrDefault();
            Assert.NotNull(sentMessage);
            var result = JsonSerializer.Deserialize<TransactionResultDTO>(sentMessage.Payload);
            Assert.Equal(TransactionStatus.Rejected, result.status);
        }

        private TransactionsDbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<TransactionsDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new TransactionsDbContext(options);
        }

        private IServiceScopeFactory GetScopeFactoryWithDbContext(TransactionsDbContext dbContext)
        {
            var scopeMock = new Mock<IServiceScope>();
            scopeMock.Setup(s => s.ServiceProvider.GetService(typeof(TransactionsDbContext)))
                     .Returns(dbContext);

            var scopeFactoryMock = new Mock<IServiceScopeFactory>();
            scopeFactoryMock.Setup(f => f.CreateScope())
                            .Returns(scopeMock.Object);

            return scopeFactoryMock.Object;
        }

        private AntiFraudActionsMock GetTestableService(TransactionsDbContext dbContext, Transaction transaction)
        {
            var json = JsonSerializer.Serialize(transaction);
            var scopeFactory = GetScopeFactoryWithDbContext(dbContext);
            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string> { ["Kafka:BootstrapServers"] = "localhost" }).Build();
            var kafkaMock = new Mock<IKafkaProducer>();

            return new AntiFraudActionsMock(scopeFactory, config, kafkaMock.Object, json);
        }
    }
}
