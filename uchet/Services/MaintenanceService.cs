using uchet.Data;
using uchet.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace uchet.Services
{
    public class MaintenanceService : IMaintenanceService
    {
        private readonly ApplicationDbContext _context;

        public MaintenanceService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<MaintenanceRequest> CreateRequestAsync(int propertyId, int requestedById, string description)
        {
            var property = await _context.Properties.FindAsync(propertyId);
            if (property == null)
                throw new ArgumentException("Имущество не найдено");

            var user = await _context.Users.FindAsync(requestedById);
            if (user == null)
                throw new ArgumentException("Пользователь не найден");

            var request = new MaintenanceRequest
            {
                PropertyId = propertyId,
                RequestedById = requestedById,
                Description = description,
                RequestDate = DateTime.UtcNow,
                Status = MaintenanceStatus.Requested
            };

            _context.MaintenanceRequests.Add(request);
            await _context.SaveChangesAsync();

            await _context.Entry(request)
                .Reference(r => r.Property)
                .LoadAsync();
            await _context.Entry(request)
                .Reference(r => r.RequestedBy)
                .LoadAsync();

            return request;
        }

        public async Task<List<MaintenanceRequest>> GetPendingRequestsAsync()
        {
            return await _context.MaintenanceRequests
                .Include(mr => mr.Property)
                .Include(mr => mr.RequestedBy)
                .Include(mr => mr.AssignedTo)
                .Where(mr => mr.Status == MaintenanceStatus.Requested || mr.Status == MaintenanceStatus.InProgress)
                .OrderByDescending(mr => mr.RequestDate)
                .ToListAsync();
        }

        public async Task<List<MaintenanceRequest>> GetRequestsByUserAsync(int userId)
        {
            return await _context.MaintenanceRequests
                .Include(mr => mr.Property)
                .Include(mr => mr.RequestedBy)
                .Include(mr => mr.AssignedTo)
                .Where(mr => mr.RequestedById == userId)
                .OrderByDescending(mr => mr.RequestDate)
                .ToListAsync();
        }

        public async Task AssignRequestAsync(int requestId, int assignedToUserId)
        {
            var request = await _context.MaintenanceRequests.FindAsync(requestId);
            if (request == null) 
                throw new ArgumentException("Заявка не найдена");

            var assignedUser = await _context.Users.FindAsync(assignedToUserId);
            if (assignedUser == null)
                throw new ArgumentException("Исполнитель не найден");

            request.AssignedToUserId = assignedToUserId;
            request.Status = MaintenanceStatus.InProgress;
            request.AssignedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }

        public async Task CompleteRequestAsync(int requestId, string resolutionNotes)
        {
            var request = await _context.MaintenanceRequests.FindAsync(requestId);
            if (request == null) 
                throw new ArgumentException("Заявка не найдена");

            request.Status = MaintenanceStatus.Completed;
            request.CompletionDate = DateTime.UtcNow;
            request.ResolutionNotes = resolutionNotes;

            await _context.SaveChangesAsync();
        }

        public async Task<List<MaintenanceRequest>> GetAllRequestsAsync()
        {
            return await _context.MaintenanceRequests
                .Include(mr => mr.Property)
                .Include(mr => mr.RequestedBy)
                .Include(mr => mr.AssignedTo)
                .OrderByDescending(mr => mr.RequestDate)
                .ToListAsync();
        }

        public async Task<MaintenanceRequest> GetRequestDetailsAsync(int requestId)
        {
            return await _context.MaintenanceRequests
                .Include(mr => mr.Property)
                .Include(mr => mr.RequestedBy)
                .Include(mr => mr.AssignedTo)
                .FirstOrDefaultAsync(mr => mr.Id == requestId);
        }
    }
}