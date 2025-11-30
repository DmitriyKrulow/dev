// Controllers/ScanController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using uchet.Models;
using System.Linq;
using System;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using uchet.Data;

namespace uchet.Controllers
{
    [Authorize]
    public class ScanController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ScanController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Метод для получения ID текущего пользователя
        private async Task<User> GetCurrentUserAsync()
        {
            var userName = User.Identity.Name;
            if (string.IsNullOrEmpty(userName))
            {
                throw new UnauthorizedAccessException("Пользователь не аутентифицирован");
            }

            var user = await _context.Users
                .Include(u => u.Location)
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Name == userName);

            if (user == null)
            {
                throw new UnauthorizedAccessException("Пользователь не найден в базе данных");
            }

            return user;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<JsonResult> GetCurrentUser()
        {
            try
            {
                var user = await GetCurrentUserAsync();
                
                // ТОЛЬКО АДМИНЫ могут принимать на склад
                var isAdmin = user.RoleId == 1; // Admin role

                return Json(new 
                {
                    id = user.Id,
                    name = user.Name,
                    location = user.Location?.Name ?? "Не указана",
                    locationId = user.LocationId, // Добавляем ID локации
                    isAdmin = isAdmin,
                    role = user.Role?.Name ?? "User"
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка получения текущего пользователя: {ex.Message} - ScanController.cs:74");
                return Json(new { error = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> ScanProperty([FromBody] ScanRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.InventoryNumber))
                {
                    return Json(new { success = false, message = "Инвентарный номер не указан" });
                }

                Console.WriteLine($"Поиск имущества с номером: {request.InventoryNumber} - ScanController.cs:90");

                var property = await _context.Properties
                    .Include(p => p.AssignedUser)
                    .Include(p => p.Location)
                    .Include(p => p.PropertyType)
                    .FirstOrDefaultAsync(p => p.InventoryNumber == request.InventoryNumber.Trim());

                if (property == null)
                {
                    Console.WriteLine("Имущество не найдено в базе данных - ScanController.cs:100");
                    return Json(new { success = false, message = "Имущество не найдено" });
                }

                Console.WriteLine($"Найдено имущество: {property.Name} - ScanController.cs:104");

                // Получаем текущего пользователя для определения прав
                var currentUser = await GetCurrentUserAsync();
                var isAdmin = currentUser.RoleId == 1; // Только админы

                var result = new
                {
                    success = true,
                    property = new
                    {
                        id = property.Id,
                        name = property.Name,
                        inventoryNumber = property.InventoryNumber,
                        propertyType = property.PropertyType?.Name,
                        location = property.Location?.Name,
                        locationId = property.LocationId, // Добавляем ID локации имущества
                        currentUser = property.AssignedUser?.Name,
                        assignedUserId = property.AssignedUserId,
                        status = "Доступно",
                        cost = property.Cost,
                        description = property.Description
                    },
                    currentUser = new
                    {
                        id = currentUser.Id,
                        locationId = currentUser.LocationId, // Добавляем ID локации пользователя
                        isAdmin = isAdmin
                    }
                };

                return Json(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при поиске: {ex.Message} - ScanController.cs:139");
                return Json(new { success = false, message = $"Ошибка при поиске: {ex.Message}" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> TransferToUser([FromBody] TransferRequest request)
        {
            try
            {
                if (request.PropertyId == 0)
                {
                    return Json(new { success = false, message = "ID имущества не указан" });
                }

                var currentUser = await GetCurrentUserAsync();
                Console.WriteLine($"Пользователь {currentUser.Name} (ID: {currentUser.Id}) принимает имущество {request.PropertyId} - ScanController.cs:156");

                var property = await _context.Properties
                    .Include(p => p.AssignedUser)
                    .Include(p => p.Location)
                    .FirstOrDefaultAsync(p => p.Id == request.PropertyId);

                if (property == null)
                {
                    return Json(new { success = false, message = "Имущество не найдено" });
                }

                // Если имущество уже у текущего пользователя
                if (property.AssignedUserId == currentUser.Id)
                {
                    return Json(new { success = false, message = "Имущество уже закреплено за вами" });
                }

                // Сохраняем старую информацию для истории
                var oldAssignedUserId = property.AssignedUserId;
                var oldLocationId = property.LocationId;

                // Создаем запись в истории передач
                var transfer = new PropertyTransfer
                {
                    PropertyId = property.Id,
                    FromUserId = property.AssignedUserId ?? currentUser.Id,
                    ToUserId = currentUser.Id,
                    TransferDate = DateTime.UtcNow,
                    Notes = $"Передача через сканирование. От: {(property.AssignedUser?.Name ?? "Склад")}, К: {currentUser.Name}. " +
                           $"Локация изменена с {(property.Location?.Name ?? "Неизвестно")} на {currentUser.Location?.Name ?? "Не указана"}"
                };

                _context.PropertyTransfers.Add(transfer);

                // ОБНОВЛЯЕМ НАЗНАЧЕННОГО ПОЛЬЗОВАТЕЛЯ И ЛОКАЦИЮ
                property.AssignedUserId = currentUser.Id;
                property.LocationId = currentUser.LocationId ?? property.LocationId; // Обновляем локацию на локацию пользователя

                _context.Properties.Update(property);

                await _context.SaveChangesAsync();

                var message = oldAssignedUserId.HasValue ? 
                    $"Имущество '{property.Name}' успешно передано вам от пользователя {property.AssignedUser?.Name}. Локация обновлена." : 
                    $"Имущество '{property.Name}' успешно выдано вам со склада. Локация обновлена.";

                return Json(new { success = true, message = message });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при передаче имущества: {ex.Message} - ScanController.cs:207");
                return Json(new { success = false, message = $"Ошибка при передаче имущества: {ex.Message}" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> ReturnToWarehouse([FromBody] TransferRequest request)
        {
            try
            {
                if (request.PropertyId == 0)
                {
                    return Json(new { success = false, message = "ID имущества не указан" });
                }

                var currentUser = await GetCurrentUserAsync();
                
                // ПРОВЕРЯЕМ, ЯВЛЯЕТСЯ ЛИ ПОЛЬЗОВАТЕЛЬ АДМИНОМ
                var isAdmin = currentUser.RoleId == 1;

                if (!isAdmin)
                {
                    return Json(new { success = false, message = "Только администраторы могут принимать имущество на склад" });
                }

                Console.WriteLine($"Администратор {currentUser.Name} (ID: {currentUser.Id}) принимает имущество {request.PropertyId} на склад - ScanController.cs:233");

                var property = await _context.Properties
                    .Include(p => p.AssignedUser)
                    .Include(p => p.Location)
                    .FirstOrDefaultAsync(p => p.Id == request.PropertyId);

                if (property == null)
                {
                    return Json(new { success = false, message = "Имущество не найдено" });
                }

                // Сохраняем старую информацию для истории
                var oldAssignedUserId = property.AssignedUserId;
                var oldLocationId = property.LocationId;

                // Создаем запись в истории передач (возврат на склад)
                var transfer = new PropertyTransfer
                {
                    PropertyId = property.Id,
                    FromUserId = property.AssignedUserId ?? currentUser.Id,
                    ToUserId = currentUser.Id,
                    TransferDate = DateTime.UtcNow,
                    Notes = $"Возврат на склад через сканирование. От: {(property.AssignedUser?.Name ?? "Неизвестно")}, Принял: {currentUser.Name}. " +
                           $"Локация изменена с {(property.Location?.Name ?? "Неизвестно")} на {currentUser.Location?.Name ?? "Склад"}"
                };

                _context.PropertyTransfers.Add(transfer);

                // ОБНОВЛЯЕМ НАЗНАЧЕННОГО ПОЛЬЗОВАТЕЛЯ И ЛОКАЦИЮ (склад админа)
                property.AssignedUserId = currentUser.Id;
                property.LocationId = currentUser.LocationId ?? property.LocationId; // Обновляем локацию на локацию админа (склад)

                _context.Properties.Update(property);

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = $"Имущество '{property.Name}' успешно принято на склад. Локация обновлена." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при приеме имущества на склад: {ex.Message} - ScanController.cs:274");
                return Json(new { success = false, message = $"Ошибка при приеме имущества на склад: {ex.Message}" });
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetUsers()
        {
            try
            {
                var users = await _context.Users
                    .Where(u => u.IsActive)
                    .Select(u => new { id = u.Id, name = u.Name })
                    .ToListAsync();

                return Json(users);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки пользователей: {ex.Message} - ScanController.cs:293");
                return Json(new List<object>());
            }
        }
    }

    // Модели запросов для ScanController
    public class ScanRequest
    {
        public string InventoryNumber { get; set; }
    }

    public class TransferRequest
    {
        public int PropertyId { get; set; }
    }
}