using AntiFraud.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AntiFraud.Infrastructure.Data
{
    public class TransactionsDbContext : DbContext
    {
        public TransactionsDbContext(DbContextOptions<TransactionsDbContext> options) : base(options) { }

        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<SentMessages> SentMessages { get; set; }
    }
}
