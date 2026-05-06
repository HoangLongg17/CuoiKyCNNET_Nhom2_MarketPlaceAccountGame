using System;
using System.Collections.Generic;

namespace CKCNNET.Models
{
    public class GameAccount
    {
        public int Id { get; set; }
        public int GameId { get; set; }
        public Game? Game { get; set; }
        
        public int SellerId { get; set; }
        public User? Seller { get; set; }
        
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public string? Username { get; set; }
        public string? Password { get; set; }
        public string? ContactInfo { get; set; }
        
        public bool IsApproved { get; set; }
        public bool IsSold { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        public ICollection<Purchase> Purchases { get; set; } = new List<Purchase>();
        public ICollection<Cart> CartItems { get; set; } = new List<Cart>();
        public ICollection<PaymentProof> PaymentProofs { get; set; } = new List<PaymentProof>();
    }
}