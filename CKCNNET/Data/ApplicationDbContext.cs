using Microsoft.EntityFrameworkCore;
using CKCNNET.Models;
using System.Security.Cryptography;
using System.Text;

namespace CKCNNET.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Role> Roles { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Game> Games { get; set; }
        public DbSet<GameAccount> GameAccounts { get; set; }
        public DbSet<Cart> Carts { get; set; }
        public DbSet<Purchase> Purchases { get; set; }
        public DbSet<SellerRequest> SellerRequests { get; set; }

        public DbSet<PaymentProof> PaymentProofs { get; set; }
        public DbSet<GameAccountImage> GameAccountImages { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<GameAccount>()
                .Property(ga => ga.Price)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Purchase>()
                .Property(p => p.Amount)
                .HasColumnType("decimal(18,2)");

            // Seed Roles
            modelBuilder.Entity<Role>().HasData(
                new Role { Id = 1, Name = "User" },
                new Role { Id = 2, Name = "Seller" },
                new Role { Id = 3, Name = "Admin" }
            );

            // Hash password cho Admin
            string adminPassword = HashPassword("Admin@123");

            // Seed Admin
            modelBuilder.Entity<User>().HasData(
                new User
                {
                    Id = 1,
                    Username = "Admin",
                    Email = "admin@gmail.com",
                    PasswordHash = adminPassword,
                    PhoneNumber = "0912345678",
                    BankAccount = "1234567890",
                    BankAccountHolder = "Nguyen Van Admin",
                    RoleId = 3,
                    CreatedAt = DateTime.Now
                }
            );

            // Seed Games
            modelBuilder.Entity<Game>().HasData(
                new Game { Id = 1, Name = "League of Legends", Description = "MOBA game - Đấu Trường Chân Lý", ImageUrl = "/images/lol.jpg" },
                new Game { Id = 2, Name = "Dota 2", Description = "MOBA game - Chiến Trường Quân Sư", ImageUrl = "/images/dota2.png" },
                new Game { Id = 3, Name = "Counter-Strike 2", Description = "FPS game - Đấu súng chiến thuật", ImageUrl = "/images/cs2.png" },
                new Game { Id = 4, Name = "Valorant", Description = "Tactical shooter - Bắn súng chiến thuật", ImageUrl = "/images/valorant.jpg" },
                new Game { Id = 5, Name = "FGO (Fate Grand Order)", Description = "Gacha RPG - Trò chơi nhập vai quay gacha", ImageUrl = "/images/fgo.jpg" },
                new Game { Id = 6, Name = "Reverse: 1999", Description = "Gacha RPG - Nhập vai quay gacha chiến thuật", ImageUrl = "/images/reverse1999.jpg" },
                new Game { Id = 7, Name = "Ninja School", Description = "MMORPG - Trường Học Ninja", ImageUrl = "/images/ninja_school.jpg" },
                new Game { Id = 8, Name = "Hiệp Sĩ Online", Description = "MMORPG - Game nhập vai trực tuyến", ImageUrl = "/images/hiep_si_online.jpg" }
            );

            modelBuilder.Entity<User>()
                .HasOne(u => u.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(u => u.RoleId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<GameAccount>()
                .HasOne(ga => ga.Game)
                .WithMany(g => g.GameAccounts)
                .HasForeignKey(ga => ga.GameId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<GameAccount>()
                .HasOne(ga => ga.Seller)
                .WithMany(u => u.GameAccounts)
                .HasForeignKey(ga => ga.SellerId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Cart>()
                .HasOne(c => c.User)
                .WithMany(u => u.CartItems)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Cart>()
                .HasOne(c => c.GameAccount)
                .WithMany(ga => ga.CartItems)
                .HasForeignKey(c => c.GameAccountId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Purchase>()
                .HasOne(p => p.Buyer)
                .WithMany(u => u.Purchases)
                .HasForeignKey(p => p.BuyerId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Purchase>()
                .HasOne(p => p.GameAccount)
                .WithMany(ga => ga.Purchases)
                .HasForeignKey(p => p.GameAccountId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<PaymentProof>()
                .HasOne(pp => pp.Buyer)
                .WithMany(u => u.PaymentProofs)
                .HasForeignKey(pp => pp.BuyerId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<PaymentProof>()
                .HasOne(pp => pp.GameAccount)
                .WithMany(ga => ga.PaymentProofs)
                .HasForeignKey(pp => pp.GameAccountId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<GameAccountImage>()
                .HasOne(gai => gai.GameAccount)
                .WithMany(ga => ga.Images)
                .HasForeignKey(gai => gai.GameAccountId)
                .OnDelete(DeleteBehavior.Cascade);
        }

        //method để hash password
        private static string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
        }
    }
}