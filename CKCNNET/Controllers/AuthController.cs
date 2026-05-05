using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CKCNNET.Services;
using CKCNNET.Models;
using CKCNNET.Data;

namespace CKCNNET.Controllers
{
    public class AuthController : Controller
    {
        private readonly IAuthService _authService;
        private readonly ApplicationDbContext _context;

        public AuthController(IAuthService authService, ApplicationDbContext context)
        {
            _authService = authService;
            _context = context;
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(string username, string email, string password, string confirmPassword, string phoneNumber)
        {
            try
            {
                if (password != confirmPassword)
                {
                    ModelState.AddModelError("", "Mật khẩu không khớp!");
                    return View();
                }

                await _authService.RegisterAsync(username, email, password, phoneNumber);
                TempData["SuccessMessage"] = "Đăng ký thành công! Vui lòng đăng nhập.";
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
                return View();
            }
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string email, string password, bool rememberMe = false)
        {
            try
            {
                var user = await _authService.LoginAsync(email, password);

                HttpContext.Session.SetInt32("UserId", user.Id);
                HttpContext.Session.SetString("Username", user.Username);
                HttpContext.Session.SetString("UserRole", user.Role.Name);
                HttpContext.Session.SetString("UserEmail", user.Email);

                if (rememberMe)
                {
                    Response.Cookies.Append("UserId", user.Id.ToString(),
                        new Microsoft.AspNetCore.Http.CookieOptions { Expires = DateTime.Now.AddDays(30) });
                }

                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
                return View();
            }
        }

        [HttpGet]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }

        // Trang hồ sơ người dùng
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
                return RedirectToAction("Login");

            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == userId.Value);

            if (user == null)
                return NotFound();

