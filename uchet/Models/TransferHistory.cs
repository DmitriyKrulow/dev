using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace uchet.Models
{
    public class TransferHistory
    {
        public int Id { get; set; }
        
        [Required]
        public int PropertyId { get; set; }
        
        [ForeignKey("PropertyId")]
        public Property Property { get; set; }
        
        public int? FromUserId { get; set; }
        
        [ForeignKey("FromUserId")]
        public User FromUser { get; set; }
        
        public int? ToUserId { get; set; }
        
        [ForeignKey("ToUserId")]
        public User ToUser { get; set; }
        
        [Required]
        public DateTime TransferDate { get; set; }
        
        [StringLength(500)]
        public string Notes { get; set; }
    }
}