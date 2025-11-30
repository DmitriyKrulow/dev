// Models/PropertyTransfer.cs
using System;
using System.ComponentModel.DataAnnotations;

namespace uchet.Models
{
    public class PropertyTransfer
    {
        public int Id { get; set; }
        
        [Required]
        public int PropertyId { get; set; }
        public virtual Property Property { get; set; }

        [Required]
        public int FromUserId { get; set; }
        public virtual User FromUser { get; set; }

        [Required]
        public int ToUserId { get; set; }
        public virtual User ToUser { get; set; }

        public DateTime TransferDate { get; set; } = DateTime.UtcNow;
        
        [StringLength(500)]
        public string? Notes { get; set; }
    }
}