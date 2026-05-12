using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CKCNNET.Data;
using CKCNNET.Models;
using CKCNNET.Authorization;
using CKCNNET.Services;
using System.Text.RegularExpressions;

namespace CKCNNET.Controllers
{
    [RoleAuthorization("Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuthService _authService;

        public AdminController(ApplicationDbContext context, IAuthService authService)
        {
            _context = context;
            _authService = authService;
        }

        // Dashboard Admin
        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            var pendingSellerRequests = await _context.SellerRequests
                .Where(sr => sr.Status == SellerRequest.RequestStatus.Pending)
                .CountAsync();

            var pendingGameAccounts = await _context.GameAccounts
                .Where(ga => !ga.IsApproved && !ga.IsSold)
                .CountAsync();

            var totalSellers = await _context.Users
                .Where(u => u.RoleId == 2)
                .CountAsync();

            var totalUsers = await _context.Users.CountAsync();

            // Lấy số báo cáo chưa xử lý
            var pendingReports = await _context.PurchaseReports
                .CountAsync(r => r.Status == "Pending");
            
            ViewBag.PendingSellerRequests = pendingSellerRequests;
            ViewBag.PendingGameAccounts = pendingGameAccounts;
            ViewBag.TotalSellers = totalSellers;
            ViewBag.TotalUsers = totalUsers;
            ViewBag.PendingReportsCount = pendingReports;

            return View();
        }

        #region === Admin Management ===

        // Danh sách các Admin
        [HttpGet]
        public async Task<IActionResult> AdminManagement()
        {
            var admins = await _context.Users
                .Include(u => u.Role)
                .Where(u => u.Role.Name == "Admin")
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();

            return View(admins);
        }

        // Trang thêm Admin mới
        [HttpGet]
        public IActionResult AddAdmin()
        {
            return View();
        }

        // Xử lý thêm Admin mới
        [HttpPost]
        public async Task<IActionResult> AddAdmin(string username, string email, string password, 
            string confirmPassword, string phoneNumber)
        {
            try
            {
                // Validate dữ liệu
                if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(email) || 
                    string.IsNullOrWhiteSpace(password))
                {
                    TempData["ErrorMessage"] = "Vui lòng điền đầy đủ thông tin!";
                    return View();
                }

                if (password.Length < 8)
                {
                    TempData["ErrorMessage"] = "Mật khẩu phải có ít nhất 8 ký tự!";
                    return View();
                }

                if (password != confirmPassword)
                {
                    TempData["ErrorMessage"] = "Mật khẩu không khớp!";
                    return View();
                }

                if (username.Length < 3)
                {
                    TempData["ErrorMessage"] = "Username phải có ít nhất 3 ký tự!";
                    return View();
                }

                // Validate email format
                if (!Regex.IsMatch(email, @"^[^\s@]+@[^\s@]+\.[^\s@]+$"))
                {
                    TempData["ErrorMessage"] = "Email không hợp lệ!";
                    return View();
                }

                if (!string.IsNullOrWhiteSpace(phoneNumber) && 
                    !Regex.IsMatch(phoneNumber, @"^[0-9]{10,11}$"))
                {
                    TempData["ErrorMessage"] = "Số điện thoại phải chứa 10-11 chữ số!";
                    return View();
                }

                if (await _context.Users.AnyAsync(u => u.Email == email))
                {
                    TempData["ErrorMessage"] = "Email này đã được đăng ký!";
                    return View();
                }

                if (await _context.Users.AnyAsync(u => u.Username == username))
                {
                    TempData["ErrorMessage"] = "Username này đã tồn tại!";
                    return View();
                }

                if (!string.IsNullOrWhiteSpace(phoneNumber) && 
                    await _context.Users.AnyAsync(u => u.PhoneNumber == phoneNumber))
                {
                    TempData["ErrorMessage"] = "Số điện thoại này đã được đăng ký!";
                    return View();
                }

                // Tạo admin mới
                var user = new User
                {
                    Username = username,
                    Email = email,
                    PasswordHash = HashPassword(password),
                    PhoneNumber = phoneNumber,
                    RoleId = 2, // Admin role
                    CreatedAt = DateTime.Now
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Thêm Admin thành công!";
                return RedirectToAction("AdminManagement");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi: " + ex.Message;
                return View();
            }
        }

        // Xóa Admin
        [HttpPost]
        public async Task<IActionResult> DeleteAdmin(int id)
        {
            try
            {
                var currentUserId = HttpContext.Session.GetInt32("UserId");
                
                // Không cho phép xóa chính mình
                if (id == currentUserId)
                {
                    TempData["ErrorMessage"] = "Không thể xóa tài khoản Admin của chính mình!";
                    return RedirectToAction("AdminManagement");
                }

                var admin = await _context.Users.FindAsync(id);
                if (admin == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy Admin!";
                    return RedirectToAction("AdminManagement");
                }

                var role = await _context.Roles.FindAsync(admin.RoleId);
                if (role?.Name != "Admin")
                {
                    TempData["ErrorMessage"] = "User này không phải Admin!";
                    return RedirectToAction("AdminManagement");
                }

                _context.Users.Remove(admin);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Đã xóa Admin '{admin.Username}' thành công!";
                return RedirectToAction("AdminManagement");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi: " + ex.Message;
                return RedirectToAction("AdminManagement");
            }
        }

        #endregion

        #region === Change Password ===

        // Trang đổi mật khẩu
        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View();
        }

