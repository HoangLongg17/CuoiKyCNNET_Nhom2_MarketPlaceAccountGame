using System;

namespace CKCNNET.Models
{
    public class PaymentProof
    {
        public int Id { get; set; }
        public int BuyerId { get; set; }
        public User? Buyer { get; set; }
        public int GameAccountId { get; set; }
        public GameAccount? GameAccount { get; set; }
        public string? FileName { get; set; }
        public DateTime UploadedAt { get; set; } = DateTime.Now;
    }
}