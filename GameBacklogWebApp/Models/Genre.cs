using System.ComponentModel.DataAnnotations;

namespace GameBacklogWebApp.Models
{
    public class Genre
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        public ICollection<Game> Games { get; set; }
    }
}
