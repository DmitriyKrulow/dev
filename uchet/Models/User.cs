using System.ComponentModel.DataAnnotations;

namespace uchet.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string Name { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string Email { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string Password { get; set; }
        
        [Required]
        public int RoleId { get; set; }
        
        public bool IsActive { get; set; } = true; // Добавляем поле для блокировки пользователя
        
        public int? LocationId { get; set; } // Добавляем привязку к локации
        
        public Role Role { get; set; }
        
        public Location Location { get; set; }

                // Новые навигационные свойства
        public virtual ICollection<PropertyTransfer> TransfersAsSender { get; set; }
        public virtual ICollection<PropertyTransfer> TransfersAsReceiver { get; set; }
        public virtual ICollection<MaintenanceRequest> MaintenanceRequests { get; set; }
        public virtual ICollection<Property> Properties { get; set; }
    }
}