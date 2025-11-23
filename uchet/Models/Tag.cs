using System.ComponentModel.DataAnnotations;

namespace uchet.Models
{
    public class Tag
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string Name { get; set; }
        
        [Required]
        public string Width { get; set; }
        
        [Required]
        public string Height { get; set; }
        
        public string Description { get; set; }
        
        public bool IsActive { get; set; } = true;
    }
}