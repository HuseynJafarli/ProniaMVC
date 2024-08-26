using System.ComponentModel.DataAnnotations;

namespace ProniaMVC.Models
{
    public class Color : BaseEntity
    {
        [Required]
        [MaxLength(25)]
        public string Name { get; set; }
        public ICollection<ProductColor> ProductColors { get; set; }
    }
}
