using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Transactions.Application.DTOs;
using Transactions.Controllers;
using Transactions.Domain.Enums;
using Transactions.Infrastructure.Data;
using Transactions.Infrastructure.Kafka;

namespace Transactions.Tests
{
    public class TransactionsControllerTests
    {
        private readonly DbContextOptions<TransactionsDbContext> _dbContextOptions;

        public TransactionsControllerTests() 
        {
            _dbContextOptions = new DbContextOptionsBuilder<TransactionsDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
        }

        [Fact]
        public async Task Create_ShouldAddTransactionAndSendMessage()
        {
            var dto = new TransactionDto(
                SourceAccountId: Guid.NewGuid(),
                TargetAccountId: Guid.NewGuid(),
                TransferType: 1,
                Value: 100
            );

            var kafkaMock = new Mock<IKafkaProducer>();

            using var context = new TransactionsDbContext(_dbContextOptions);

            var controller = new TransactionsController(null);

            var result = await controller.Create(dto);

            var transaction = await context.Transactions.ToListAsync();
            var message= await context.SentMessages.ToListAsync();

            transaction.Should().HaveCount(1);
            message.Should().HaveCount(1);

            var savedTransaction = transaction[0];
            savedTransaction.SourceAccountId.Should().Be(dto.SourceAccountId);
            savedTransaction.TargetAccountId.Should().Be(dto.TargetAccountId);
            savedTransaction.TransferTypeId.Should().Be(dto.TransferType);
            savedTransaction.Value.Should().Be(dto.Value);
            savedTransaction.Status.Should().Be(TransactionStatus.Pending);

            var savedMessage = message[0];
            savedMessage.Topic.Should().Be("transaction-validate");
            savedMessage.Payload.Should().Contain(dto.SourceAccountId.ToString());

            result.Should().BeOfType<AcceptedResult>();
        }
    }
}
