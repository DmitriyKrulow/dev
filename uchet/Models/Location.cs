using System.ComponentModel.DataAnnotations;

namespace uchet.Models
{
    public class Location
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [MaxLength(200)]
        public string Name { get; set; }
        
        public string Description { get; set; }
        
        public ICollection<Property> Properties { get; set; }
    }
}