using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CKCNNET.Data;
using CKCNNET.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System.IO;
using CKCNNET.Authorization;

namespace CKCNNET.Controllers
{
    [RoleAuthorization("User", "Seller", "Admin")]
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public CartController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // Hiển thị giỏ hàng của người dùng hiện tại
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập để xem giỏ hàng.";
                return RedirectToAction("Login", "Auth");
            }

            var items = await _context.Carts
                .Include(c => c.GameAccount)
                .ThenInclude(ga => ga.Game)
                .Include(c => c.GameAccount)
                .ThenInclude(ga => ga.Seller)
                .Where(c => c.UserId == userId.Value)
                .OrderByDescending(c => c.AddedAt)
                .ToListAsync();

            return View(items);
        }

        // Thêm game account vào giỏ
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToCart(int accountId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập để thêm vào giỏ hàng.";
                return RedirectToAction("Login", "Auth");
            }

            var account = await _context.GameAccounts.FindAsync(accountId);
            if (account == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy account.";
                return Redirect(Request.Headers["Referer"].ToString());
            }

            if (!account.IsApproved)
            {
                TempData["WarningMessage"] = "Account này chưa được duyệt bởi Admin.";
                return Redirect(Request.Headers["Referer"].ToString());
            }

            if (account.IsSold)
            {
                TempData["WarningMessage"] = "Account này đã bán.";
                return Redirect(Request.Headers["Referer"].ToString());
            }

            var exists = await _context.Carts
                .AnyAsync(c => c.UserId == userId.Value && c.GameAccountId == accountId);

            if (exists)
            {
                TempData["InfoMessage"] = "Giỏ hàng của bạn đã có sản phẩm này rồi.";
                return Redirect(Request.Headers["Referer"].ToString());
            }

            var cart = new Cart
            {
                UserId = userId.Value,
                GameAccountId = accountId,
                AddedAt = DateTime.Now
            };

            _context.Carts.Add(cart);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đã thêm vào giỏ hàng.";
            return Redirect(Request.Headers["Referer"].ToString());
        }

        // Xóa mục trong giỏ
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập.";
                return RedirectToAction("Login", "Auth");
            }

            var cart = await _context.Carts.FindAsync(id);
            if (cart == null || cart.UserId != userId.Value)
            {
                TempData["ErrorMessage"] = "Mục giỏ hàng không tồn tại.";
                return RedirectToAction("Index");
            }

            _context.Carts.Remove(cart);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đã xóa mục khỏi giỏ hàng.";
            return RedirectToAction("Index");
        }

        // Thanh toán giỏ hàng: tạo Purchases (Pending), đánh dấu GameAccount.IsSold = true
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập để mua hàng.";
                return RedirectToAction("Login", "Auth");
            }

            var cartItems = await _context.Carts
                .Include(c => c.GameAccount)
                .Where(c => c.UserId == userId.Value)
                .ToListAsync();

            if (!cartItems.Any())
            {
                TempData["InfoMessage"] = "Giỏ hàng trống.";
                return RedirectToAction("Index");
            }

            // Nếu có bất kỳ item nào đã bị mua (IsSold == true) thì không cho checkout toàn bộ
            if (cartItems.Any(c => c.GameAccount == null || c.GameAccount.IsSold))
            {
                TempData["ErrorMessage"] = "trong giỏ hàng của bạn có một hoặc nhiều Account không còn nữa";
                return RedirectToAction("Index");
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                foreach (var item in cartItems)
                {
                    var account = await _context.GameAccounts.FindAsync(item.GameAccountId);
                    if (account == null || account.IsSold)
                    {
                        // Should not happen because of pre-check, but keep safety
                        continue;
                    }

                    // Create purchase in Pending state
                    var purchase = new Purchase
                    {
                        BuyerId = userId.Value,
                        GameAccountId = account.Id,
                        Amount = account.Price,
                        Status = "Pending",
                        PurchaseDate = DateTime.Now
                        // AccountUsername/Password and CompletedDate left null until seller approves
                    };

                    // Prevent others from buying while awaiting payment (reusing existing IsSold check)
                    account.IsSold = true;

                    _context.Purchases.Add(purchase);
                    _context.GameAccounts.Update(account);

                    // Xoá mục giỏ tương ứng
                    _context.Carts.Remove(item);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["SuccessMessage"] = "Yêu cầu mua đã tạo. Vui lòng tải ảnh minh chứng chuyển khoản trong lịch sử mua để Seller kiểm tra.";
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                TempData["ErrorMessage"] = "Lỗi khi thanh toán: " + ex.Message;
            }

            return RedirectToAction("Purchases");
        }

        // Upload payment proof (buyer)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadProof(int purchaseId, IFormFile proofImage)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập.";
                return RedirectToAction("Login", "Auth");
            }

            var purchase = await _context.Purchases
                .Include(p => p.GameAccount)
                .ThenInclude(ga => ga.Seller)
                .FirstOrDefaultAsync(p => p.Id == purchaseId);

            if (purchase == null || purchase.BuyerId != userId.Value)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đơn hàng.";
                return RedirectToAction("Purchases");
            }

            if (purchase.Status != "Pending")
            {
                TempData["WarningMessage"] = "Đơn hàng không ở trạng thái chờ thanh toán.";
                return RedirectToAction("Purchases");
            }

            if (proofImage == null || proofImage.Length == 0)
            {
                TempData["ErrorMessage"] = "Vui lòng chọn ảnh minh chứng.";
                return RedirectToAction("Purchases");
            }

            // Save file
            var uploadsRoot = Path.Combine(_env.WebRootPath ?? "wwwroot", "uploads", "payments");
            if (!Directory.Exists(uploadsRoot))
            {
                Directory.CreateDirectory(uploadsRoot);
            }

            var uniqueFileName = $"{Guid.NewGuid()}{Path.GetExtension(proofImage.FileName)}";
            var filePath = Path.Combine(uploadsRoot, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await proofImage.CopyToAsync(stream);
            }

            var proof = new PaymentProof
            {
                BuyerId = userId.Value,
                GameAccountId = purchase.GameAccountId,
                FileName = uniqueFileName,
                UploadedAt = DateTime.Now
            };

            // If a proof exists for same buyer/account, replace it (optional)
            var existing = await _context.PaymentProofs
                .FirstOrDefaultAsync(pp => pp.BuyerId == proof.BuyerId && pp.GameAccountId == proof.GameAccountId);

            if (existing != null)
            {
                // delete old file if exists
                try
                {
                    var oldPath = Path.Combine(uploadsRoot, existing.FileName ?? "");
                    if (System.IO.File.Exists(oldPath))
                    {
                        System.IO.File.Delete(oldPath);
                    }
                }
                catch { /* ignore */ }

                existing.FileName = proof.FileName;
                existing.UploadedAt = proof.UploadedAt;
                _context.PaymentProofs.Update(existing);
            }
            else
            {
                _context.PaymentProofs.Add(proof);
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Ảnh minh chứng đã được tải lên. Vui lòng chờ Seller kiểm tra.";
            return RedirectToAction("Purchases");
        }

        // Seller approves a pending purchase
        [RoleAuthorization("Seller", "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApprovePurchase(int purchaseId)
        {
            var sellerId = HttpContext.Session.GetInt32("UserId");
            if (!sellerId.HasValue)
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập.";
                return RedirectToAction("Login", "Auth");
            }

            var purchase = await _context.Purchases
                .Include(p => p.GameAccount)
                .FirstOrDefaultAsync(p => p.Id == purchaseId);

            if (purchase == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đơn hàng.";
                return RedirectToAction("Index", "Home");
            }

            if (purchase.GameAccount == null || purchase.GameAccount.SellerId != sellerId.Value)
            {
                TempData["ErrorMessage"] = "Bạn không có quyền xử lý đơn này.";
                return RedirectToAction("Index", "Home");
            }

            if (purchase.Status != "Pending")
            {
                TempData["WarningMessage"] = "Đơn hàng không ở trạng thái chờ.";
                return RedirectToAction("Index", "Home");
            }

            // Mark completed and copy credentials to purchase
            purchase.Status = "Completed";
            purchase.CompletedDate = DateTime.Now;
            purchase.AccountUsername = purchase.GameAccount.Username;
            purchase.AccountPassword = purchase.GameAccount.Password;

            _context.Purchases.Update(purchase);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đã xác nhận thanh toán. Thông tin đăng nhập đã hiển thị trong lịch sử mua.";
            return RedirectToAction("Purchases");
        }

        // Seller rejects a pending purchase
        [RoleAuthorization("Seller", "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectPurchase(int purchaseId, string? reason)
        {
            var sellerId = HttpContext.Session.GetInt32("UserId");
            if (!sellerId.HasValue)
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập.";
                return RedirectToAction("Login", "Auth");
            }

            var purchase = await _context.Purchases
                .Include(p => p.GameAccount)
                .FirstOrDefaultAsync(p => p.Id == purchaseId);

            if (purchase == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đơn hàng.";
                return RedirectToAction("Index", "Home");
            }

            if (purchase.GameAccount == null || purchase.GameAccount.SellerId != sellerId.Value)
            {
                TempData["ErrorMessage"] = "Bạn không có quyền xử lý đơn này.";
                return RedirectToAction("Index", "Home");
            }

            if (purchase.Status != "Pending")
            {
                TempData["WarningMessage"] = "Đơn hàng không ở trạng thái chờ.";
                return RedirectToAction("Index", "Home");
            }

            // Reject: set reason, mark purchase rejected and reopen account for sale
            purchase.Status = "Rejected";
            purchase.RejectionReason = reason;
            purchase.CompletedDate = DateTime.Now;

            if (purchase.GameAccount != null)
            {
                purchase.GameAccount.IsSold = false; // make it available again
                _context.GameAccounts.Update(purchase.GameAccount);
            }

            _context.Purchases.Update(purchase);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đã từ chối đơn hàng và mở lại account để bán. Người mua sẽ thấy lý do từ chối.";
            return RedirectToAction("Purchases");
        }

        // Lịch sử mua của người dùng
        [HttpGet]
        public async Task<IActionResult> Purchases()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập để xem lịch sử mua.";
                return RedirectToAction("Login", "Auth");
            }

            var purchases = await _context.Purchases
                .Include(p => p.GameAccount)
                .ThenInclude(ga => ga.Game)
                .Include(p => p.GameAccount)
                .ThenInclude(ga => ga.Seller)
                .Where(p => p.BuyerId == userId.Value)
                .OrderByDescending(p => p.PurchaseDate)
                .ToListAsync();

            // Map any existing payment proof filenames for quick lookup
            var proofMap = new System.Collections.Generic.Dictionary<int, string?>();
            foreach (var p in purchases)
            {
                var proof = await _context.PaymentProofs
                    .FirstOrDefaultAsync(pp => pp.BuyerId == p.BuyerId && pp.GameAccountId == p.GameAccountId);
                proofMap[p.Id] = proof?.FileName;
            }

            ViewBag.PaymentProofs = proofMap;

            return View(purchases);
        }
    }
}