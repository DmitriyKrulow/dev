using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using uchet.Data;
using uchet.Models;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace uchet.Controllers
{
    [Authorize(Roles = "Admin,Manager,User")]
    public class InventoryController : Controller
    {
        private readonly ApplicationDbContext _context;

        public InventoryController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Список всех инвентаризаций
        public async Task<IActionResult> Index()
        {
            var inventories = await _context.Inventories
                .Include(i => i.Location)
                .OrderByDescending(i => i.StartDate)
                .ToListAsync();
            
            return View(inventories);
        }

        // Детали инвентаризации
        public async Task<IActionResult> Details(int id)
        {
            var inventory = await _context.Inventories
                .Include(i => i.Location)
                .Include(i => i.InventoryItems)
                .ThenInclude(ii => ii.Property)
                .ThenInclude(p => p.PropertyType)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (inventory == null)
            {
                return NotFound();
            }

            return View(inventory);
        }

        // Создание новой инвентаризации - доступно Admin и Manager
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Create()
        {
            var locations = await _context.Locations.ToListAsync();
            var users = await _context.Users
                .Where(u => u.IsActive)
                .OrderBy(u => u.Name)
                .ToListAsync();
            
            ViewBag.Locations = new SelectList(locations, "Id", "Name");
            ViewBag.Users = new SelectList(users, "Id", "Name");
            return View();
        }
        // Создание инвентаризации по переданному имуществу пользователя - доступно Admin и Manager
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> CreateByUser()
        {
            var users = await _context.Users
                .Where(u => u.IsActive) // Только активные пользователи
                .OrderBy(u => u.Name)
                .ToListAsync();
            
            ViewBag.Users = new SelectList(users, "Id", "Name");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> CreateByUser(string name, int userId)
        {
            Console.WriteLine($"CreateByUser: Starting with name={name}, userId={userId} - InventoryController.cs:84");
            
            if (string.IsNullOrWhiteSpace(name))
            {
                Console.WriteLine("CreateByUser: Name is empty or whitespace - InventoryController.cs:88");
                ModelState.AddModelError("", "Название инвентаризации обязательно");
                var users = await _context.Users
                    .Where(u => u.IsActive)
                    .OrderBy(u => u.Name)
                    .ToListAsync();
                ViewBag.Users = new SelectList(users, "Id", "Name");
                return View();
            }

            // Получаем имущество, переданное пользователю
            var userProperties = await _context.Properties
                .Include(p => p.PropertyType)
                .Where(p => p.AssignedUserId == userId)
                .ToListAsync();

            Console.WriteLine($"CreateByUser: Found {userProperties.Count} properties for userId={userId} - InventoryController.cs:104");

            if (!userProperties.Any())
            {
                Console.WriteLine("CreateByUser: No properties found for user - InventoryController.cs:108");
                ModelState.AddModelError("", "У выбранного пользователя нет переданного имущества");
                var users = await _context.Users
                    .Where(u => u.IsActive)
                    .OrderBy(u => u.Name)
                    .ToListAsync();
                ViewBag.Users = new SelectList(users, "Id", "Name");
                return View();
            }

            // Сбрасываем статус проверки для имущества пользователя
            foreach (var property in userProperties)
            {
                property.IsCheckedInLastInventory = false;
                property.LastInventoryCheckDate = null;
            }

            // Получаем пользователя с локацией
            var userWithLocation = await _context.Users
                .Include(u => u.Location)
                .FirstOrDefaultAsync(u => u.Id == userId);
            
            Console.WriteLine($"CreateByUser: userWithLocation={userWithLocation?.Name}, locationId={userWithLocation?.LocationId} - InventoryController.cs:130");
            
            // Проверяем, что у пользователя есть локация
            if (userWithLocation?.LocationId == null)
            {
                Console.WriteLine("CreateByUser: User has no location assigned - InventoryController.cs:135");
                ModelState.AddModelError("", "У пользователя не указана локация");
                var users = await _context.Users
                    .Where(u => u.IsActive)
                    .OrderBy(u => u.Name)
                    .ToListAsync();
                ViewBag.Users = new SelectList(users, "Id", "Name");
                return View();
            }
            
            Console.WriteLine($"CreateByUser: User locationId={userWithLocation.LocationId} - InventoryController.cs:145");
            
            // Создаем инвентаризацию с привязкой к локации пользователя
            var inventory = new Inventory
            {
                Name = name,
                LocationId = userWithLocation.LocationId.Value, // Преобразуем int? в int через Value
                TotalItems = userProperties.Count,
                CheckedItems = 0,
                StartDate = DateTime.UtcNow
            };
            
            Console.WriteLine($"CreateByUser: Creating inventory with name={name}, locationId={inventory.LocationId}, totalItems={inventory.TotalItems} - InventoryController.cs:157");

            _context.Inventories.Add(inventory);
            
            try
            {
                await _context.SaveChangesAsync();
                Console.WriteLine($"CreateByUser: Inventory created with id={inventory.Id} - InventoryController.cs:164");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"CreateByUser: Exception occurred while saving inventory: {ex.Message} - InventoryController.cs:168");
                ModelState.AddModelError("", "Ошибка при создании инвентаризации");
                var users = await _context.Users
                    .Where(u => u.IsActive)
                    .OrderBy(u => u.Name)
                    .ToListAsync();
                ViewBag.Users = new SelectList(users, "Id", "Name");
                return View();
            }
            
            // Создаем записи InventoryItem для каждого имущества пользователя
            foreach (var property in userProperties)
            {
                var inventoryItem = new InventoryItem
                {
                    InventoryId = inventory.Id,
                    PropertyId = property.Id,
                    IsChecked = false
                };
                _context.InventoryItems.Add(inventoryItem);
            }
            
            try
            {
                await _context.SaveChangesAsync();
                Console.WriteLine($"CreateByUser: Saved {userProperties.Count} inventory items for inventoryId={inventory.Id} - InventoryController.cs:193");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"CreateByUser: Exception occurred while saving inventory items: {ex.Message} - InventoryController.cs:197");
                // Удаляем инвентаризацию, если не удалось создать элементы
                _context.Inventories.Remove(inventory);
                await _context.SaveChangesAsync();
                ModelState.AddModelError("", "Ошибка при создании элементов инвентаризации");
                var users = await _context.Users
                    .Where(u => u.IsActive)
                    .OrderBy(u => u.Name)
                    .ToListAsync();
                ViewBag.Users = new SelectList(users, "Id", "Name");
                return View();
            }

            Console.WriteLine($"CreateByUser: Redirecting to Details page for inventoryId={inventory.Id} - InventoryController.cs:210");
            return RedirectToAction("Details", new { id = inventory.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Create(string name, int locationId)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                ModelState.AddModelError("", "Название инвентаризации обязательно");
                var locations = await _context.Locations.ToListAsync();
                ViewBag.Locations = new SelectList(locations, "Id", "Name");
                return View();
            }

            // Сбрасываем статус проверки для всей локации
            await ResetInventoryStatusForLocation(locationId);

            // Подсчитываем общее количество имущества в выбранной локации
            var totalItems = await _context.Properties.CountAsync(p => p.LocationId == locationId);

            var inventory = new Inventory
            {
                Name = name,
                LocationId = locationId,
                TotalItems = totalItems,
                CheckedItems = 0,
                StartDate = DateTime.UtcNow
            };

            _context.Inventories.Add(inventory);
            
            try
            {
                await _context.SaveChangesAsync();
                Console.WriteLine($"Create: inventory created with id={inventory.Id} - InventoryController.cs:247");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Create: exception occurred while saving inventory: {ex.Message} - InventoryController.cs:251");
                ModelState.AddModelError("", "Ошибка при создании инвентаризации");
                var locations = await _context.Locations.ToListAsync();
                ViewBag.Locations = new SelectList(locations, "Id", "Name");
                return View();
            }
            
            // Создаем записи InventoryItem для каждого имущества в локации
            var properties = await _context.Properties
                .Where(p => p.LocationId == locationId)
                .ToListAsync();
                
            Console.WriteLine($"Create: found {properties.Count} properties for locationId={locationId} - InventoryController.cs:263");

            foreach (var property in properties)
            {
                var inventoryItem = new InventoryItem
                {
                    InventoryId = inventory.Id,
                    PropertyId = property.Id,
                    IsChecked = false
                };
                _context.InventoryItems.Add(inventoryItem);
            }
            
            try
            {
                await _context.SaveChangesAsync();
                Console.WriteLine($"Create: saved {properties.Count} inventory items for inventoryId={inventory.Id} - InventoryController.cs:279");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Create: exception occurred while saving inventory items: {ex.Message} - InventoryController.cs:283");
                // Удаляем инвентаризацию, если не удалось создать элементы
                _context.Inventories.Remove(inventory);
                await _context.SaveChangesAsync();
                ModelState.AddModelError("", "Ошибка при создании элементов инвентаризации");
                var locations = await _context.Locations.ToListAsync();
                ViewBag.Locations = new SelectList(locations, "Id", "Name");
                return View();
            }

            return RedirectToAction("Details", new { id = inventory.Id });
        }

        // Сброс статуса проверки имущества при создании новой инвентаризации в той же локации
        private async Task ResetInventoryStatusForLocation(int locationId)
        {
            var properties = await _context.Properties
                .Where(p => p.LocationId == locationId)
                .ToListAsync();

            foreach (var property in properties)
            {
                property.IsCheckedInLastInventory = false;
                property.LastInventoryCheckDate = null;
            }

            await _context.SaveChangesAsync();
        }

        // Страница сканирования для инвентаризации - доступно всем авторизованным
        [Authorize]
        public async Task<IActionResult> Scan(int id)
        {
            var inventory = await _context.Inventories
                .Include(i => i.Location)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (inventory == null)
            {
                return NotFound();
            }

            // Проверяем, завершена ли инвентаризация
            if (inventory.IsCompleted)
            {
                TempData["Message"] = "Инвентаризация уже завершена";
                return RedirectToAction("Details", new { id = id });
            }

            return View(inventory);
        }

        // Получение списка имущества для инвентаризации
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetLocationProperties(int inventoryId)
        {
            try
            {
                var inventory = await _context.Inventories
                    .Include(i => i.Location)
                    .FirstOrDefaultAsync(i => i.Id == inventoryId);

                if (inventory == null)
                {
                    return Json(new { success = false, message = "Инвентаризация не найдена" });
                }

                // Получаем все имущество в локации
                var allLocationProperties = await _context.Properties
                    .Include(p => p.PropertyType)
                    .Where(p => p.LocationId == inventory.LocationId)
                    .ToListAsync();

                // Получаем записи InventoryItem для этой инвентаризации
                var inventoryItems = await _context.InventoryItems
                    .Where(ii => ii.InventoryId == inventoryId)
                    .ToListAsync();

                // Создаем словарь для быстрого поиска статуса проверки
                var inventoryItemDict = inventoryItems.ToDictionary(ii => ii.PropertyId, ii => ii);

                var properties = allLocationProperties
                    .Select(p => new
                    {
                        id = p.Id,
                        name = p.Name ?? "Не указано",
                        propertyTypeName = p.PropertyType?.Name ?? "Не указан",
                        inventoryNumber = p.InventoryNumber ?? "Без номера",
                        qrCode = p.QRCode ?? "",
                        barcode = p.Barcode ?? "",
                        isChecked = inventoryItemDict.ContainsKey(p.Id) ? inventoryItemDict[p.Id].IsChecked : false,
                        checkDate = inventoryItemDict.ContainsKey(p.Id) ? inventoryItemDict[p.Id].CheckDate : null
                    })
                    .OrderBy(p => p.name)
                    .ToList();

                Console.WriteLine($"GetLocationProperties: возвращено {properties.Count} свойств - InventoryController.cs:380");

                return Json(new { success = true, properties });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetLocationProperties ошибка: {ex.Message} - InventoryController.cs:386");
                return Json(new { success = false, message = $"Ошибка: {ex.Message}" });
            }
        }

        // Проверка имущества по QR коду или инвентарному номеру
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> CheckItem(int inventoryId, string code)
        {
            // Добавляем логирование для диагностики
            Console.WriteLine($"CheckItem: inventoryId={inventoryId}, code={code} - InventoryController.cs:397");
             
            var inventory = await _context.Inventories
                .Include(i => i.InventoryItems)
                .ThenInclude(ii => ii.Property)
                .FirstOrDefaultAsync(i => i.Id == inventoryId);
             
            Console.WriteLine($"CheckItem: inventory found={inventory != null} - InventoryController.cs:404");
             
            if (inventory != null)
            {
                Console.WriteLine($"CheckItem: inventory completed={inventory.IsCompleted} - InventoryController.cs:408");
                Console.WriteLine($"CheckItem: inventory locationId={inventory.LocationId} - InventoryController.cs:409");
            }
 
            if (inventory == null)
            {
                return Json(new { success = false, message = "Инвентаризация не найдена" });
            }
 
            if (inventory.IsCompleted)
            {
                return Json(new { success = false, message = "Инвентаризация уже завершена" });
            }
            
            // Добавляем логирование для диагностики поиска имущества
            Console.WriteLine($"CheckItem: searching property with QRCode={code} or InventoryNumber={code} - InventoryController.cs:423");
             
            // Ищем имущество по QR коду или инвентарному номеру
            var property = await _context.Properties
                .FirstOrDefaultAsync(p => p.QRCode == code || p.InventoryNumber == code);
                 
            Console.WriteLine($"CheckItem: property found={property != null} - InventoryController.cs:429");
             
            if (property != null)
            {
                Console.WriteLine($"CheckItem: property locationId={property.LocationId}, property id={property.Id} - InventoryController.cs:433");
            }
 
            if (property == null)
            {
                return Json(new { success = false, message = "Имущество не найдено" });
            }
 
            // Проверяем, принадлежит ли имущество к локации инвентаризации
            if (property.LocationId != inventory.LocationId)
            {
                return Json(new { success = false, message = "Имущество не принадлежит данной локации" });
            }
 
            // Ищем запись InventoryItem для этого имущества
            var inventoryItem = inventory.InventoryItems
                .FirstOrDefault(ii => ii.PropertyId == property.Id);
 
            if (inventoryItem == null)
            {
                return Json(new { success = false, message = "Имущество не включено в инвентаризацию" });
            }
 
            if (inventoryItem.IsChecked)
            {
                return Json(new { success = false, message = "Имущество уже проверено" });
            }
 
            // Обновляем запись InventoryItem
            inventoryItem.IsChecked = true;
            inventoryItem.CheckDate = DateTime.UtcNow;
            inventoryItem.CheckedById = User.Identity.Name;
 
            // Увеличиваем счетчик проверенных предметов
            inventory.CheckedItems++;
 
            // Проверяем, все ли имущество проверено
            if (inventory.CheckedItems >= inventory.TotalItems)
            {
                inventory.IsCompleted = true;
                inventory.EndDate = DateTime.UtcNow;
            }
 
            await _context.SaveChangesAsync();
 
            // Обновляем статус проверки в самом имуществе
            property.LastInventoryCheckDate = DateTime.UtcNow;
            property.IsCheckedInLastInventory = true;
            await _context.SaveChangesAsync();
 
            return Json(new {
                success = true,
                message = "Имущество проверено",
                checkedItems = inventory.CheckedItems,
                totalItems = inventory.TotalItems,
                isCompleted = inventory.IsCompleted
            });
        }

        // Завершение инвентаризации вручную - доступно Admin и Manager
        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Complete(int id)
        {
            var inventory = await _context.Inventories.FindAsync(id);
             
            if (inventory == null)
            {
                return NotFound();
            }
 
            if (inventory.IsCompleted)
            {
                TempData["Message"] = "Инвентаризация уже завершена";
                return RedirectToAction("Details", new { id = id });
            }
 
            inventory.IsCompleted = true;
            inventory.EndDate = DateTime.UtcNow;
 
            // Обновляем статус для непроверенного имущества
            var uncheckedItems = await _context.InventoryItems
                .Include(ii => ii.Property)
                .Where(ii => ii.InventoryId == id && !ii.IsChecked)
                .ToListAsync();
 
            foreach (var item in uncheckedItems)
            {
                item.Property.IsCheckedInLastInventory = false;
            }
 
            await _context.SaveChangesAsync();
 
            TempData["Message"] = "Инвентаризация завершена";
            return RedirectToAction("Details", new { id = id });
        }
        
        // Удаление завершенной инвентаризации - доступно только Admin
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var inventory = await _context.Inventories
                .Include(i => i.InventoryItems)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (inventory == null)
            {
                return NotFound();
            }

            try
            {
                // Удаляем связанные записи InventoryItems
                _context.InventoryItems.RemoveRange(inventory.InventoryItems);
                
                // Удаляем саму инвентаризацию
                _context.Inventories.Remove(inventory);
                
                await _context.SaveChangesAsync();
                
                TempData["Message"] = "Инвентаризация успешно удалена";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Ошибка при удалении инвентаризации: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        // Удаление всех завершенных инвентаризаций - доступно только Admin
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteCompleted()
        {
            try
            {
                // Находим все завершенные инвентаризации
                var completedInventories = await _context.Inventories
                    .Include(i => i.InventoryItems)
                    .Where(i => i.IsCompleted)
                    .ToListAsync();

                if (!completedInventories.Any())
                {
                    TempData["Warning"] = "Нет завершенных инвентаризаций для удаления";
                    return RedirectToAction(nameof(Index));
                }

                int deletedCount = 0;

                foreach (var inventory in completedInventories)
                {
                    // Удаляем связанные записи InventoryItems
                    _context.InventoryItems.RemoveRange(inventory.InventoryItems);
                    
                    // Удаляем саму инвентаризацию
                    _context.Inventories.Remove(inventory);
                    
                    deletedCount++;
                }

                await _context.SaveChangesAsync();

                TempData["Success"] = $"Успешно удалено {deletedCount} завершенных инвентаризаций";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при удалении завершенных инвентаризаций: {ex.Message} - InventoryController.cs:604");
                TempData["Error"] = $"Ошибка при удалении завершенных инвентаризаций: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        // Получение даты последней инвентаризации для имущества
        [HttpGet]
        public async Task<IActionResult> GetLastInventoryDate(int propertyId)
        {
            var lastInventoryItem = await _context.InventoryItems
                .Include(ii => ii.Inventory)
                .Where(ii => ii.PropertyId == propertyId && ii.IsChecked)
                .OrderByDescending(ii => ii.CheckDate)
                .FirstOrDefaultAsync();

            if (lastInventoryItem != null)
            {
                return Json(new { 
                    success = true, 
                    lastInventoryDate = lastInventoryItem.CheckDate?.ToString("dd.MM.yyyy HH:mm"),
                    inventoryName = lastInventoryItem.Inventory.Name
                });
            }

            return Json(new { success = false, message = "Инвентаризации не проводились" });
        }
    }
}