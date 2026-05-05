using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using CKCNNET.Data;
using CKCNNET.Models;
using Microsoft.EntityFrameworkCore;

namespace CKCNNET.Services
{
    public interface IAuthService
    {
        Task<User> RegisterAsync(string username, string email, string password, string phoneNumber);
        Task<User> LoginAsync(string email, string password);
        string HashPassword(string password);
        bool VerifyPassword(string password, string hash);
    }

    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;

        public AuthService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<User> RegisterAsync(string username, string email, string password, string phoneNumber)
        {
            // Kiểm tra email đã tồn tại
            if (await _context.Users.AnyAsync(u => u.Email == email))
                throw new Exception("Email đã được đăng ký!");

            var user = new User
            {
                Username = username,
                Email = email,
                PasswordHash = HashPassword(password),
                PhoneNumber = phoneNumber,
                RoleId = 1, // User role
                IsSellerApproved = false,
                CreatedAt = DateTime.Now
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return user;
        }

        public async Task<User> LoginAsync(string email, string password)
        {
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email == email);

            if (user == null || !VerifyPassword(password, user.PasswordHash))
                throw new Exception("Email hoặc mật khẩu không chính xác!");

            return user;
        }

        public string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
        }

        public bool VerifyPassword(string password, string hash)
        {
            var hashOfInput = HashPassword(password);
            return hashOfInput.Equals(hash);
        }
    }
}