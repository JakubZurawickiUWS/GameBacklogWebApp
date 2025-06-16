using System.ComponentModel.DataAnnotations;

namespace GameBacklogWebApp.Models
{
    public class UserWallet
    {
        [Key]
        public string UserId { get; set; }
        public int Currency { get; set; } = 0;
    }
}
