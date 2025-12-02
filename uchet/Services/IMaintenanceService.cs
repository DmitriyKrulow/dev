using uchet.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace uchet.Services
{
    public interface IMaintenanceService
    {
        Task<List<MaintenanceRequest>> GetPendingRequestsAsync();
        Task<List<MaintenanceRequest>> GetRequestsByUserAsync(int userId);
        Task AssignRequestAsync(int requestId, int assignedToUserId);
        Task CompleteRequestAsync(int requestId, string resolutionNotes);
        Task<List<MaintenanceRequest>> GetAllRequestsAsync();
        Task<MaintenanceRequest> CreateRequestAsync(int propertyId, int requestedById, string description);
        Task<MaintenanceRequest> GetRequestDetailsAsync(int requestId);
    }
}