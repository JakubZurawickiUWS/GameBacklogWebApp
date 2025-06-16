using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace GameBacklogWebApp.Models
{
    public class GameComment
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }
        public virtual IdentityUser User { get; set; }

        [Required]
        public int GameId { get; set; }
        public virtual Game Game { get; set; }

        [Required]
        [MaxLength(1000)]
        public string Content { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
