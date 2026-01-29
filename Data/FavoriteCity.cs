using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SkyCast.Data
{
    public class FavoriteCity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string CityName { get; set; }

        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; }
    }
}