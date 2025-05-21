using Microsoft.AspNetCore.Mvc;
using Transactions.Application.DTOs;
using Transactions.Application.Services;
using Transactions.Domain.Entities;

namespace Transactions.Controllers
{
    [ApiController]
    [Route("api/transactions")]
    public class TransactionsController : ControllerBase
    {
        private readonly ITransactionService _transactionService;

        public TransactionsController(ITransactionService transactionService)
        {
            _transactionService = transactionService;
        }

        [HttpPost]
        public async Task<IActionResult> Create(TransactionDto dto)
        {
            await _transactionService.CreateTransactionAsync(dto);
            return Accepted();
        }

        [HttpPost("retrieveTransaction")]
        public async Task<Transaction?> RetrieveTransaction(RetrieveTransactionDTO dto)
        {
            return await _transactionService.RetrieveTransactionAsync(dto.TransactionExternalId, dto.CreatedAt);
        }
    }
}