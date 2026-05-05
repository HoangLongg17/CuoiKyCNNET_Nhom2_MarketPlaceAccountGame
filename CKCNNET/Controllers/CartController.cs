using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CKCNNET.Data;
using CKCNNET.Models;

namespace CKCNNET.Controllers
{
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CartController(ApplicationDbContext context)
        {
            _context = context;
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

        // Thanh toán giỏ hàng: tạo Purchases, đánh dấu GameAccount.IsSold = true
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

                    var purchase = new Purchase
                    {
                        BuyerId = userId.Value,
                        GameAccountId = account.Id,
                        Amount = account.Price,
                        Status = "Completed",
                        PurchaseDate = DateTime.Now,
                        CompletedDate = DateTime.Now,
                        AccountUsername = account.Username,
                        AccountPassword = account.Password
                    };

                    account.IsSold = true;
                    _context.Purchases.Add(purchase);
                    _context.GameAccounts.Update(account);

                    // Xoá mục giỏ tương ứng
                    _context.Carts.Remove(item);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["SuccessMessage"] = "Thanh toán thành công. Thông tin account đã được ghi vào lịch sử mua.";
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                TempData["ErrorMessage"] = "Lỗi khi thanh toán: " + ex.Message;
            }

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

            return View(purchases);
        }
    }
}