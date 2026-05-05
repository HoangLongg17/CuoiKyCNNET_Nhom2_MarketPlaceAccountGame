using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CKCNNET.Models
{
    public class SellerRequest
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }
        public User? User { get; set; }

        [Required]
        [StringLength(500)]
        public string? Reason { get; set; } // Lý do muốn làm seller

        [StringLength(500)]
        public string? BankAccount { get; set; } // Tài khoản ngân hàng

        [StringLength(500)]
        public string? BankAccountHolder { get; set; } // Chủ tài khoản

        public enum RequestStatus
        {
            Pending = 0,    // Chờ duyệt
            Approved = 1,   // Duyệt
            Rejected = 2    // Từ chối
        }

        public RequestStatus Status { get; set; } = RequestStatus.Pending;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? ReviewedAt { get; set; }

        public int? ReviewedByAdminId { get; set; } // Admin duyệt
        public User? ReviewedByAdmin { get; set; }

        [StringLength(500)]
        public string? AdminNotes { get; set; } // Ghi chú từ Admin
    }
}