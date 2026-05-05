using System.Collections.Generic;

namespace CKCNNET.Models
{
    public class Game
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        
        public ICollection<GameAccount> GameAccounts { get; set; } = new List<GameAccount>();
    }
}