        // Xử lý đổi mật khẩu
        [HttpPost]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (!userId.HasValue)
                    return RedirectToAction("Login", "Auth");

                if (string.IsNullOrWhiteSpace(currentPassword) || string.IsNullOrWhiteSpace(newPassword))
                {
                    TempData["ErrorMessage"] = "Vui lòng điền đầy đủ thông tin!";
                    return View();
                }

                if (newPassword != confirmPassword)
                {
                    TempData["ErrorMessage"] = "Mật khẩu mới không khớp!";
                    return View();
                }

                if (newPassword.Length < 8)
                {
                    TempData["ErrorMessage"] = "Mật khẩu phải có ít nhất 8 ký tự!";
                    return View();
                }

                if (currentPassword == newPassword)
                {
                    TempData["ErrorMessage"] = "Mật khẩu mới phải khác mật khẩu cũ!";
                    return View();
                }

                var user = await _context.Users.FindAsync(userId.Value);
                if (user == null)
                {
                    return RedirectToAction("Login", "Auth");
                }

                // Kiểm tra mật khẩu hiện tại
                if (!VerifyPassword(currentPassword, user.PasswordHash))
                {
                    TempData["ErrorMessage"] = "Mật khẩu hiện tại không chính xác!";
                    return View();
                }

                // Cập nhật mật khẩu mới
                user.PasswordHash = HashPassword(newPassword);
                user.UpdatedAt = DateTime.Now;
                _context.Users.Update(user);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Đổi mật khẩu thành công!";
                return RedirectToAction("Dashboard");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi: " + ex.Message;
                return View();
            }
        }

        #endregion

        #region === Seller Requests ===

        // Danh sách các yêu cầu đăng ký Seller
        [HttpGet]
        public async Task<IActionResult> SellerRequests(string status = "pending")
        {
            IQueryable<SellerRequest> query = _context.SellerRequests
                .Include(sr => sr.User)
                .Include(sr => sr.ReviewedByAdmin)
                .OrderByDescending(sr => sr.CreatedAt);

            if (status == "pending")
                query = query.Where(sr => sr.Status == SellerRequest.RequestStatus.Pending);
            else if (status == "approved")
                query = query.Where(sr => sr.Status == SellerRequest.RequestStatus.Approved);
            else if (status == "rejected")
                query = query.Where(sr => sr.Status == SellerRequest.RequestStatus.Rejected);

            var requests = await query.ToListAsync();
            ViewBag.CurrentStatus = status;
            return View(requests);
        }

        // Chi tiết yêu cầu Seller
        [HttpGet]
        public async Task<IActionResult> SellerRequestDetail(int id)
        {
            var request = await _context.SellerRequests
                .Include(sr => sr.User)
                .Include(sr => sr.ReviewedByAdmin)
                .FirstOrDefaultAsync(sr => sr.Id == id);

            if (request == null)
                return NotFound();

            return View(request);
        }

        // Duyệt yêu cầu Seller - Đổi Role từ User → Seller
        [HttpPost]
        public async Task<IActionResult> ApprveSellerRequest(int id, string adminNotes = "")
        {
            try
            {
                var adminId = HttpContext.Session.GetInt32("UserId");
                if (!adminId.HasValue)
                    return RedirectToAction("Login", "Auth");

                var request = await _context.SellerRequests.FindAsync(id);
                if (request == null)
                    return NotFound();

                // Cập nhật request
                request.Status = SellerRequest.RequestStatus.Approved;
                request.ReviewedAt = DateTime.Now;
                request.ReviewedByAdminId = adminId.Value;
                request.AdminNotes = adminNotes;

                // Cập nhật user - Đổi Role thành Seller
                var user = await _context.Users
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u => u.Id == request.UserId);
                
                if (user != null)
                {
                    var sellerRole = await _context.Roles
                        .FirstOrDefaultAsync(r => r.Name == "Seller");
                    
                    if (sellerRole != null)
                    {
                        user.RoleId = sellerRole.Id;
                        user.BankAccount = request.BankAccount;
                        user.BankAccountHolder = request.BankAccountHolder;
                        user.UpdatedAt = DateTime.Now;
                        _context.Users.Update(user);
                    }
                }

                _context.SellerRequests.Update(request);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Đã duyệt yêu cầu thành công!";
                return RedirectToAction("SellerRequests");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi: " + ex.Message;
                return RedirectToAction("SellerRequestDetail", new { id });
            }
        }

        // Từ chối yêu cầu Seller
        [HttpPost]
        public async Task<IActionResult> RejectSellerRequest(int id, string adminNotes = "")
        {
            try
            {
                var adminId = HttpContext.Session.GetInt32("UserId");
                if (!adminId.HasValue)
                    return RedirectToAction("Login", "Auth");

                var request = await _context.SellerRequests.FindAsync(id);
                if (request == null)
                    return NotFound();

                request.Status = SellerRequest.RequestStatus.Rejected;
                request.ReviewedAt = DateTime.Now;
                request.ReviewedByAdminId = adminId.Value;
                request.AdminNotes = adminNotes;

                _context.SellerRequests.Update(request);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Đã từ chối yêu cầu!";
                return RedirectToAction("SellerRequests");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi: " + ex.Message;
                return RedirectToAction("SellerRequestDetail", new { id });
            }
        }

        #endregion

        #region === Game Account Approval ===

        // Danh sách account chờ duyệt
        [HttpGet]
        public async Task<IActionResult> PendingGameAccounts()
        {
            var accounts = await _context.GameAccounts
                .Include(ga => ga.Game)
                .Include(ga => ga.Seller)
                .Where(ga => !ga.IsApproved && !ga.IsSold)
                .OrderByDescending(ga => ga.CreatedAt)
                .ToListAsync();

            return View(accounts);
        }

        // Chi tiết account chờ duyệt
        [HttpGet]
        public async Task<IActionResult> PendingGameAccountDetail(int id)
        {
            var account = await _context.GameAccounts
                .Include(ga => ga.Game)
                .Include(ga => ga.Seller)
                .FirstOrDefaultAsync(ga => ga.Id == id);

            if (account == null)
                return NotFound();

            return View(account);
        }

        // Duyệt account
        [HttpPost]
        public async Task<IActionResult> ApproveGameAccount(int id)
        {
            try
            {
                var account = await _context.GameAccounts.FindAsync(id);
                if (account == null)
                    return NotFound();

                account.IsApproved = true;
                _context.GameAccounts.Update(account);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Đã duyệt account thành công!";
                return RedirectToAction("PendingGameAccounts");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi: " + ex.Message;
                return RedirectToAction("PendingGameAccountDetail", new { id });
            }
        }

        // Từ chối account
        [HttpPost]
        public async Task<IActionResult> RejectGameAccount(int id)
        {
            try
            {
                var account = await _context.GameAccounts.FindAsync(id);
                if (account == null)
                    return NotFound();

                _context.GameAccounts.Remove(account);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Đã từ chối account!";
                return RedirectToAction("PendingGameAccounts");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi: " + ex.Message;
                return RedirectToAction("PendingGameAccountDetail", new { id });
            }
        }

        #endregion

        #region === Seller Management ===

        // Danh sách các Seller
        [HttpGet]
        public async Task<IActionResult> SellerManagement()
        {
            var sellers = await _context.Users
                .Where(u => u.RoleId == 2)
                .Include(u => u.GameAccounts)
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();

            return View(sellers);
        }

        // Chi tiết Seller
        [HttpGet]
        public async Task<IActionResult> SellerDetail(int id)
        {
            var seller = await _context.Users
                .Include(u => u.Role)
                .Include(u => u.GameAccounts)
                .ThenInclude(ga => ga.Game)
                .FirstOrDefaultAsync(u => u.Id == id && u.RoleId == 2);

            if (seller == null)
                return NotFound();

            return View(seller);
        }

        // Phế chức Seller về User
        [HttpPost]
        public async Task<IActionResult> RevokeSeller(int id)
        {
            try
            {
                var adminId = HttpContext.Session.GetInt32("UserId");
                
                // Không cho phép phế chức chính mình
                if (id == adminId)
                {
                    TempData["ErrorMessage"] = "Không thể phế chức tài khoản của chính mình!";
                    return RedirectToAction("SellerManagement");
                }

                var seller = await _context.Users
                    .Include(u => u.Role)
                    .Include(u => u.GameAccounts)
                    .FirstOrDefaultAsync(u => u.Id == id);

                if (seller == null)
                    return NotFound();

                // Kiểm tra xem có phải Seller không
                if (seller.Role?.Name != "Seller")
                {
                    TempData["ErrorMessage"] = "User này không phải Seller!";
                    return RedirectToAction("SellerManagement");
                }

                // Kiểm tra xem Seller có account đang đăng bán (chưa bán) hay không
                var activeAccounts = seller.GameAccounts
                    .Where(ga => !ga.IsSold)
                    .ToList();

                if (activeAccounts.Any())
                {
                    TempData["WarningMessage"] = $"Seller {seller.Username} đang đăng bán {activeAccounts.Count} account. Vui lòng xóa các account này trước khi phế chức.";
                    return RedirectToAction("SellerDetail", new { id });
                }

                // Nếu không có account đang đăng bán, phế chức
                var userRole = await _context.Roles
                    .FirstOrDefaultAsync(r => r.Name == "User");
                
                if (userRole != null)
                {
                    seller.RoleId = userRole.Id;
                    seller.UpdatedAt = DateTime.Now;
                    _context.Users.Update(seller);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = $"Đã phế chức {seller.Username} từ Seller về User.";
                }

                return RedirectToAction("SellerManagement");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi: " + ex.Message;
                return RedirectToAction("SellerDetail", new { id });
            }
        }

        // Xóa account của Seller trước khi phế chức
        [HttpPost]
        public async Task<IActionResult> DeleteSellerAccount(int sellerId, int accountId)
        {
            try
            {
                var account = await _context.GameAccounts.FindAsync(accountId);
                if (account == null)
                    return NotFound();

                if (account.SellerId != sellerId)
                {
                    TempData["ErrorMessage"] = "Account không thuộc về Seller này!";
                    return RedirectToAction("SellerDetail", new { id = sellerId });
                }

                _context.GameAccounts.Remove(account);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Đã xóa account #{account.Id}. Bạn có thể phế chức Seller ngay.";
                return RedirectToAction("SellerDetail", new { id = sellerId });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi: " + ex.Message;
                return RedirectToAction("SellerDetail", new { id = sellerId });
            }
        }

        #endregion

        #region === Report Management ===

        // Danh sách các báo cáo
        [HttpGet]
        public async Task<IActionResult> PurchaseReports()
        {
            var reports = await _context.PurchaseReports
                .Include(r => r.Buyer)
                .Include(r => r.Purchase)
                    .ThenInclude(p => p.GameAccount)
                        .ThenInclude(ga => ga.Game)
                .Include(r => r.Purchase)
                    .ThenInclude(p => p.GameAccount)
                        .ThenInclude(ga => ga.Seller)
                .OrderByDescending(r => r.ReportedDate)
                .ToListAsync();

            ViewBag.PendingReportsCount = reports.Count(r => r.Status == "Pending");
            return View(reports);
        }

        [HttpPost]
        public async Task<IActionResult> ApproveReport(int reportId)
        {
            try
            {
                var report = await _context.PurchaseReports
                    .Include(r => r.Purchase)
                        .ThenInclude(p => p.GameAccount)
                    .FirstOrDefaultAsync(r => r.Id == reportId);

                if (report == null)
                    return NotFound();

                report.Status = "Resolved";
                report.ResolvedDate = DateTime.Now;

                _context.PurchaseReports.Update(report);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "✅ Báo cáo đã được duyệt! Admin sẽ liên hệ với seller để yêu cầu hoàn tiền.";
                return RedirectToAction("PurchaseReports");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"❌ Lỗi: {ex.Message}";
                return RedirectToAction("PurchaseReports");
            }
        }

        [HttpPost]
        public async Task<IActionResult> DismissReport(int reportId)
        {
            try
            {
                var report = await _context.PurchaseReports
                    .FirstOrDefaultAsync(r => r.Id == reportId);

                if (report == null)
                    return NotFound();

                report.Status = "Dismissed";
                report.ResolvedDate = DateTime.Now;

                _context.PurchaseReports.Update(report);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "📋 Báo cáo đã bị bác bỏ.";
                return RedirectToAction("PurchaseReports");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"❌ Lỗi: {ex.Message}";
                return RedirectToAction("PurchaseReports");
            }
        }

        #endregion

        #region

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