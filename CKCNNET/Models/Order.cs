using System;

namespace CKCNNET.Models
{
    public class Order
    {
        public int Id { get; set; }
        public int BuyerId { get; set; }
        public User Buyer { get; set; }
        public int AccountGameId { get; set; }
        public GameAccount GameAccount { get; set; }
        public DateTime OrderDate { get; set; }
        public string Status { get; set; }
    }
}