            return View(user);
        }

        // Đăng ký làm Seller
        [HttpPost]
        public async Task<IActionResult> RegisterAsSeller(string reason, string bankAccount, string bankAccountHolder)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (!userId.HasValue)
                    return RedirectToAction("Login");

                var user = await _context.Users
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u => u.Id == userId.Value);

                if (user == null)
                    return NotFound();

                if (user.IsSellerApproved)
                {
                    TempData["ErrorMessage"] = "Bạn đã là Seller rồi!";
                    return RedirectToAction("Profile");
                }

                // Kiểm tra xem đã có request chưa duyệt chưa
                var existingRequest = await _context.SellerRequests
                    .FirstOrDefaultAsync(sr => sr.UserId == userId.Value && sr.Status == SellerRequest.RequestStatus.Pending);

                if (existingRequest != null)
                {
                    TempData["ErrorMessage"] = "Bạn đã gửi yêu cầu rồi. Vui lòng chờ Admin duyệt!";
                    return RedirectToAction("Profile");
                }

                if (string.IsNullOrEmpty(reason))
                {
                    TempData["ErrorMessage"] = "Vui lòng nhập lý do muốn làm Seller!";
                    return RedirectToAction("Profile");
                }

                // Tạo request mới
                var sellerRequest = new SellerRequest
                {
                    UserId = userId.Value,
                    Reason = reason,
                    BankAccount = bankAccount,
                    BankAccountHolder = bankAccountHolder,
                    Status = SellerRequest.RequestStatus.Pending,
                    CreatedAt = DateTime.Now
                };

                _context.SellerRequests.Add(sellerRequest);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Gửi yêu cầu thành công! Admin sẽ duyệt trong thời gian sớm nhất.";
                return RedirectToAction("Profile");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi: " + ex.Message;
                return RedirectToAction("Profile");
            }
        }

        // Trang chỉnh sửa hồ sơ
        [HttpGet]
        public async Task<IActionResult> EditProfile()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
                return RedirectToAction("Login");

            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == userId.Value);

            if (user == null)
                return NotFound();

            return View(user);
        }

        // Xử lý cập nhật hồ sơ
        [HttpPost]
        public async Task<IActionResult> EditProfile(string phoneNumber, string bankAccount, string bankAccountHolder)
        {
            int? userId = null;  // ← Khai báo userId bên ngoài try
            try
            {
                userId = HttpContext.Session.GetInt32("UserId");
                if (!userId.HasValue)
                    return RedirectToAction("Login");

                var user = await _context.Users
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u => u.Id == userId.Value);

                if (user == null)
                    return NotFound();

                // Kiểm tra quyền Admin hoặc Seller để cập nhật ngân hàng
                bool isAdminOrSeller = user.Role?.Name == "Admin" || user.Role?.Name == "Seller" || user.IsSellerApproved;

                // Cập nhật thông tin cá nhân (cho tất cả user)
                if (!string.IsNullOrWhiteSpace(phoneNumber))
                {
                    if (!System.Text.RegularExpressions.Regex.IsMatch(phoneNumber, @"^\d{10,11}$"))
                    {
                        TempData["ErrorMessage"] = "Số điện thoại phải có 10-11 ký tự số!";
                        return View(user);
                    }
                    user.PhoneNumber = phoneNumber;
                }

                // Cập nhật tài khoản ngân hàng (chỉ cho Admin/Seller)
                if (isAdminOrSeller)
                {
                    if (!string.IsNullOrWhiteSpace(bankAccount) || !string.IsNullOrWhiteSpace(bankAccountHolder))
                    {
                        if (string.IsNullOrWhiteSpace(bankAccount) || string.IsNullOrWhiteSpace(bankAccountHolder))
                        {
                            TempData["ErrorMessage"] = "Vui lòng điền đầy đủ thông tin tài khoản ngân hàng!";
                            return View(user);
                        }

                        // Kiểm tra định dạng số tài khoản
                        if (!System.Text.RegularExpressions.Regex.IsMatch(bankAccount, @"^\d{10,20}$"))
                        {
                            TempData["ErrorMessage"] = "Số tài khoản phải chứa 10-20 ký tự số!";
                            return View(user);
                        }

                        // Kiểm tra tên chủ tài khoản
                        if (bankAccountHolder.Length < 3 || bankAccountHolder.Length > 100)
                        {
                            TempData["ErrorMessage"] = "Tên chủ tài khoản phải từ 3-100 ký tự!";
                            return View(user);
                        }

                        user.BankAccount = bankAccount;
                        user.BankAccountHolder = bankAccountHolder;
                    }
                }

                user.UpdatedAt = DateTime.Now;
                _context.Users.Update(user);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Cập nhật hồ sơ thành công!";
                return RedirectToAction("Profile");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi: " + ex.Message;
                
                if (userId.HasValue)
                {
                    var user = await _context.Users
                        .Include(u => u.Role)
                        .FirstOrDefaultAsync(u => u.Id == userId.Value);
                    return View(user);
                }
                
                return RedirectToAction("Profile");
            }
        }

        // Trang đổi mật khẩu
        [HttpGet]
        public async Task<IActionResult> ChangePassword()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
                return RedirectToAction("Login");

            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == userId.Value);

            if (user == null)
                return NotFound();

            return View(user);
        }

        // Xử lý đổi mật khẩu
        [HttpPost]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            int? userId = null;
            try
            {
                userId = HttpContext.Session.GetInt32("UserId");
                if (!userId.HasValue)
                    return RedirectToAction("Login");

                if (string.IsNullOrWhiteSpace(currentPassword) || string.IsNullOrWhiteSpace(newPassword))
                {
                    TempData["ErrorMessage"] = "Vui lòng điền đầy đủ thông tin!";
                    var user1 = await _context.Users
                        .Include(u => u.Role)
                        .FirstOrDefaultAsync(u => u.Id == userId.Value);
                    return View(user1);
                }

                if (newPassword != confirmPassword)
                {
                    TempData["ErrorMessage"] = "Mật khẩu mới không khớp!";
                    var user1 = await _context.Users
                        .Include(u => u.Role)
                        .FirstOrDefaultAsync(u => u.Id == userId.Value);
                    return View(user1);
                }

                if (newPassword.Length < 8)
                {
                    TempData["ErrorMessage"] = "Mật khẩu phải có ít nhất 8 ký tự!";
                    var user1 = await _context.Users
                        .Include(u => u.Role)
                        .FirstOrDefaultAsync(u => u.Id == userId.Value);
                    return View(user1);
                }

                if (currentPassword == newPassword)
                {
                    TempData["ErrorMessage"] = "Mật khẩu mới phải khác mật khẩu cũ!";
                    var user1 = await _context.Users
                        .Include(u => u.Role)
                        .FirstOrDefaultAsync(u => u.Id == userId.Value);
                    return View(user1);
                }

                var user = await _context.Users.FindAsync(userId.Value);
                if (user == null)
                    return RedirectToAction("Login");

                // Kiểm tra mật khẩu hiện tại
                if (!VerifyPassword(currentPassword, user.PasswordHash))
                {
                    TempData["ErrorMessage"] = "Mật khẩu hiện tại không chính xác!";
                    var user2 = await _context.Users
                        .Include(u => u.Role)
                        .FirstOrDefaultAsync(u => u.Id == userId.Value);
                    return View(user2);
                }

                // Cập nhật mật khẩu mới
                user.PasswordHash = HashPassword(newPassword);
                user.UpdatedAt = DateTime.Now;
                _context.Users.Update(user);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Đổi mật khẩu thành công!";
                return RedirectToAction("Profile");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi: " + ex.Message;
                if (userId.HasValue)
                {
                    var user = await _context.Users
                        .Include(u => u.Role)
                        .FirstOrDefaultAsync(u => u.Id == userId.Value);
                    return View(user);
                }
                return RedirectToAction("Profile");
            }
        }

        #region === Helper Methods ===

        private string HashPassword(string password)
        {
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
                return System.Convert.ToBase64String(hashedBytes);
            }
        }

        private bool VerifyPassword(string password, string hash)
        {
            var hashOfInput = HashPassword(password);
            return hashOfInput.Equals(hash);
        }

        #endregion
    }
}