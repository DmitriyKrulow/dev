using uchet.Data;
using uchet.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace uchet.Services
{
    public class PropertyTransferService : IPropertyTransferService
    {
        private readonly ApplicationDbContext _context;

        public PropertyTransferService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Property> GetPropertyByInventoryNumberAsync(string inventoryNumber)
        {
            return await _context.Properties
                .Include(p => p.AssignedUser)
                .Include(p => p.Location)
                .Include(p => p.PropertyType)
                .FirstOrDefaultAsync(p => p.InventoryNumber == inventoryNumber);
        }

        public async Task<PropertyTransfer> TransferPropertyAsync(int propertyId, int fromUserId, int toUserId, string notes)
        {
            var property = await _context.Properties.FindAsync(propertyId);
            if (property == null) 
                throw new ArgumentException("Имущество не найдено");

            if (property.AssignedUserId != fromUserId)
                throw new InvalidOperationException("Только текущий владелец может передать имущество.");

            var transfer = new PropertyTransfer
            {
                PropertyId = propertyId,
                FromUserId = fromUserId,
                ToUserId = toUserId,
                Notes = notes,
                TransferDate = DateTime.UtcNow
            };

            property.AssignedUserId = toUserId;

            _context.PropertyTransfers.Add(transfer);
            await _context.SaveChangesAsync();

            return transfer;
        }

        public async Task<MaintenanceRequest> CreateMaintenanceRequestAsync(int propertyId, int requestedByUserId, string description)
        {
            var request = new MaintenanceRequest
            {
                PropertyId = propertyId,
                RequestedById = requestedByUserId,
                Description = description,
                RequestDate = DateTime.UtcNow,
                Status = MaintenanceStatus.Requested
            };

            _context.MaintenanceRequests.Add(request);
            await _context.SaveChangesAsync();

            return request;
        }

        public async Task<List<PropertyTransfer>> GetTransferHistoryAsync(int propertyId)
        {
            return await _context.PropertyTransfers
                .Include(pt => pt.FromUser)
                .Include(pt => pt.ToUser)
                .Include(pt => pt.Property)
                .Where(pt => pt.PropertyId == propertyId)
                .OrderByDescending(pt => pt.TransferDate)
                .ToListAsync();
        }

        public async Task<List<PropertyTransfer>> GetAllTransfersAsync()
        {
            return await _context.PropertyTransfers
                .Include(pt => pt.FromUser)
                .Include(pt => pt.ToUser)
                .Include(pt => pt.Property)
                .OrderByDescending(pt => pt.TransferDate)
                .ToListAsync();
        }
    }
}