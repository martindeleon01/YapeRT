using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Transactions.Application.DTOs;
using Transactions.Application.Services;
using Transactions.Controllers;
using Transactions.Domain.Entities;
using Transactions.Infrastructure.Data;

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
            //Arrange
            var dto = new TransactionDto(
                SourceAccountId: Guid.NewGuid(),
                TargetAccountId: Guid.NewGuid(),
                TransferType: 1,
                Value: 100
            );

            var serviceMock = new Mock<ITransactionService>();
            serviceMock
                .Setup(s => s.CreateTransactionAsync(dto))
                .Returns(Task.CompletedTask);

            var controller = new TransactionsController(serviceMock.Object);
            
            //Act
            var result = await controller.Create(dto);

            //Assert
            result.Should().BeOfType<AcceptedResult>();
            serviceMock.Verify(s => s.CreateTransactionAsync(dto), Times.Once);
        }

        [Fact]
        public async Task RetrieveTransaction_ShouldReturnTransaction_WhenFound()
        {
            //Arrange
            var transactionId = Guid.NewGuid();
            var createdAt = DateTime.UtcNow.Date;

            var dto = new RetrieveTransactionDTO(transactionId, createdAt);

            var expectedTransaction = new Transaction
            {
                Id = transactionId,
                SourceAccountId = Guid.NewGuid(),
                TargetAccountId = Guid.NewGuid(),
                Value = 150,
                TransferTypeId = 1,
                Status = Domain.Enums.TransactionStatus.Approved,
                CreatedAt = createdAt,
                UpdatedAt = DateTime.UtcNow
            };

            var serviceMock = new Mock<ITransactionService>();
            serviceMock
                .Setup(s => s.RetrieveTransactionAsync(transactionId, createdAt))
                .ReturnsAsync(expectedTransaction);

            var controller = new TransactionsController(serviceMock.Object);
            
            //Act
            var result = await controller.RetrieveTransaction(dto);

            //Assert
            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(expectedTransaction);
            serviceMock.Verify(s => s.RetrieveTransactionAsync(transactionId, createdAt), Times.Once);
        }

        [Fact]
        public async Task RetrieveTransaction_ShouldReturnNull_WhenTransactionNotFound()
        {
            //Arrange
            var transactionId = Guid.NewGuid();
            var createdAt = DateTime.UtcNow.Date;
            var dto = new RetrieveTransactionDTO(transactionId, createdAt);

            var serviceMock = new Mock<ITransactionService>();
            serviceMock
                .Setup(s => s.RetrieveTransactionAsync(transactionId, createdAt))
                .ReturnsAsync((Transaction?)null);

            var controller = new TransactionsController(serviceMock.Object);

            //Act
            var result = await controller.RetrieveTransaction(dto);

            //Assert
            result.Should().BeNull();
            serviceMock.Verify(s => s.RetrieveTransactionAsync(transactionId, createdAt), Times.Once);
        }
    }
}
