using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using GameBacklogWebApp.Data;
using GameBacklogWebApp.Models;

namespace GameBacklogWebApp.Controllers
{
    public class GamesController : Controller
    {
        private readonly GameBacklogWebAppContext _context;

        public GamesController(GameBacklogWebAppContext context)
        {
            _context = context;
        }

        private List<SelectListItem> GetStatusSelectList(GameStatus? selected = null)
        {
            return Enum.GetValues(typeof(GameStatus))
                .Cast<GameStatus>()
                .Select(s => new SelectListItem
                {
                    Text = s.ToString(),
                    Value = ((int)s).ToString(),
                    Selected = s == selected
                }).ToList();
        }

        // GET: Games
        public async Task<IActionResult> Index(string searchString, int? platformId, int? genreId, GameStatus? status, string sortOrder, int page = 1)
        {
            const int PageSize = 5;

            var query = _context.Games
                .Include(g => g.Platform)
                .Include(g => g.Genre)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
                query = query.Where(g => g.Title.Contains(searchString));

            if (platformId.HasValue)
                query = query.Where(g => g.PlatformId == platformId);

            if (status.HasValue)
                query = query.Where(g => g.Status == status);

            if (genreId.HasValue)
                query = query.Where(g => g.GenreId == genreId);

            ViewData["CurrentSort"] = sortOrder;
            ViewData["TitleSortParam"] = sortOrder == "title_asc" ? "title_desc" : "title_asc";
            query = sortOrder switch
            {
                "title_desc" => query.OrderByDescending(g => g.Title),
                _ => query.OrderBy(g => g.Title),
            };

            int totalItems = await query.CountAsync();
            var games = await query
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            ViewData["Platforms"] = await _context.Platforms.ToListAsync();
            ViewData["Genres"] = await _context.Genres.ToListAsync();
            ViewData["Statuses"] = Enum.GetValues(typeof(GameStatus));
            ViewData["CurrentPage"] = page;
            ViewData["TotalPages"] = (int)Math.Ceiling(totalItems / (double)PageSize);

            return View(games);
        }

        // GET: Games/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var game = await _context.Games
                .Include(g => g.Genre)
                .Include(g => g.Platform)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (game == null)
                return NotFound();

            return View(game);
        }

        // GET: Games/Create
        public IActionResult Create()
        {
            ViewData["GenreId"] = new SelectList(_context.Genres, "Id", "Name");
            ViewData["PlatformId"] = new SelectList(_context.Platforms, "Id", "Name");
            ViewData["StatusList"] = GetStatusSelectList();
            return View();
        }

        // POST: Games/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Title,EstimatedPlaytimeMinutes,PlaytimeMinutes,Rating,Status,PlatformId,GenreId")] Game game)
        {
            if (!ModelState.IsValid)
            {
                ViewData["GenreId"] = new SelectList(_context.Genres, "Id", "Name", game.GenreId);
                ViewData["PlatformId"] = new SelectList(_context.Platforms, "Id", "Name", game.PlatformId);
                ViewData["StatusList"] = GetStatusSelectList(game.Status);
                return View(game);
            }

            _context.Add(game);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: Games/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var game = await _context.Games.FindAsync(id);
            if (game == null)
                return NotFound();

            ViewData["GenreId"] = new SelectList(_context.Genres, "Id", "Name", game.GenreId);
            ViewData["PlatformId"] = new SelectList(_context.Platforms, "Id", "Name", game.PlatformId);
            ViewData["StatusList"] = GetStatusSelectList(game.Status);
            return View(game);
        }

        // POST: Games/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,EstimatedPlaytimeMinutes,PlaytimeMinutes,Rating,Status,PlatformId,GenreId")] Game game)
        {
            if (id != game.Id)
                return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(game);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!GameExists(game.Id))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }

            ViewData["GenreId"] = new SelectList(_context.Genres, "Id", "Name", game.GenreId);
            ViewData["PlatformId"] = new SelectList(_context.Platforms, "Id", "Name", game.PlatformId);
            ViewData["StatusList"] = GetStatusSelectList(game.Status);
            return View(game);
        }

        // GET: Games/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var game = await _context.Games
                .Include(g => g.Genre)
                .Include(g => g.Platform)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (game == null)
                return NotFound();

            return View(game);
        }

        // POST: Games/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var game = await _context.Games.FindAsync(id);
            if (game != null)
                _context.Games.Remove(game);

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool GameExists(int id)
        {
            return _context.Games.Any(e => e.Id == id);
        }

        public async Task<IActionResult> Stats()
        {
            var games = await _context.Games.ToListAsync();

            var stats = new GameStats
            {
                TotalGames = games.Count,
                CompletedGames = games.Count(g => g.Status == GameStatus.Completed),
                AverageRating = games.Any() ? games.Average(g => g.Rating) : 0,
                TotalPlaytime = games.Sum(g => g.PlaytimeMinutes)
            };

            return View(stats);
        }

        // POST: Games/Play/5
        [HttpPost]
        public async Task<IActionResult> Play(int id)
        {
            var game = await _context.Games.FindAsync(id);
            if (game == null)
                return NotFound();

            game.PlaytimeMinutes += 1;

            if (game.PlaytimeMinutes > 0 && game.Status == GameStatus.NotStarted)
                game.Status = GameStatus.InProgress;

            if (game.PlaytimeMinutes >= game.EstimatedPlaytimeMinutes && game.Status != GameStatus.Completed)
                game.Status = GameStatus.Completed;

            _context.Update(game);
            await _context.SaveChangesAsync();

            return Json(new
            {
                newPlaytime = game.PlaytimeMinutes,
                newStatus = game.Status.ToString(),
                newProgress = game.EstimatedPlaytimeMinutes > 0
                    ? Math.Min((int)((double)game.PlaytimeMinutes / game.EstimatedPlaytimeMinutes * 100), 100)
                    : 0
            });
        }
    }
}
