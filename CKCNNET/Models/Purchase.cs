using System;

namespace CKCNNET.Models
{
    public class Purchase
    {
        public int Id { get; set; }
        public int BuyerId { get; set; }
        public User? Buyer { get; set; }
        
        public int GameAccountId { get; set; }
        public GameAccount? GameAccount { get; set; }
        
        public decimal Amount { get; set; }
        public string? Status { get; set; }
        public DateTime PurchaseDate { get; set; } = DateTime.Now;
        public DateTime? CompletedDate { get; set; }
        
        public string? AccountUsername { get; set; }
        public string? AccountPassword { get; set; }

        public string? RejectionReason { get; set; }
    }
}