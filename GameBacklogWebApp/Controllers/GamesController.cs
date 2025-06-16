using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using GameBacklogWebApp.Data;
using GameBacklogWebApp.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;

namespace GameBacklogWebApp.Controllers
{
    [Authorize]
    public class GamesController : Controller
    {
        private readonly GameBacklogWebAppContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public GamesController(GameBacklogWebAppContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
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
            var userId = _userManager.GetUserId(User);

            var query = _context.Games
                .Where(g => g.UserId == userId)
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

            var comments = await _context.GameComments
                .Include(c => c.User)
                .Where(c =>
                    c.Game.Title == game.Title &&
                    c.Game.PlatformId == game.PlatformId
                )
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
            ViewBag.Comments = comments;

            var userId = _userManager.GetUserId(User);
            var userOwnsGame = await _context.Games.AnyAsync(g =>
                g.Title == game.Title && g.PlatformId == game.PlatformId && g.UserId == userId
            );
            ViewBag.UserOwnsGame = userOwnsGame;

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
        public async Task<IActionResult> Create([Bind("Id,Title,EstimatedPlaytimeMinutes,PlaytimeMinutes,Rating,Status,PlatformId,GenreId,Price")] Game game)
        {
            game.UserId = _userManager.GetUserId(User);
            game.ApprovalStatus = GameApprovalStatus.Pending;
            game.OriginalCreatorId = game.UserId;

            bool exists = await _context.Games.AnyAsync(g => g.Title.ToLower() == game.Title.ToLower());
            if (exists)
            {
                ModelState.AddModelError("Title", "Gra o takiej nazwie już istnieje!");
            }

            if (!ModelState.IsValid)
            {
                ViewData["GenreId"] = new SelectList(_context.Genres, "Id", "Name", game.GenreId);
                ViewData["PlatformId"] = new SelectList(_context.Platforms, "Id", "Name", game.PlatformId);
                ViewData["StatusList"] = GetStatusSelectList(game.Status);
                return View(game);
            }

            _context.Add(game);
            await _context.SaveChangesAsync();
            TempData["Message"] = "Gra została zgłoszona i czeka na akceptację admina!";
            return RedirectToAction(nameof(Index));
        }

        // GET: Games/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var userId = _userManager.GetUserId(User);
            var game = await _context.Games.FirstOrDefaultAsync(g => g.Id == id && g.UserId == userId);
            if (game == null)
                return NotFound();

            if (game.OriginalCreatorId != userId)
            {
                TempData["Error"] = "Nie możesz edytować gry dodanej z katalogu!";
                return RedirectToAction(nameof(Index));
            }


            ViewData["GenreId"] = new SelectList(_context.Genres, "Id", "Name", game.GenreId);
            ViewData["PlatformId"] = new SelectList(_context.Platforms, "Id", "Name", game.PlatformId);
            ViewData["StatusList"] = GetStatusSelectList(game.Status);
            return View(game);
        }

