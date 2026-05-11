using System;

namespace CKCNNET.Models
{
    public class PurchaseReport
    {
        public int Id { get; set; }
        public int PurchaseId { get; set; }
        public Purchase? Purchase { get; set; }

        public int BuyerId { get; set; }
        public User? Buyer { get; set; }

        public string Reason { get; set; } // Lý do báo cáo
        public string Description { get; set; } // Mô tả chi tiết

        public string Status { get; set; } = "Pending"; // Pending, Under Review, Resolved, Dismissed
        public string? AdminNote { get; set; }

        public DateTime ReportedDate { get; set; } = DateTime.Now;
        public DateTime? ResolvedDate { get; set; }
    }
}