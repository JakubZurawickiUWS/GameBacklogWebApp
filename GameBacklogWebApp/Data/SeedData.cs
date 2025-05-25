using GameBacklogWebApp.Models;
using Microsoft.EntityFrameworkCore;

namespace GameBacklogWebApp.Data
{
    public static class SeedData
    {
        public static void Initialize(IServiceProvider serviceProvider)
        {
            using var context = new GameBacklogWebAppContext(
                serviceProvider.GetRequiredService<DbContextOptions<GameBacklogWebAppContext>>());

            if (context.Games.Any()) return;

            var pc = new Platform { Name = "PC" };
            var xbox = new Platform { Name = "Xbox" };
            var switchP = new Platform { Name = "Switch" };

            context.Platforms.AddRange(pc, xbox, switchP);

            var rpg = new Genre { Name = "RPG" };
            var fps = new Genre { Name = "FPS" };
            var strategy = new Genre { Name = "Strategia" };

            context.Genres.AddRange(rpg, fps, strategy);
            context.SaveChanges();

            context.Games.AddRange(
                new Game
                {
                    Title = "Skyrim",
                    EstimatedPlaytimeMinutes = 2000,
                    PlaytimeMinutes = 500,
                    Rating = 9,
                    Status = GameStatus.InProgress,
                    PlatformId = pc.Id,
                    GenreId = rpg.Id
                },
                new Game
                {
                    Title = "Halo",
                    EstimatedPlaytimeMinutes = 600,
                    PlaytimeMinutes = 600,
                    Rating = 8,
                    Status = GameStatus.Completed,
                    PlatformId = xbox.Id,
                    GenreId = fps.Id
                },
                new Game
                {
                    Title = "Fire Emblem",
                    EstimatedPlaytimeMinutes = 1200,
                    PlaytimeMinutes = 200,
                    Rating = 7,
                    Status = GameStatus.NotStarted,
                    PlatformId = switchP.Id,
                    GenreId = strategy.Id
                }
            );

            context.SaveChanges();
        }
    }
}
