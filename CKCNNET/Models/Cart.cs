using System;

namespace CKCNNET.Models
{
    public class Cart
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public User? User { get; set; }
        
        public int GameAccountId { get; set; }
        public GameAccount? GameAccount { get; set; }
        
        public DateTime AddedAt { get; set; } = DateTime.Now;
    }
}