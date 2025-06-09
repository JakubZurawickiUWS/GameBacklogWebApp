using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace GameBacklogWebApp.Models
{
    public enum GameStatus
    {
        NotStarted,
        InProgress,
        Completed
    }

    public class Game
    {
        public int Id { get; set; }

        [Required]
        public string Title { get; set; }

        [Display(Name = "Planowany czas (min)")]
        [Range(1, 99999)]
        public int EstimatedPlaytimeMinutes { get; set; }

        [Display(Name = "Czas grany (min)")]
        [Range(0, 99999)]
        public int PlaytimeMinutes { get; set; }

        [Range(1, 10)]
        public int Rating { get; set; }

        public GameStatus Status { get; set; }

        [Display(Name = "Platforma")]
        public int PlatformId { get; set; }
        public virtual Platform? Platform { get; set; }

        [Display(Name = "Gatunek")]
        public int GenreId { get; set; }
        public virtual Genre? Genre { get; set; }

        public string ProgressPercent
        {
            get
            {
                if (EstimatedPlaytimeMinutes == 0) return "0%";
                var percent = (int)((double)PlaytimeMinutes / EstimatedPlaytimeMinutes * 100);
                return $"{Math.Min(percent, 100)}%";
            }
        }

        public string? UserId { get; set; }
        public virtual IdentityUser? User { get; set; }
    }

}
