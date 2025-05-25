using System.ComponentModel.DataAnnotations;

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

        [Display(Name = "Postęp (%)")]
        public string ProgressPercent =>
            EstimatedPlaytimeMinutes == 0 ? "0%" :
            $"{Math.Min((int)((double)PlaytimeMinutes / EstimatedPlaytimeMinutes * 100), 100)}%";
    }
}
