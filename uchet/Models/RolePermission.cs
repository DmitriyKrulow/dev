using System.ComponentModel.DataAnnotations;

namespace uchet.Models
{
    public class RolePermission
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public int RoleId { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string ControllerName { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string ActionName { get; set; }
        
        public Role Role { get; set; }
    }
}