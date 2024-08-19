using System.ComponentModel.DataAnnotations;

namespace ProniaMVC.Models
{
    public class Category : BaseEntity
    {
        [Required]
        [MaxLength(25)]
        public string Name { get; set; }

        //relational
        public ICollection<Product>? Products { get; set; }
    }
}
