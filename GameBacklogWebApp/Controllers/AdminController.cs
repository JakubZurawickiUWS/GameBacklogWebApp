using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GameBacklogWebApp.Data;
using GameBacklogWebApp.Models;

namespace GameBacklogWebApp.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly GameBacklogWebAppContext _context;

        public AdminController(GameBacklogWebAppContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> PendingGames()
        {
            var pending = await _context.Games
                .Include(g => g.Genre)
                .Include(g => g.Platform)
                .Where(g => g.ApprovalStatus == GameApprovalStatus.Pending)
                .ToListAsync();

            return View(pending);
        }

        [HttpPost]
        public async Task<IActionResult> Approve(int id)
        {
            var game = await _context.Games.FindAsync(id);
            if (game != null)
            {
                game.ApprovalStatus = GameApprovalStatus.Approved;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("PendingGames");
        }

        [HttpPost]
        public async Task<IActionResult> Reject(int id)
        {
            var game = await _context.Games.FindAsync(id);
            if (game != null)
            {
                game.ApprovalStatus = GameApprovalStatus.Rejected;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("PendingGames");
        }

        public async Task<IActionResult> Stats()
        {
            var totalGames = await _context.Games.CountAsync();
            var approvedGames = await _context.Games.CountAsync(g => g.ApprovalStatus == GameApprovalStatus.Approved);
            var users = await _context.Users.CountAsync();

            ViewData["TotalGames"] = totalGames;
            ViewData["ApprovedGames"] = approvedGames;
            ViewData["Users"] = users;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteComment(int id, int? returnGameId = null)
        {
            var comment = await _context.GameComments.FindAsync(id);
            if (comment != null)
            {
                _context.GameComments.Remove(comment);
                await _context.SaveChangesAsync();
                TempData["Message"] = "Komentarz został usunięty.";
            }
            else
            {
                TempData["Error"] = "Komentarz nie istnieje!";
            }

            if (returnGameId.HasValue)
                return RedirectToAction("Details", "Games", new { id = returnGameId.Value });
            else
                return RedirectToAction("PendingGames");
        }

    }
}
