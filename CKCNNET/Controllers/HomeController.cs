using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CKCNNET.Data;
using CKCNNET.Models;

namespace CKCNNET.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Hiển thị danh sách game
        public async Task<IActionResult> Index()
        {
            var games = await _context.Games.ToListAsync();
            return View(games);
        }

        // Hiển thị các account của game cụ thể
        public async Task<IActionResult> GameDetails(int gameId)
        {
            var game = await _context.Games.FindAsync(gameId);
            if (game == null)
                return NotFound();

            var gameAccounts = await _context.GameAccounts
                .Include(ga => ga.Game)
                .Include(ga => ga.Seller)
                .Include(ga => ga.Images)
                .Where(ga => ga.GameId == gameId && ga.IsApproved && !ga.IsSold)
                .OrderByDescending(ga => ga.CreatedAt)
                .ToListAsync();

            ViewBag.CurrentGame = game;
            return View(gameAccounts);
        }

        // Tìm kiếm account
        public async Task<IActionResult> Search(string search)
        {
            if (string.IsNullOrEmpty(search))
                return RedirectToAction("Index");

            var accounts = await _context.GameAccounts
                .Include(ga => ga.Game)
                .Include(ga => ga.Seller)
                .Include(ga => ga.Images)
                .Where(ga => ga.IsApproved && !ga.IsSold && 
                       (ga.Game.Name.Contains(search) || ga.Description.Contains(search)))
                .OrderByDescending(ga => ga.CreatedAt)
                .ToListAsync();

            ViewBag.SearchQuery = search;
            return View(accounts);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
