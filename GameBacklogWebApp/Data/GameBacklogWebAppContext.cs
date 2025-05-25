using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using GameBacklogWebApp.Models;

namespace GameBacklogWebApp.Data
{
    public class GameBacklogWebAppContext : DbContext
    {
        public GameBacklogWebAppContext (DbContextOptions<GameBacklogWebAppContext> options)
            : base(options)
        {
        }

        public DbSet<GameBacklogWebApp.Models.Game> Games { get; set; } = default!;
        public DbSet<Genre> Genres { get; set; }
        public DbSet<Platform> Platforms { get; set; }

    }
}
