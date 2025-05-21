using Common.Models;
using Microsoft.EntityFrameworkCore;
using Transactions.Domain.Entities;

namespace Transactions.Infrastructure.Data
{
    public class TransactionsDbContext : DbContext
    {
        public TransactionsDbContext(DbContextOptions<TransactionsDbContext> options) : base(options) { }

        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<SentMessages> SentMessages { get; set; }
    }
}
