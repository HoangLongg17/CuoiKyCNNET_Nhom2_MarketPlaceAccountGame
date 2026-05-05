using System;
using System.Collections.Generic;

namespace CKCNNET.Models
{
    public class User
    {
        public int Id { get; set; }
        public string? Username { get; set; }
        public string? Email { get; set; }
        public string? PasswordHash { get; set; }
        public string? PhoneNumber { get; set; }
        public string? BankAccount { get; set; }
        public string? BankAccountHolder { get; set; }
        
        public int RoleId { get; set; }
        public Role? Role { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }
        
        public ICollection<GameAccount> GameAccounts { get; set; } = new List<GameAccount>();
        public ICollection<Purchase> Purchases { get; set; } = new List<Purchase>();
        public ICollection<Cart> CartItems { get; set; } = new List<Cart>();
    }
}
