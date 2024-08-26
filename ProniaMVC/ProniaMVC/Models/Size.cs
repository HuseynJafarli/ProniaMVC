using System.ComponentModel.DataAnnotations;

namespace ProniaMVC.Models
{
    public class Size : BaseEntity
    {
        [Required]
        [MaxLength(25)]
        public string Name { get; set; }
        public ICollection<ProductSize> ProductSizes { get; set; }
    }
}
