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
    [Authorize(Roles = "Admin,Manager")]
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

        // Создание новой инвентаризации
        public async Task<IActionResult> Create()
        {
            var locations = await _context.Locations.ToListAsync();
            ViewBag.Locations = new SelectList(locations, "Id", "Name");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string name, int locationId)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                ModelState.AddModelError("", "Название инвентаризации обязательно");
                var locations = await _context.Locations.ToListAsync();
                ViewBag.Locations = new SelectList(locations, "Id", "Name");
                return View();
            }

            // Подсчитываем общее количество имущества в выбранной локации
            var totalItems = await _context.Properties.CountAsync(p => p.LocationId == locationId);

            var inventory = new Inventory
            {
                Name = name,
                LocationId = locationId,
                TotalItems = totalItems,
                CheckedItems = 0
            };

            _context.Inventories.Add(inventory);
            
            try
            {
                await _context.SaveChangesAsync();
                Console.WriteLine($"Create: inventory created with id={inventory.Id} - InventoryController.cs:88");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Create: exception occurred while saving inventory: {ex.Message} - InventoryController.cs:92");
                Console.WriteLine($"Create: exception stack trace: {ex.StackTrace} - InventoryController.cs:93");
                ModelState.AddModelError("", "Ошибка при создании инвентаризации");
                var locations = await _context.Locations.ToListAsync();
                ViewBag.Locations = new SelectList(locations, "Id", "Name");
                return View();
            }
            
            // Создаем записи InventoryItem для каждого имущества в локации
            var properties = await _context.Properties
                .Where(p => p.LocationId == locationId)
                .ToListAsync();
                
            Console.WriteLine($"Create: found {properties.Count} properties for locationId={locationId} - InventoryController.cs:105");

            foreach (var property in properties)
            {
                var inventoryItem = new InventoryItem
                {
                    InventoryId = inventory.Id,
                    PropertyId = property.Id,
                    IsChecked = false
                };
                _context.InventoryItems.Add(inventoryItem);
                Console.WriteLine($"Create: added inventoryItem for propertyId={property.Id}, inventoryId={inventory.Id} - InventoryController.cs:116");
            }
            
            try
            {
                var changes = await _context.SaveChangesAsync();
                Console.WriteLine($"Create: saved {properties.Count} inventory items for inventoryId={inventory.Id}, changes={changes} - InventoryController.cs:122");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Create: exception occurred while saving inventory items: {ex.Message} - InventoryController.cs:126");
                Console.WriteLine($"Create: exception stack trace: {ex.StackTrace} - InventoryController.cs:127");
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

        // Тестовый метод для создания записей InventoryItem
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> TestCreateInventoryItems(int inventoryId)
        {
            try
            {
                Console.WriteLine($"TestCreateInventoryItems: called with inventoryId={inventoryId} - InventoryController.cs:147");
                
                var inventory = await _context.Inventories.FindAsync(inventoryId);
                Console.WriteLine($"TestCreateInventoryItems: inventory found={inventory != null} - InventoryController.cs:150");
                
                if (inventory == null)
                {
                    return Json(new { success = false, message = "Инвентаризация не найдена" });
                }
                
                // Получаем все имущество в локации
                var properties = await _context.Properties
                    .Where(p => p.LocationId == inventory.LocationId)
                    .ToListAsync();
                    
                Console.WriteLine($"TestCreateInventoryItems: found {properties.Count} properties for locationId={inventory.LocationId} - InventoryController.cs:162");
                    
                // Создаем записи InventoryItem для каждого имущества в локации
                foreach (var property in properties)
                {
                    var inventoryItem = new InventoryItem
                    {
                        InventoryId = inventory.Id,
                        PropertyId = property.Id,
                        IsChecked = false
                    };
                    _context.InventoryItems.Add(inventoryItem);
                    Console.WriteLine($"TestCreateInventoryItems: added inventoryItem for propertyId={property.Id}, inventoryId={inventory.Id} - InventoryController.cs:174");
                }
                
                Console.WriteLine($"TestCreateInventoryItems: about to save changes - InventoryController.cs:177");
                await _context.SaveChangesAsync();
                Console.WriteLine($"TestCreateInventoryItems: changes saved successfully - InventoryController.cs:179");
                
                return Json(new { success = true, message = $"Создано {properties.Count} записей InventoryItem" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"TestCreateInventoryItems: exception occurred: {ex.Message} - InventoryController.cs:185");
                Console.WriteLine($"TestCreateInventoryItems: exception stack trace: {ex.StackTrace} - InventoryController.cs:186");
                return Json(new { success = false, message = $"Ошибка: {ex.Message}" });
            }
        }

        // Тестовый метод для создания записей InventoryItem напрямую через SQL
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> TestCreateInventoryItemsDirect(int inventoryId)
        {
            try
            {
                Console.WriteLine($"TestCreateInventoryItemsDirect: called with inventoryId={inventoryId} - InventoryController.cs:198");
                
                var inventory = await _context.Inventories.FindAsync(inventoryId);
                Console.WriteLine($"TestCreateInventoryItemsDirect: inventory found={inventory != null} - InventoryController.cs:201");
                
                if (inventory == null)
                {
                    return Json(new { success = false, message = "Инвентаризация не найдена" });
                }
                
                // Получаем все имущество в локации
                var properties = await _context.Properties
                    .Where(p => p.LocationId == inventory.LocationId)
                    .ToListAsync();
                    
                Console.WriteLine($"TestCreateInventoryItemsDirect: found {properties.Count} properties for locationId={inventory.LocationId} - InventoryController.cs:213");
                
                if (properties.Count == 0)
                {
                    return Json(new { success = true, message = "Нет имущества в локации" });
                }
                
                // Создаем SQL команду для вставки записей InventoryItem
                var sqlCommands = new List<string>();
                foreach (var property in properties)
                {
                    sqlCommands.Add($"INSERT INTO \"InventoryItems\" (\"InventoryId\", \"PropertyId\", \"IsChecked\") VALUES ({inventory.Id}, {property.Id}, false);");
                }
                
                // Выполняем SQL команды
                foreach (var sql in sqlCommands)
                {
                    Console.WriteLine($"TestCreateInventoryItemsDirect: executing SQL: {sql} - InventoryController.cs:230");
                    await _context.Database.ExecuteSqlRawAsync(sql);
                }
                
                Console.WriteLine($"TestCreateInventoryItemsDirect: successfully created {properties.Count} inventory items - InventoryController.cs:234");
                
                return Json(new { success = true, message = $"Создано {properties.Count} записей InventoryItem" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"TestCreateInventoryItemsDirect: exception occurred: {ex.Message} - InventoryController.cs:240");
                Console.WriteLine($"TestCreateInventoryItemsDirect: exception stack trace: {ex.StackTrace} - InventoryController.cs:241");
                return Json(new { success = false, message = $"Ошибка: {ex.Message}" });
            }
        }
        
        // Тестовый метод для проверки состояния базы данных
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> TestDatabaseState()
        {
            try
            {
                Console.WriteLine($"TestDatabaseState: called - InventoryController.cs:253");
                
                // Получаем количество записей в каждой таблице
                var inventoriesCount = await _context.Inventories.CountAsync();
                var inventoryItemsCount = await _context.InventoryItems.CountAsync();
                var propertiesCount = await _context.Properties.CountAsync();
                var locationsCount = await _context.Locations.CountAsync();
                
                Console.WriteLine($"TestDatabaseState: inventories={inventoriesCount}, inventoryItems={inventoryItemsCount}, properties={propertiesCount}, locations={locationsCount} - InventoryController.cs:261");
                
                // Получаем все записи из таблицы InventoryItems
                var inventoryItems = await _context.InventoryItems.ToListAsync();
                Console.WriteLine($"TestDatabaseState: inventoryItems count={inventoryItems.Count} - InventoryController.cs:265");
                
                foreach (var item in inventoryItems)
                {
                    Console.WriteLine($"TestDatabaseState: inventoryItem id={item.Id}, inventoryId={item.InventoryId}, propertyId={item.PropertyId}, isChecked={item.IsChecked} - InventoryController.cs:269");
                }
                
                // Получаем все записи из таблицы Inventories
                var inventories = await _context.Inventories.ToListAsync();
                Console.WriteLine($"TestDatabaseState: inventories count={inventories.Count} - InventoryController.cs:274");
                
                foreach (var inventory in inventories)
                {
                    Console.WriteLine($"TestDatabaseState: inventory id={inventory.Id}, name={inventory.Name}, locationId={inventory.LocationId}, totalItems={inventory.TotalItems}, checkedItems={inventory.CheckedItems} - InventoryController.cs:278");
                }
                
                // Получаем все записи из таблицы Properties
                var properties = await _context.Properties.ToListAsync();
                Console.WriteLine($"TestDatabaseState: properties count={properties.Count} - InventoryController.cs:283");
                
                foreach (var property in properties)
                {
                    Console.WriteLine($"TestDatabaseState: property id={property.Id}, name={property.Name}, locationId={property.LocationId}, propertyTypeId={property.PropertyTypeId} - InventoryController.cs:287");
                }
                
                return Json(new {
                    success = true,
                    message = $"Состояние базы данных: inventories={inventoriesCount}, inventoryItems={inventoryItemsCount}, properties={propertiesCount}, locations={locationsCount}",
                    inventories = inventoriesCount,
                    inventoryItems = inventoryItemsCount,
                    properties = propertiesCount,
                    locations = locationsCount
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"TestDatabaseState: exception occurred: {ex.Message} - InventoryController.cs:301");
                Console.WriteLine($"TestDatabaseState: exception stack trace: {ex.StackTrace} - InventoryController.cs:302");
                return Json(new { success = false, message = $"Ошибка: {ex.Message}" });
            }
        }
        
        // Тестовый метод для создания записей InventoryItem вручную
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> TestCreateInventoryItemManually(int inventoryId, int propertyId)
        {
            try
            {
                Console.WriteLine($"TestCreateInventoryItemManually: called with inventoryId={inventoryId}, propertyId={propertyId} - InventoryController.cs:314");
                
                // Проверяем, существует ли инвентаризация
                var inventory = await _context.Inventories.FindAsync(inventoryId);
                Console.WriteLine($"TestCreateInventoryItemManually: inventory found={inventory != null} - InventoryController.cs:318");
                
                if (inventory == null)
                {
                    return Json(new { success = false, message = "Инвентаризация не найдена" });
                }
                
                // Проверяем, существует ли имущество
                var property = await _context.Properties.FindAsync(propertyId);
                Console.WriteLine($"TestCreateInventoryItemManually: property found={property != null} - InventoryController.cs:327");
                
                if (property == null)
                {
                    return Json(new { success = false, message = "Имущество не найдено" });
                }
                
                // Создаем запись InventoryItem
                var inventoryItem = new InventoryItem
                {
                    InventoryId = inventoryId,
                    PropertyId = propertyId,
                    IsChecked = false
                };
                
                _context.InventoryItems.Add(inventoryItem);
                Console.WriteLine($"TestCreateInventoryItemManually: added inventoryItem to context - InventoryController.cs:343");
                
                // Сохраняем изменения
                var changes = await _context.SaveChangesAsync();
                Console.WriteLine($"TestCreateInventoryItemManually: saved changes, changes={changes} - InventoryController.cs:347");
                
                return Json(new { success = true, message = "Запись InventoryItem создана успешно" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"TestCreateInventoryItemManually: exception occurred: {ex.Message} - InventoryController.cs:353");
                Console.WriteLine($"TestCreateInventoryItemManually: exception stack trace: {ex.StackTrace} - InventoryController.cs:354");
                return Json(new { success = false, message = $"Ошибка: {ex.Message}" });
            }
        }
        
        // Тестовый метод для проверки состояния контекста
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> TestContextState()
        {
            try
            {
                Console.WriteLine($"TestContextState: called - InventoryController.cs:366");
                
                // Получаем количество записей в контексте
                var totalEntries = _context.ChangeTracker.Entries().Count();
                var inventoryEntries = _context.ChangeTracker.Entries<Inventory>().Count();
                var inventoryItemEntries = _context.ChangeTracker.Entries<InventoryItem>().Count();
                var propertyEntries = _context.ChangeTracker.Entries<Property>().Count();
                var locationEntries = _context.ChangeTracker.Entries<Location>().Count();
                
                Console.WriteLine($"TestContextState: totalEntries={totalEntries}, inventoryEntries={inventoryEntries}, inventoryItemEntries={inventoryItemEntries}, propertyEntries={propertyEntries}, locationEntries={locationEntries} - InventoryController.cs:375");
                
                // Получаем состояние записей InventoryItem
                foreach (var entry in _context.ChangeTracker.Entries<InventoryItem>())
                {
                    Console.WriteLine($"TestContextState: inventoryItem entry state={entry.State}, id={entry.Entity.Id}, inventoryId={entry.Entity.InventoryId}, propertyId={entry.Entity.PropertyId} - InventoryController.cs:380");
                }
                
                // Получаем состояние записей Inventory
                foreach (var entry in _context.ChangeTracker.Entries<Inventory>())
                {
                    Console.WriteLine($"TestContextState: inventory entry state={entry.State}, id={entry.Entity.Id}, name={entry.Entity.Name}, locationId={entry.Entity.LocationId} - InventoryController.cs:386");
                }
                
                return Json(new {
                    success = true,
                    message = $"Состояние контекста: totalEntries={totalEntries}, inventoryEntries={inventoryEntries}, inventoryItemEntries={inventoryItemEntries}, propertyEntries={propertyEntries}, locationEntries={locationEntries}",
                    totalEntries = totalEntries,
                    inventoryEntries = inventoryEntries,
                    inventoryItemEntries = inventoryItemEntries,
                    propertyEntries = propertyEntries,
                    locationEntries = locationEntries
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"TestContextState: exception occurred: {ex.Message} - InventoryController.cs:401");
                Console.WriteLine($"TestContextState: exception stack trace: {ex.StackTrace} - InventoryController.cs:402");
                return Json(new { success = false, message = $"Ошибка: {ex.Message}" });
            }
        }
        
        // Тестовый метод для проверки сохранения контекста
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> TestSaveChanges()
        {
            try
            {
                Console.WriteLine($"TestSaveChanges: called - InventoryController.cs:414");
                
                // Получаем состояние контекста до сохранения
                var totalEntriesBefore = _context.ChangeTracker.Entries().Count();
                var inventoryItemEntriesBefore = _context.ChangeTracker.Entries<InventoryItem>().Count();
                Console.WriteLine($"TestSaveChanges: before save  totalEntries={totalEntriesBefore}, inventoryItemEntries={inventoryItemEntriesBefore} - InventoryController.cs:419");
                
                // Получаем состояние записей InventoryItem до сохранения
                foreach (var entry in _context.ChangeTracker.Entries<InventoryItem>())
                {
                    Console.WriteLine($"TestSaveChanges: before save  inventoryItem entry state={entry.State}, id={entry.Entity.Id}, inventoryId={entry.Entity.InventoryId}, propertyId={entry.Entity.PropertyId} - InventoryController.cs:424");
                }
                
                // Сохраняем изменения
                var changes = await _context.SaveChangesAsync();
                Console.WriteLine($"TestSaveChanges: saved changes, changes={changes} - InventoryController.cs:429");
                
                // Получаем состояние контекста после сохранения
                var totalEntriesAfter = _context.ChangeTracker.Entries().Count();
                var inventoryItemEntriesAfter = _context.ChangeTracker.Entries<InventoryItem>().Count();
                Console.WriteLine($"TestSaveChanges: after save  totalEntries={totalEntriesAfter}, inventoryItemEntries={inventoryItemEntriesAfter} - InventoryController.cs:434");
                
                // Получаем состояние записей InventoryItem после сохранения
                foreach (var entry in _context.ChangeTracker.Entries<InventoryItem>())
                {
                    Console.WriteLine($"TestSaveChanges: after save  inventoryItem entry state={entry.State}, id={entry.Entity.Id}, inventoryId={entry.Entity.InventoryId}, propertyId={entry.Entity.PropertyId} - InventoryController.cs:439");
                }
                
                return Json(new {
                    success = true,
                    message = $"Сохранение контекста: changes={changes}",
                    changes = changes
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"TestSaveChanges: exception occurred: {ex.Message} - InventoryController.cs:450");
                Console.WriteLine($"TestSaveChanges: exception stack trace: {ex.StackTrace} - InventoryController.cs:451");
                return Json(new { success = false, message = $"Ошибка: {ex.Message}" });
            }
        }

        // Страница сканирования для инвентаризации
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
                .Include(ii => ii.Property)
                .ToListAsync();
                
            // Добавляем логирование для диагностики получения записей InventoryItem
            Console.WriteLine($"GetLocationProperties: retrieved {inventoryItems.Count} inventory items for inventoryId={inventoryId} - InventoryController.cs:505");
            
            // Дополнительная проверка - получаем все записи InventoryItem для диагностики
            var totalInventoryItems = await _context.InventoryItems.CountAsync();
            Console.WriteLine($"GetLocationProperties: total inventory items in database={totalInventoryItems} - InventoryController.cs:509");
            
            // Дополнительная проверка - получаем все записи InventoryItem для диагностики
            var allInventoryItems = await _context.InventoryItems.CountAsync();
            Console.WriteLine($"GetLocationProperties: total inventory items in database={allInventoryItems} - InventoryController.cs:513");

            // Создаем словарь для быстрого поиска статуса проверки
            var inventoryItemDict = inventoryItems.ToDictionary(ii => ii.PropertyId, ii => ii);

            // Добавляем логирование для диагностики
            Console.WriteLine($"GetLocationProperties: inventoryId={inventoryId}, locationId={inventory.LocationId} - InventoryController.cs:519");
            Console.WriteLine($"GetLocationProperties: total properties in location={allLocationProperties.Count} - InventoryController.cs:520");
            Console.WriteLine($"GetLocationProperties: inventory items count={inventoryItems.Count} - InventoryController.cs:521");
            var properties = allLocationProperties
                .Select(p => new
                {
                    Id = p.Id,
                    Name = p.Name ?? "Не указано",
                    PropertyTypeName = p.PropertyType != null ? p.PropertyType.Name : "Не указан",
                    InventoryNumber = p.InventoryNumber ?? "Не указан",
                    QRCode = p.QRCode ?? "",
                    Barcode = p.Barcode ?? "",
                    // Проверяем, есть ли это имущество в инвентаризации и проверено ли оно
                    IsChecked = inventoryItemDict.ContainsKey(p.Id) ? inventoryItemDict[p.Id].IsChecked : false,
                    CheckDate = inventoryItemDict.ContainsKey(p.Id) ? inventoryItemDict[p.Id].CheckDate : null
                })
                .OrderBy(p => p.Name)
                .ToList();

            // Добавляем логирование для диагностики возвращаемых данных
            Console.WriteLine($"GetLocationProperties: returning {properties.Count} properties - InventoryController.cs:539");
            
            return Json(new { success = true, properties });
        }

            // Проверка имущества по QR коду или инвентарному номеру
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> CheckItem(int inventoryId, string code)
        {
            // Добавляем логирование для диагностики
            Console.WriteLine($"CheckItem: inventoryId={inventoryId}, code={code} - InventoryController.cs:550");
             
            var inventory = await _context.Inventories
                .Include(i => i.InventoryItems)
                .ThenInclude(ii => ii.Property)
                .FirstOrDefaultAsync(i => i.Id == inventoryId);
             
            Console.WriteLine($"CheckItem: inventory found={inventory != null} - InventoryController.cs:557");
             
            if (inventory != null)
            {
                Console.WriteLine($"CheckItem: inventory completed={inventory.IsCompleted} - InventoryController.cs:561");
                Console.WriteLine($"CheckItem: inventory locationId={inventory.LocationId} - InventoryController.cs:562");
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
            Console.WriteLine($"CheckItem: searching property with QRCode={code} or InventoryNumber={code} - InventoryController.cs:576");
             
            // Ищем имущество по QR коду или инвентарному номеру
            var property = await _context.Properties
                .FirstOrDefaultAsync(p => p.QRCode == code || p.InventoryNumber == code);
                 
            Console.WriteLine($"CheckItem: property found={property != null} - InventoryController.cs:582");
             
            if (property != null)
            {
                Console.WriteLine($"CheckItem: property locationId={property.LocationId}, property id={property.Id} - InventoryController.cs:586");
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
            inventoryItem.CheckedById = User.Identity.Name; // Здесь должно быть ID пользователя
 
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

        // Завершение инвентаризации вручную
        [HttpPost]
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
        
        // Тестовый метод для проверки создания записи InventoryItem напрямую через контекст
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> TestCreateInventoryItemDirect()
        {
            try
            {
                Console.WriteLine($"TestCreateInventoryItemDirect: called - InventoryController.cs:689");
                
                // Создаем запись InventoryItem напрямую через контекст
                var inventoryItem = new InventoryItem
                {
                    InventoryId = 1,
                    PropertyId = 3,
                    IsChecked = false
                };
                
                _context.InventoryItems.Add(inventoryItem);
                Console.WriteLine($"TestCreateInventoryItemDirect: added inventoryItem to context - InventoryController.cs:700");
                
                // Получаем состояние контекста до сохранения
                var totalEntriesBefore = _context.ChangeTracker.Entries().Count();
                var inventoryItemEntriesBefore = _context.ChangeTracker.Entries<InventoryItem>().Count();
                Console.WriteLine($"TestCreateInventoryItemDirect: before save  totalEntries={totalEntriesBefore}, inventoryItemEntries={inventoryItemEntriesBefore} - InventoryController.cs:705");
                
                // Получаем состояние записей InventoryItem до сохранения
                foreach (var entry in _context.ChangeTracker.Entries<InventoryItem>())
                {
                    Console.WriteLine($"TestCreateInventoryItemDirect: before save  inventoryItem entry state={entry.State}, id={entry.Entity.Id}, inventoryId={entry.Entity.InventoryId}, propertyId={entry.Entity.PropertyId} - InventoryController.cs:710");
                }
                
                // Сохраняем изменения
                var changes = await _context.SaveChangesAsync();
                Console.WriteLine($"TestCreateInventoryItemDirect: saved changes, changes={changes} - InventoryController.cs:715");
                
                // Получаем состояние контекста после сохранения
                var totalEntriesAfter = _context.ChangeTracker.Entries().Count();
                var inventoryItemEntriesAfter = _context.ChangeTracker.Entries<InventoryItem>().Count();
                Console.WriteLine($"TestCreateInventoryItemDirect: after save  totalEntries={totalEntriesAfter}, inventoryItemEntries={inventoryItemEntriesAfter} - InventoryController.cs:720");
                
                // Получаем состояние записей InventoryItem после сохранения
                foreach (var entry in _context.ChangeTracker.Entries<InventoryItem>())
                {
                    Console.WriteLine($"TestCreateInventoryItemDirect: after save  inventoryItem entry state={entry.State}, id={entry.Entity.Id}, inventoryId={entry.Entity.InventoryId}, propertyId={entry.Entity.PropertyId} - InventoryController.cs:725");
                }
                
                return Json(new {
                    success = true,
                    message = $"Создание записи InventoryItem напрямую: changes={changes}",
                    changes = changes
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"TestCreateInventoryItemDirect: exception occurred: {ex.Message} - InventoryController.cs:736");
                Console.WriteLine($"TestCreateInventoryItemDirect: exception stack trace: {ex.StackTrace} - InventoryController.cs:737");
                return Json(new { success = false, message = $"Ошибка: {ex.Message}" });
            }
        }
        
        // Тестовый метод для проверки создания записи InventoryItem с явным указанием ID
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> TestCreateInventoryItemWithId()
        {
            try
            {
                Console.WriteLine($"TestCreateInventoryItemWithId: called - InventoryController.cs:749");
                
                // Создаем запись InventoryItem с явным указанием ID
                var inventoryItem = new InventoryItem
                {
                    Id = 0, // Позволяем базе данных сгенерировать ID
                    InventoryId = 1,
                    PropertyId = 3,
                    IsChecked = false
                };
                
                _context.InventoryItems.Add(inventoryItem);
                Console.WriteLine($"TestCreateInventoryItemWithId: added inventoryItem to context, inventoryItem.Id={inventoryItem.Id} - InventoryController.cs:761");
                
                // Получаем состояние контекста до сохранения
                var totalEntriesBefore = _context.ChangeTracker.Entries().Count();
                var inventoryItemEntriesBefore = _context.ChangeTracker.Entries<InventoryItem>().Count();
                Console.WriteLine($"TestCreateInventoryItemWithId: before save  totalEntries={totalEntriesBefore}, inventoryItemEntries={inventoryItemEntriesBefore} - InventoryController.cs:766");
                
                // Получаем состояние записей InventoryItem до сохранения
                foreach (var entry in _context.ChangeTracker.Entries<InventoryItem>())
                {
                    Console.WriteLine($"TestCreateInventoryItemWithId: before save  inventoryItem entry state={entry.State}, id={entry.Entity.Id}, inventoryId={entry.Entity.InventoryId}, propertyId={entry.Entity.PropertyId} - InventoryController.cs:771");
                }
                
                // Сохраняем изменения
                var changes = await _context.SaveChangesAsync();
                Console.WriteLine($"TestCreateInventoryItemWithId: saved changes, changes={changes}, inventoryItem.Id={inventoryItem.Id} - InventoryController.cs:776");
                
                // Получаем состояние контекста после сохранения
                var totalEntriesAfter = _context.ChangeTracker.Entries().Count();
                var inventoryItemEntriesAfter = _context.ChangeTracker.Entries<InventoryItem>().Count();
                Console.WriteLine($"TestCreateInventoryItemWithId: after save  totalEntries={totalEntriesAfter}, inventoryItemEntries={inventoryItemEntriesAfter} - InventoryController.cs:781");
                
                // Получаем состояние записей InventoryItem после сохранения
                foreach (var entry in _context.ChangeTracker.Entries<InventoryItem>())
                {
                    Console.WriteLine($"TestCreateInventoryItemWithId: after save  inventoryItem entry state={entry.State}, id={entry.Entity.Id}, inventoryId={entry.Entity.InventoryId}, propertyId={entry.Entity.PropertyId} - InventoryController.cs:786");
                }
                
                return Json(new {
                    success = true,
                    message = $"Создание записи InventoryItem с ID: changes={changes}, inventoryItem.Id={inventoryItem.Id}",
                    changes = changes,
                    inventoryItemId = inventoryItem.Id
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"TestCreateInventoryItemWithId: exception occurred: {ex.Message} - InventoryController.cs:798");
                Console.WriteLine($"TestCreateInventoryItemWithId: exception stack trace: {ex.StackTrace} - InventoryController.cs:799");
                return Json(new { success = false, message = $"Ошибка: {ex.Message}" });
            }
        }
    }
}