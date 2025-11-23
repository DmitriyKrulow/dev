using System.ComponentModel.DataAnnotations;

namespace uchet.Models
{
    public class PropertyFile
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public int PropertyId { get; set; }
        
        [Required]
        [MaxLength(500)]
        public string FilePath { get; set; }
        
        [Required]
        public string FileType { get; set; } // photo or receipt
        
        public Property Property { get; set; }
    }
}