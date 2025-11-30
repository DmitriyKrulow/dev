using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using uchet.Services;
using uchet.Data;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using uchet.Models;

namespace uchet.Controllers
{
    [Authorize]
    public class MaintenanceController : Controller
    {
        private readonly IMaintenanceService _maintenanceService;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<MaintenanceController> _logger;

        public MaintenanceController(IMaintenanceService maintenanceService, ApplicationDbContext context, ILogger<MaintenanceController> logger)
        {
            _maintenanceService = maintenanceService;
            _context = context;
            _logger = logger;
        }

        [Authorize(Roles = "Admin,Manager,User")]
        public async Task<IActionResult> Index()
        {
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            _logger.LogInformation($"Пользователь {currentUserId} с ролью {userRole} зашел на страницу заявок");

            List<MaintenanceRequest> requests;

            if (userRole == "Admin")
            {
                // Админ видит все заявки
                requests = await _maintenanceService.GetAllRequestsAsync();
            }
            else if (userRole == "Manager")
            {
                // Менеджер видит заявки, которые ему назначены ИЛИ новые заявки (без исполнителя)
                requests = await _context.MaintenanceRequests
                    .Include(mr => mr.Property)
                    .Include(mr => mr.RequestedBy)
                    .Include(mr => mr.AssignedTo)
                    .Where(mr => mr.AssignedToUserId == currentUserId || mr.AssignedToUserId == null)
                    .OrderByDescending(mr => mr.RequestDate)
                    .ToListAsync();
            }
            else
            {
                // Обычный пользователь видит только свои заявки
                requests = await _maintenanceService.GetRequestsByUserAsync(currentUserId);
            }

            return View(requests);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<JsonResult> AssignRequest([FromBody] AssignRequestModel model)
        {
            try
            {
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
                if (userRole != "Admin")
                {
                    return Json(new { success = false, message = "Недостаточно прав для назначения заявок" });
                }

                _logger.LogInformation($"Попытка назначения заявки {model.RequestId} пользователю {model.AssignedToUserId}");
                
                await _maintenanceService.AssignRequestAsync(model.RequestId, model.AssignedToUserId);
                
                _logger.LogInformation($"Заявка {model.RequestId} успешно назначена пользователю {model.AssignedToUserId}");
                return Json(new { success = true, message = "Заявка назначена" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка назначения заявки {model.RequestId} пользователю {model.AssignedToUserId}");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<JsonResult> CompleteRequest([FromBody] CompleteRequestModel model)
        {
            try
            {
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

                // Проверяем права на завершение заявки
                var request = await _context.MaintenanceRequests.FindAsync(model.RequestId);
                if (request == null)
                {
                    return Json(new { success = false, message = "Заявка не найдена" });
                }

                if (userRole == "Manager" && request.AssignedToUserId != currentUserId)
                {
                    return Json(new { success = false, message = "Вы можете завершать только заявки, назначенные вам" });
                }

                _logger.LogInformation($"Попытка завершения заявки {model.RequestId} пользователем {currentUserId}");
                
                await _maintenanceService.CompleteRequestAsync(model.RequestId, model.ResolutionNotes);
                
                _logger.LogInformation($"Заявка {model.RequestId} успешно завершена");
                return Json(new { success = true, message = "Заявка завершена" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка завершения заявки {model.RequestId}");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetRequestDetails(int requestId)
        {
            try
            {
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

                var request = await _maintenanceService.GetRequestDetailsAsync(requestId);
                if (request == null)
                    return Json(new { success = false, message = "Заявка не найдена" });

                // Проверяем права на просмотр деталей
                if (userRole == "User" && request.RequestedById != currentUserId)
                {
                    return Json(new { success = false, message = "Доступ запрещен" });
                }

                if (userRole == "Manager" && request.AssignedToUserId != currentUserId && request.RequestedById != currentUserId)
                {
                    return Json(new { success = false, message = "Доступ запрещен" });
                }

                return Json(new { 
                    success = true, 
                    request = new {
                        id = request.Id,
                        propertyName = request.Property?.Name,
                        requestedByName = request.RequestedBy?.Name,
                        assignedToName = request.AssignedTo?.Name,
                        description = request.Description,
                        requestDate = request.RequestDate.ToString("dd.MM.yyyy HH:mm"),
                        status = request.Status.ToString(),
                        assignedDate = request.AssignedDate?.ToString("dd.MM.yyyy HH:mm"),
                        completionDate = request.CompletionDate?.ToString("dd.MM.yyyy HH:mm"),
                        resolutionNotes = request.ResolutionNotes
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка получения деталей заявки {requestId}");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<JsonResult> CheckRequestExists(int requestId)
        {
            try
            {
                var request = await _context.MaintenanceRequests
                    .FirstOrDefaultAsync(mr => mr.Id == requestId);
                
                return Json(new { 
                    success = true, 
                    exists = request != null,
                    requestId = requestId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка проверки заявки {requestId}");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<JsonResult> CreateRequest([FromBody] CreateRequestModel model)
        {
            try
            {
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                
                var request = await _maintenanceService.CreateRequestAsync(
                    model.PropertyId, 
                    currentUserId, 
                    model.Description
                );
                
                return Json(new { 
                    success = true, 
                    message = "Заявка на ремонт успешно создана",
                    requestId = request.Id
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка создания заявки");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetAvailableProperties()
        {
            try
            {
                // Получаем ID текущего пользователя
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                
                var properties = await _context.Properties
                    .Where(p => p.AssignedUserId == currentUserId) // Только имущество закрепленное за текущим пользователем
                    .Select(p => new { 
                        p.Id, 
                        p.Name, 
                        p.InventoryNumber,
                        AssignedUser = p.AssignedUser.Name
                    })
                    .ToListAsync();
                
                _logger.LogInformation($"Для пользователя {currentUserId} найдено {properties.Count} закрепленных объектов имущества");
                
                return Json(properties);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка загрузки доступного имущества");
                return Json(new { success = false, message = "Ошибка загрузки имущества" });
            }
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<JsonResult> GetAvailableUsers()
        {
            var users = await _context.Users
                .Where(u => u.IsActive && (u.Role.Name == "Manager" || u.Role.Name == "Admin"))
                .Select(u => new { u.Id, u.Name })
                .ToListAsync();
            
            return Json(users);
        }

        // Новый метод для получения ID текущего пользователя
        [HttpGet]
        public JsonResult GetCurrentUserId()
        {
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            return Json(new { userId = currentUserId });
        }

        // Новый метод для получения роли текущего пользователя
        [HttpGet]
        public JsonResult GetCurrentUserRole()
        {
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            return Json(new { role = userRole });
        }

        // Проверка прав пользователя на просмотр заявки
        private async Task<bool> CanUserViewRequest(int requestId, int currentUserId, string userRole)
        {
            if (userRole == "Admin") return true;
            
            var request = await _context.MaintenanceRequests
                .FirstOrDefaultAsync(mr => mr.Id == requestId);
                
            if (request == null) return false;
            
            if (userRole == "Manager")
            {
                return request.AssignedToUserId == currentUserId || request.RequestedById == currentUserId;
            }
            
            if (userRole == "User")
            {
                return request.RequestedById == currentUserId;
            }
            
            return false;
        }

        // Проверка прав пользователя на редактирование заявки
        private bool CanUserEditRequest(string userRole)
        {
            return userRole == "Admin" || userRole == "Manager";
        }
    }

    public class CreateRequestModel
    {
        public int PropertyId { get; set; }
        public string Description { get; set; }
    }

    public class AssignRequestModel
    {
        public int RequestId { get; set; }
        public int AssignedToUserId { get; set; }
    }

    public class CompleteRequestModel
    {
        public int RequestId { get; set; }
        public string ResolutionNotes { get; set; }
    }
}