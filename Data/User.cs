using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SkyCast.Data
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Username { get; set; }

        [Required]
        public string PasswordHash { get; set; } 
        
        public string DefaultCity { get; set; } = "Istanbul";
        public bool IsMetric { get; set; } = true;

        public virtual ICollection<FavoriteCity> FavoriteCities { get; set; } = new List<FavoriteCity>();
    }
}