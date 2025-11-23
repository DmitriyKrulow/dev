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
        
        public Role Role { get; set; }
    }
}