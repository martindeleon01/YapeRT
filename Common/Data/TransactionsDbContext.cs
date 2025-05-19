using Common.Models;
using Microsoft.EntityFrameworkCore;

namespace Common.Data
{
    public class TransactionsDbContext : DbContext
    {
        public TransactionsDbContext(DbContextOptions<TransactionsDbContext> options) : base(options) { }
        public DbSet<Transaction> Transactions => Set<Transaction>();
        public DbSet<SentMessages> SentMessages => Set<SentMessages>();
    }
}
