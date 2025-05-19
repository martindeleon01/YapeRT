using System.ComponentModel.DataAnnotations.Schema;

namespace Common.Models
{
    [Table("Transactions")]
    public class Transaction
    {
        public Guid Id { get; set; }
        public Guid SourceAccountId { get; set; }
        public Guid TargetAccountId { get; set; }
        public int Value { get; set; }
        public int TransferTypeId { get; set; }
        public TransactionStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public enum TransactionStatus
    {
        Pending = 1,
        Approved = 2,
        Rejected = 3
    }
}
