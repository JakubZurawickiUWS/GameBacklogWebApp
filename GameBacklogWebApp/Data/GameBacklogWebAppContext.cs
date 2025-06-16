using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using GameBacklogWebApp.Models;

namespace GameBacklogWebApp.Data
{
    public class GameBacklogWebAppContext : IdentityDbContext
    {
        public GameBacklogWebAppContext(DbContextOptions<GameBacklogWebAppContext> options)
            : base(options)
        {
        }

        public DbSet<Game> Games { get; set; }
        public DbSet<Genre> Genres { get; set; }
        public DbSet<Platform> Platforms { get; set; }
        public DbSet<UserWallet> UserWallets { get; set; }
        public DbSet<GameComment> GameComments { get; set; }

    }
}
