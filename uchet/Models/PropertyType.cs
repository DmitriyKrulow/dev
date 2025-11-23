using System.ComponentModel.DataAnnotations;

namespace uchet.Models
{
    public class PropertyType
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string Name { get; set; }
        
        public string Description { get; set; }
        
        public ICollection<Property> Properties { get; set; }
    }
}