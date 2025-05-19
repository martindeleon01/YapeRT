using Common.Data;
using Common.DTO;
using Common.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Transactions.Controllers;
using Transactions.Services;

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
                sourceAccountId: Guid.NewGuid(),
                targetAccountId: Guid.NewGuid(),
                transferType: 1,
                value: 100
            );

            var kafkaMock = new Mock<IKafkaProducer>();

            using var context = new TransactionsDbContext(_dbContextOptions);
            var controller = new TransactionsController(context, kafkaMock.Object);

            var result = await controller.Create(dto);

            var transaction = await context.Transactions.ToListAsync();
            var message= await context.SentMessages.ToListAsync();

            transaction.Should().HaveCount(1);
            message.Should().HaveCount(1);

            var savedTransaction = transaction[0];
            savedTransaction.SourceAccountId.Should().Be(dto.sourceAccountId);
            savedTransaction.TargetAccountId.Should().Be(dto.targetAccountId);
            savedTransaction.TransferTypeId.Should().Be(dto.transferType);
            savedTransaction.Value.Should().Be(dto.value);
            savedTransaction.Status.Should().Be(TransactionStatus.Pending);

            var savedMessage = message[0];
            savedMessage.Topic.Should().Be("transaction-validate");
            savedMessage.Payload.Should().Contain(dto.sourceAccountId.ToString());

            result.Should().BeOfType<AcceptedResult>();
        }
    }
}
