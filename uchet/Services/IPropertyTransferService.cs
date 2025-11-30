using uchet.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace uchet.Services
{
    public interface IPropertyTransferService
    {
        Task<Property> GetPropertyByInventoryNumberAsync(string inventoryNumber);
        Task<PropertyTransfer> TransferPropertyAsync(int propertyId, int fromUserId, int toUserId, string notes);
        Task<MaintenanceRequest> CreateMaintenanceRequestAsync(int propertyId, int requestedByUserId, string description);
        Task<List<PropertyTransfer>> GetTransferHistoryAsync(int propertyId);
        Task<List<PropertyTransfer>> GetAllTransfersAsync();
    }
}