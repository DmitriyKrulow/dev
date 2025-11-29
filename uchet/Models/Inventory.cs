using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace uchet.Models
{
    public class Inventory
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public string Name { get; set; }
        
        [Required]
        public int LocationId { get; set; }
        
        public DateTime StartDate { get; set; } = DateTime.UtcNow;
        
        public DateTime? EndDate { get; set; }
        
        public bool IsCompleted { get; set; } = false;
        
        public int TotalItems { get; set; }
        
        public int CheckedItems { get; set; }
        
        public Location Location { get; set; }
        
        public ICollection<InventoryItem> InventoryItems { get; set; } = new List<InventoryItem>();
    }
}