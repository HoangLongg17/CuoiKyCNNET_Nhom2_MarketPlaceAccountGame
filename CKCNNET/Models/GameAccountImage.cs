using System;

namespace CKCNNET.Models
{
    public class GameAccountImage
    {
        public int Id { get; set; }
        public int GameAccountId { get; set; }
        public GameAccount? GameAccount { get; set; }

        public string ImagePath { get; set; } // Đường dẫn ảnh lưu trên server
        public string FileName { get; set; } // Tên file gốc
        public DateTime UploadedAt { get; set; } = DateTime.Now;
    }
}