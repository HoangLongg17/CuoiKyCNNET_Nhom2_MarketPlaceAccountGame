using System;

namespace CKCNNET.Models
{
    public class Order
    {
        public int Id { get; set; }

        // Người mua
        public int BuyerId { get; set; }
        public User Buyer { get; set; }

        // Acc được mua
        public int AccountGameId { get; set; }
        public GameAccount GameAccount { get; set; }

        public DateTime OrderDate { get; set; }

        // Trạng thái: Pending, Paid, Cancelled
        public string Status { get; set; }
    }
}
