using System;
using System.ComponentModel.DataAnnotations;

namespace uchet.Models
{
    public class MaintenanceRequest
    {
        public int Id { get; set; }
        
        [Required]
        public int PropertyId { get; set; }
        public virtual Property Property { get; set; }

        [Required]
        public int RequestedById { get; set; }
        public virtual User RequestedBy { get; set; }

        public int? AssignedToUserId { get; set; }
        public virtual User? AssignedTo { get; set; }

        [Required]
        [StringLength(1000)]
        public string Description { get; set; }

        public DateTime RequestDate { get; set; } = DateTime.UtcNow;

        public MaintenanceStatus Status { get; set; } = MaintenanceStatus.Requested;

        public DateTime? AssignedDate { get; set; }
        public DateTime? CompletionDate { get; set; }
        
        [StringLength(1000)]
        public string? ResolutionNotes { get; set; }
    }

    public enum MaintenanceStatus
    {
        Requested,
        InProgress, 
        Completed,
        Cancelled
    }
}