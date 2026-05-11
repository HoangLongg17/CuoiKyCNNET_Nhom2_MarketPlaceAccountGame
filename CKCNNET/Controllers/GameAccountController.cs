using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CKCNNET.Data;
using CKCNNET.Models;
using CKCNNET.Authorization;
using Microsoft.AspNetCore.Http;
using System.IO;

namespace CKCNNET.Controllers
{
    public class GameAccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public GameAccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        [RoleAuthorization("Seller", "Admin")]
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
                return RedirectToAction("Login", "Auth");

            var user = await _context.Users.FindAsync(userId.Value);
    
            if (user?.RoleId == null || (user.RoleId != 2 && user.RoleId != 3))
            {
                TempData["ErrorMessage"] = "Bạn không có quyền đăng bán account.";
                return RedirectToAction("Index", "Home");
            }

            var games = await _context.Games.ToListAsync();
            ViewBag.Games = games;
            return View();
        }

        [RoleAuthorization("Seller", "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(GameAccount model, List<IFormFile> images)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
                return RedirectToAction("Login", "Auth");

            var userRole = HttpContext.Session.GetString("UserRole");

            try
            {
                if (string.IsNullOrEmpty(model.Description))
                {
                    ModelState.AddModelError("Description", "Vui lòng nhập mô tả!");
                    var games = await _context.Games.ToListAsync();
                    ViewBag.Games = games;
                    return View(model);
                }

                if (model.Price <= 0)
                {
                    ModelState.AddModelError("Price", "Giá phải lớn hơn 0!");
                    var games = await _context.Games.ToListAsync();
                    ViewBag.Games = games;
                    return View(model);
                }

                if (model.GameId <= 0)
                {
                    ModelState.AddModelError("GameId", "Vui lòng chọn game!");
                    var games = await _context.Games.ToListAsync();
                    ViewBag.Games = games;
                    return View(model);
                }
                var game = await _context.Games.FindAsync(model.GameId);
                if (game == null)
                {
                    ModelState.AddModelError("GameId", "Game không tồn tại!");
                    var games = await _context.Games.ToListAsync();
                    ViewBag.Games = games;
                    return View(model);
                }

                var gameAccount = new GameAccount
                {
                    GameId = model.GameId,
                    SellerId = userId.Value,
                    Description = model.Description,
                    Price = model.Price,
                    Username = string.IsNullOrEmpty(model.Username) ? null : model.Username,
                    Password = string.IsNullOrEmpty(model.Password) ? null : model.Password,
                    ContactInfo = model.ContactInfo,
                    IsApproved = userRole == "Admin",
                    IsSold = false,
                    CreatedAt = DateTime.Now
                };

                _context.GameAccounts.Add(gameAccount);
                await _context.SaveChangesAsync();

                // Xử lý upload ảnh
                if (images != null && images.Count > 0)
                {
                    string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "game-accounts");
                    
                    if (!Directory.Exists(uploadsFolder))
                        Directory.CreateDirectory(uploadsFolder);

                    foreach (var image in images.Take(10)) // Giới hạn 10 ảnh
                    {
                        if (image.Length > 5 * 1024 * 1024) // 5MB
                        {
                            TempData["WarningMessage"] = "Một số ảnh bị vượt quá 5MB, bỏ qua!";
                            continue;
                        }

                        string fileName = $"{gameAccount.Id}_{Guid.NewGuid()}_{image.FileName}";
                        string filePath = Path.Combine(uploadsFolder, fileName);

                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await image.CopyToAsync(fileStream);
                        }

                        var gameAccountImage = new GameAccountImage
                        {
                            GameAccountId = gameAccount.Id,
                            ImagePath = $"/uploads/game-accounts/{fileName}",
                            FileName = image.FileName,
                            UploadedAt = DateTime.Now
                        };

                        _context.GameAccountImages.Add(gameAccountImage);
                    }

                    await _context.SaveChangesAsync();
                }

                string message = userRole == "Admin" 
                    ? "Đăng bán account thành công! Account sẽ hiển thị ngay trên marketplace."
                    : "Đăng bán account thành công! Admin sẽ duyệt yêu cầu của bạn sớm.";
                
                TempData["SuccessMessage"] = message;
                return RedirectToAction("MyListings", "GameAccount");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Lỗi: " + ex.Message);
                var games = await _context.Games.ToListAsync();
                ViewBag.Games = games;
                return View(model);
            }
        }

        [RoleAuthorization("Seller", "Admin")]
        [HttpGet]
        public async Task<IActionResult> MyListings()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
                return RedirectToAction("Login", "Auth");

            var userRole = HttpContext.Session.GetString("UserRole");

            IQueryable<GameAccount> query = _context.GameAccounts
                .Include(ga => ga.Game)
                .Include(ga => ga.Seller)
                .Include(ga => ga.Images)
                .Include(ga => ga.Purchases)
                    .ThenInclude(p => p.Buyer)
                .Include(ga => ga.PaymentProofs);

            if (userRole != "Admin")
            {
                query = query.Where(ga => ga.SellerId == userId.Value);
            }

            var accounts = await query
                .OrderByDescending(ga => ga.CreatedAt)
                .ToListAsync();

            return View(accounts);
        }

        [RoleAuthorization("Seller", "Admin")]
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
                return RedirectToAction("Login", "Auth");

            var account = await _context.GameAccounts
                .Include(ga => ga.Game)
                .Include(ga => ga.Images)
                .FirstOrDefaultAsync(ga => ga.Id == id);

            if (account == null)
                return NotFound();
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "Admin" && account.SellerId != userId.Value)
                return Unauthorized();
            if (account.IsSold)
            {
                TempData["ErrorMessage"] = "Không thể sửa account đã bán!";
                return RedirectToAction("MyListings");
            }

            var games = await _context.Games.ToListAsync();
            ViewBag.Games = games;
            ViewBag.UserRole = userRole;
            return View(account);
        }

        //Cập nhật account
        [RoleAuthorization("Seller", "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, GameAccount model, List<IFormFile> images)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
                return RedirectToAction("Login", "Auth");

            var account = await _context.GameAccounts.FindAsync(id);
            if (account == null)
                return NotFound();

            // Kiểm tra quyền
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "Admin" && account.SellerId != userId.Value)
                return Unauthorized();

            if (account.IsSold)
            {
                TempData["ErrorMessage"] = "Không thể sửa account đã bán!";
                return RedirectToAction("MyListings");
            }

            try
            {
                account.GameId = model.GameId;
                account.Description = model.Description;
                account.Price = model.Price;
                account.Username = string.IsNullOrEmpty(model.Username) ? null : model.Username;
                account.Password = string.IsNullOrEmpty(model.Password) ? null : model.Password;
                account.ContactInfo = model.ContactInfo;
                
                if (userRole == "Admin")
                {
                    account.IsApproved = true;
                }
                else if (userRole == "Seller")
                {
                    account.IsApproved = false;
                }

                _context.GameAccounts.Update(account);
                await _context.SaveChangesAsync();

                // Xử lý upload ảnh mới
                if (images != null && images.Count > 0)
                {
                    string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "game-accounts");
                    
                    if (!Directory.Exists(uploadsFolder))
                        Directory.CreateDirectory(uploadsFolder);

                    foreach (var image in images.Take(10))
                    {
                        if (image.Length > 5 * 1024 * 1024)
                        {
                            TempData["WarningMessage"] = "Một số ảnh bị vượt quá 5MB, bỏ qua!";
                            continue;
                        }

                        string fileName = $"{account.Id}_{Guid.NewGuid()}_{image.FileName}";
                        string filePath = Path.Combine(uploadsFolder, fileName);

                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await image.CopyToAsync(fileStream);
                        }

                        var gameAccountImage = new GameAccountImage
                        {
                            GameAccountId = account.Id,
                            ImagePath = $"/uploads/game-accounts/{fileName}",
                            FileName = image.FileName
                        };

                        _context.GameAccountImages.Add(gameAccountImage);
                    }

                    await _context.SaveChangesAsync();
                }

                TempData["SuccessMessage"] = "Cập nhật account thành công!";
                return RedirectToAction("Edit", new { id });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Lỗi: " + ex.Message);
                var games = await _context.Games.ToListAsync();
                ViewBag.Games = games;
                ViewBag.UserRole = userRole;
                account = await _context.GameAccounts
                    .Include(ga => ga.Images)
                    .FirstOrDefaultAsync(ga => ga.Id == account.Id);
                return View(account);
            }
        }

        //Xóa account
        [RoleAuthorization("Seller", "Admin")]
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
                return RedirectToAction("Login", "Auth");

            var account = await _context.GameAccounts.FindAsync(id);
            if (account == null)
                return NotFound();

            // Kiểm tra quyền
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "Admin" && account.SellerId != userId.Value)
                return Unauthorized();

            try
            {
                _context.GameAccounts.Remove(account);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Xóa account thành công!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi khi xóa: " + ex.Message;
            }

            return RedirectToAction("MyListings");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteImage(int imageId, int accountId)
        {
            try
            {
                var image = await _context.GameAccountImages.FindAsync(imageId);
                if (image == null)
                    return BadRequest("Ảnh không tồn tại");

                var account = await _context.GameAccounts.FindAsync(accountId);
                var userRole = HttpContext.Session.GetString("UserRole");
                var userId = HttpContext.Session.GetInt32("UserId");

                // Kiểm tra quyền
                if (userRole != "Admin" && (account?.SellerId != userId.Value))
                    return Unauthorized();

                // Xóa file từ server
                string filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", image.ImagePath.TrimStart('/'));
                if (System.IO.File.Exists(filePath))
                {
                    try
                    {
                        System.IO.File.Delete(filePath);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Lỗi xóa file: {ex.Message}");
                    }
                }

                _context.GameAccountImages.Remove(image);
                await _context.SaveChangesAsync();

                return Ok("Xóa ảnh thành công");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi: {ex.Message}");
            }
        }
    }
}