        // POST: Games/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,EstimatedPlaytimeMinutes,PlaytimeMinutes,Rating,Status,PlatformId,GenreId,Price")] Game game)
        {
            var userId = _userManager.GetUserId(User);

            if (id != game.Id)
                return NotFound();

            var gameToUpdate = await _context.Games.FirstOrDefaultAsync(g => g.Id == id && g.UserId == userId);
            if (gameToUpdate == null)
                return NotFound();

            if (gameToUpdate.OriginalCreatorId != userId)
            {
                TempData["Error"] = "Nie możesz edytować gry dodanej z katalogu!";
                return RedirectToAction(nameof(Index));
            }

            if (ModelState.IsValid)
            {
                try
                {
                    //gameToUpdate.Title = game.Title;
                    gameToUpdate.EstimatedPlaytimeMinutes = game.EstimatedPlaytimeMinutes;
                    gameToUpdate.PlaytimeMinutes = game.PlaytimeMinutes;
                    gameToUpdate.Rating = game.Rating;
                    gameToUpdate.Status = game.Status;
                    gameToUpdate.PlatformId = game.PlatformId;
                    gameToUpdate.GenreId = game.GenreId;
                    gameToUpdate.ApprovalStatus = GameApprovalStatus.Pending;

                    _context.Update(gameToUpdate);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!GameExists(game.Id, userId))
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

            var userId = _userManager.GetUserId(User);
            var game = await _context.Games
                .Include(g => g.Genre)
                .Include(g => g.Platform)
                .FirstOrDefaultAsync(m => m.Id == id && m.UserId == userId);
            if (game == null)
                return NotFound();

            return View(game);
        }

        // POST: Games/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var userId = _userManager.GetUserId(User);
            var game = await _context.Games.FirstOrDefaultAsync(g => g.Id == id && g.UserId == userId);
            if (game != null)
                _context.Games.Remove(game);

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool GameExists(int id, string userId)
        {
            return _context.Games.Any(e => e.Id == id && e.UserId == userId);
        }

        public async Task<IActionResult> Stats()
        {
            var userId = _userManager.GetUserId(User);
            var games = await _context.Games.Where(g => g.UserId == userId).ToListAsync();

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
            var userId = _userManager.GetUserId(User);
            var game = await _context.Games.FirstOrDefaultAsync(g => g.Id == id && g.UserId == userId);
            if (game == null)
                return NotFound();

            game.PlaytimeMinutes += 1;

            if (game.PlaytimeMinutes > 0 && game.Status == GameStatus.NotStarted)
                game.Status = GameStatus.InProgress;

            if (game.PlaytimeMinutes >= game.EstimatedPlaytimeMinutes && game.Status != GameStatus.Completed)
                game.Status = GameStatus.Completed;

            var wallet = await _context.UserWallets.FindAsync(userId);
            if (wallet == null)
            {
                wallet = new UserWallet { UserId = userId, Currency = 0 };
                _context.UserWallets.Add(wallet);
            }

            var rand = new Random();
            bool gainedCoin = rand.NextDouble() < 0.5;
            int coinsGained = 0;
            if (gainedCoin)
            {
                wallet.Currency += 1;
                coinsGained = 1;
            }

            _context.Update(game);
            await _context.SaveChangesAsync();

            return Json(new
            {
                newPlaytime = game.PlaytimeMinutes,
                newStatus = game.Status.ToString(),
                newProgress = game.EstimatedPlaytimeMinutes > 0
                    ? Math.Min((int)((double)game.PlaytimeMinutes / game.EstimatedPlaytimeMinutes * 100), 100)
                    : 0,
                coinsGained = coinsGained,
                newCurrency = wallet.Currency
            });
        }

        // GET: Games/GetPlaytime/5
        [HttpGet]
        public async Task<IActionResult> GetPlaytime(int id)
        {
            var userId = _userManager.GetUserId(User);
            var game = await _context.Games
                .Include(g => g.Platform)
                .Include(g => g.Genre)
                .FirstOrDefaultAsync(g => g.Id == id && g.UserId == userId);

            if (game == null) return NotFound();

            var progress = (game.EstimatedPlaytimeMinutes > 0)
                ? Math.Min((int)Math.Round((double)game.PlaytimeMinutes / game.EstimatedPlaytimeMinutes * 100), 100)
                : 0;

            return Json(new
            {
                playtime = game.PlaytimeMinutes,
                progress = progress,
                status = game.Status.ToString()
            });
        }

        // W GamesController.cs
        public async Task<IActionResult> Catalog()
        {
            var games = await _context.Games
                .Include(g => g.Platform)
                .Where(g => g.ApprovalStatus == GameApprovalStatus.Approved)
                .ToListAsync();

            var userId = _userManager.GetUserId(User);
            var wallet = await _context.UserWallets.FindAsync(userId);
            ViewData["UserCurrency"] = wallet?.Currency ?? 0;

            var uniqueGames = games
                .GroupBy(g => new { g.Title, g.PlatformId })
                .Select(g => g.First())
                .ToList();

            return View(uniqueGames);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToLibrary(int gameId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            var game = await _context.Games.FindAsync(gameId);
            if (game == null)
                return NotFound();

            bool alreadyExists = await _context.Games.AnyAsync(g =>
                g.UserId == user.Id &&
                g.Title == game.Title &&
                g.PlatformId == game.PlatformId
            );
            if (alreadyExists)
            {
                TempData["Error"] = "Masz już tę grę w swojej bibliotece!";
                return RedirectToAction("Catalog");
            }

            var newGame = new Game
            {
                Title = game.Title,
                EstimatedPlaytimeMinutes = game.EstimatedPlaytimeMinutes,
                PlaytimeMinutes = 0,
                Rating = 0,
                Status = 0,
                PlatformId = game.PlatformId,
                GenreId = game.GenreId,
                UserId = user.Id,
                ApprovalStatus = GameApprovalStatus.Approved,
                OriginalCreatorId = game.UserId,
                Price = game.Price
            };
    

            _context.Games.Add(newGame);
            await _context.SaveChangesAsync();

            TempData["Message"] = "Gra została dodana do Twojej biblioteki!";
            return RedirectToAction("Catalog");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Buy(int gameId)
        {
            var userId = _userManager.GetUserId(User);
            var wallet = await _context.UserWallets.FindAsync(userId);
            if (wallet == null)
            {
                wallet = new UserWallet { UserId = userId, Currency = 0 };
                _context.UserWallets.Add(wallet);
                await _context.SaveChangesAsync();
            }

            var game = await _context.Games.FindAsync(gameId);
            if (game == null)
                return NotFound();

            bool alreadyExists = await _context.Games.AnyAsync(g =>
                g.UserId == userId &&
                g.Title == game.Title &&
                g.PlatformId == game.PlatformId
            );
            if (alreadyExists)
            {
                TempData["Error"] = "Masz już tę grę w swojej bibliotece!";
                return RedirectToAction("Catalog");
            }

            if (wallet.Currency < game.Price)
            {
                TempData["Error"] = "Nie masz wystarczającej ilości waluty!";
                return RedirectToAction("Catalog");
            }

            wallet.Currency -= game.Price;

            if (!string.IsNullOrEmpty(game.OriginalCreatorId))
            {
                var creatorWallet = await _context.UserWallets.FindAsync(game.OriginalCreatorId);
                if (creatorWallet == null)
                {
                    creatorWallet = new UserWallet { UserId = game.OriginalCreatorId, Currency = 0 };
                    _context.UserWallets.Add(creatorWallet);
                }
                int reward = (int)(game.Price * 0.7);
                creatorWallet.Currency += reward;
            }

            var newGame = new Game
            {
                Title = game.Title,
                EstimatedPlaytimeMinutes = game.EstimatedPlaytimeMinutes,
                PlaytimeMinutes = 0,
                Rating = 0,
                Status = 0,
                PlatformId = game.PlatformId,
                GenreId = game.GenreId,
                UserId = userId,
                ApprovalStatus = GameApprovalStatus.Approved,
                OriginalCreatorId = game.UserId,
                Price = game.Price
            };

            _context.Games.Add(newGame);
            await _context.SaveChangesAsync();

            TempData["Message"] = "Gra została kupiona i dodana do Twojej biblioteki!";
            return RedirectToAction("Catalog");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddComment(int gameId, string content)
        {
            var userId = _userManager.GetUserId(User);

            bool ownsGame = await _context.Games.AnyAsync(g => g.Id == gameId && g.UserId == userId);

            if (!ownsGame)
            {
                TempData["Error"] = "Możesz komentować tylko gry, które posiadasz!";
                return RedirectToAction("Details", new { id = gameId });
            }

            if (string.IsNullOrWhiteSpace(content))
            {
                TempData["Error"] = "Komentarz nie może być pusty!";
                return RedirectToAction("Details", new { id = gameId });
            }

            var comment = new GameComment
            {
                UserId = userId,
                GameId = gameId,
                Content = content
            };
            _context.GameComments.Add(comment);
            await _context.SaveChangesAsync();

            TempData["Message"] = "Komentarz dodany!";
            return RedirectToAction("Details", new { id = gameId });
        }

    }
}
