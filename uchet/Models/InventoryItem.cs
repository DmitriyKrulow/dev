using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace uchet.Models
{
    public class InventoryItem
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public int InventoryId { get; set; }
        
        [Required]
        public int PropertyId { get; set; }
        
        public bool IsChecked { get; set; } = false;
        
        public DateTime? CheckDate { get; set; }
        
        public string? CheckedById { get; set; }
        
        public Inventory Inventory { get; set; }
        
        public Property Property { get; set; }
        
        // public User CheckedBy { get; set; }
    }
}