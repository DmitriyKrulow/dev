using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace uchet.Models
{
    public class Property
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [MaxLength(200)]
        public string Name { get; set; }
        
        [Required]
        public string Description { get; set; }
        
        [Required]
        public int LocationId { get; set; }
        
        [Required]
        public int PropertyTypeId { get; set; }
        
        public int? AssignedUserId { get; set; }
        
        public string QRCode { get; set; }
        
        public string Barcode { get; set; }
        
        [Required]
        public string InventoryNumber { get; set; }
        
        public DateTime? BalanceDate { get; set; }
        
        public int? UsagePeriod { get; set; }
        
        public decimal? Cost { get; set; }
        
        public DateTime? LastMaintenanceDate { get; set; }
        
        public DateTime? ExpiryDate { get; set; }
        
        public Location Location { get; set; }
        
        public PropertyType PropertyType { get; set; }
        
        public User AssignedUser { get; set; }
        
        public ICollection<PropertyFile> PropertyFiles { get; set; } = new List<PropertyFile>();
        
        public DateTime? LastInventoryCheckDate { get; set; }
        
        public bool IsCheckedInLastInventory { get; set; } = false;

                // Новые навигационные свойства
        public virtual ICollection<PropertyTransfer> TransferHistory { get; set; }
        public virtual ICollection<MaintenanceRequest> MaintenanceHistory { get; set; }
    }
}