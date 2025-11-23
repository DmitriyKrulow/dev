using System.ComponentModel.DataAnnotations;

namespace uchet.Models
{
    public class CreatePropertyDto
    {
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
        
        [Required]
        public string InventoryNumber { get; set; }
        
        public DateTime? BalanceDate { get; set; }
        
        public int? UsagePeriod { get; set; }
        
        public decimal? Cost { get; set; }
        
        public DateTime? LastMaintenanceDate { get; set; }
        
        public DateTime? ExpiryDate { get; set; }
    }
}