using System.ComponentModel.DataAnnotations;

namespace ProniaMVC.Models
{
    public class Tag : BaseEntity
    {
        [Required]
        [MaxLength(25)]
        public string Name { get; set; }
    }
}
