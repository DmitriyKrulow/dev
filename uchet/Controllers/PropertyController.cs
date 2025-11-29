using System.Drawing;
using System.Drawing.Imaging;
using QRCoder;
using Microsoft.AspNetCore.Mvc;
using uchet.Data;
using uchet.Models;
using Microsoft.AspNetCore.Authorization;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using ZXing;
using ZXing.Common;
using ClosedXML.Excel;
using uchet.Services;
using System.Runtime.Versioning;
using System.Text;
using Microsoft.AspNetCore.Hosting;

namespace uchet.Controllers
{
    [Authorize]
    [SupportedOSPlatform("windows")]
    public class PropertyController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly BarcodeDocxService _barcodeDocxService;
        private readonly IWebHostEnvironment _environment;

        public PropertyController(ApplicationDbContext context, IWebHostEnvironment environment, BarcodeDocxService barcodeDocxService)
        {
            _context = context;
            _environment = environment;
            _barcodeDocxService = barcodeDocxService;
        }

        public async Task<IActionResult> Index(int? propertyTypeId, int? locationId, int? userId)
        {
            // Отладка: проверяем параметры фильтрации
            Console.WriteLine($"Параметры фильтрации  Тип: {propertyTypeId}, Локация: {locationId}, Пользователь: {userId} - PropertyController.cs:42");
            
            var properties = _context.Properties
                .Include(p => p.PropertyType)
                .Include(p => p.Location)
                .Include(p => p.AssignedUser)
                .AsQueryable();

            if (propertyTypeId.HasValue)
            {
                Console.WriteLine($"Фильтрация по типу имущества: {propertyTypeId.Value} - PropertyController.cs:52");
                properties = properties.Where(p => p.PropertyTypeId == propertyTypeId.Value);
                Console.WriteLine($"Количество имущества после фильтрации по типу: {properties.Count()} - PropertyController.cs:54");
            }

            if (locationId.HasValue)
            {
                Console.WriteLine($"Фильтрация по локации: {locationId.Value} - PropertyController.cs:59");
                properties = properties.Where(p => p.LocationId == locationId.Value);
                Console.WriteLine($"Количество имущества после фильтрации по локации: {properties.Count()} - PropertyController.cs:61");
            }

            if (userId.HasValue)
            {
                Console.WriteLine($"Фильтрация по пользователю: {userId.Value} - PropertyController.cs:66");
                properties = properties.Where(p => p.AssignedUserId == userId.Value);
                Console.WriteLine($"Количество имущества после фильтрации по пользователю: {properties.Count()} - PropertyController.cs:68");
            }

            var propertyTypes = await _context.PropertyTypes.ToListAsync();
            var locations = await _context.Locations.ToListAsync();
            var users = await _context.Users.Where(u => u.IsActive).ToListAsync();
            
            // Отладка: проверяем загрузку данных
            Console.WriteLine($"Загружено типов имущества: {propertyTypes.Count} - PropertyController.cs:76");
            Console.WriteLine($"Загружено локаций: {locations.Count} - PropertyController.cs:77");
            Console.WriteLine($"Загружено активных пользователей: {users.Count} - PropertyController.cs:78");
            
            // Отладка: проверяем содержимое базы данных
            var locationsInDb = await _context.Locations.ToListAsync();
            var propertiesInDb = await _context.Properties.ToListAsync();
            Console.WriteLine($"Локаций в базе: {locationsInDb.Count} - PropertyController.cs:83");
            Console.WriteLine($"Имущества в базе: {propertiesInDb.Count} - PropertyController.cs:84");
            
            // Отладка: выводим содержимое списков
            foreach (var location in locationsInDb)
            {
                Console.WriteLine($"Локация в базе: {location.Id}  {location.Name} - PropertyController.cs:89");
            }
            
            foreach (var propertyType in propertyTypes)
            {
                Console.WriteLine($"Тип имущества: {propertyType.Id}  {propertyType.Name} - PropertyController.cs:94");
            }
            
            // Отладка: выводим содержимое списка пользователей
            foreach (var user in users)
            {
                Console.WriteLine($"Пользователь: {user.Id}  {user.Name} - PropertyController.cs:100");
            }
            
            // Отладка: выводим информацию о первых 5 имуществах
            foreach (var property in propertiesInDb.Take(5))
            {
                Console.WriteLine($"Имущество в базе: {property.Id}  {property.Name}, Локация ID: {property.LocationId}, Тип ID: {property.PropertyTypeId} - PropertyController.cs:106");
            }

            ViewBag.PropertyTypes = new SelectList(propertyTypes, "Id", "Name", propertyTypeId);
            ViewBag.Locations = new SelectList(locations, "Id", "Name", locationId);
            ViewBag.Users = new SelectList(users, "Id", "Name", userId);

            // Отладка: проверяем общее количество имущества в базе
            var totalProperties = await _context.Properties.CountAsync();
            Console.WriteLine($"Общее количество имущества в базе: {totalProperties} - PropertyController.cs:115");
            
            var propertyList = await properties.ToListAsync();
            Console.WriteLine($"Количество имущества после фильтрации: {propertyList.Count} - PropertyController.cs:118");
            foreach (var prop in propertyList.Take(5)) // Ограничиваем вывод первыми 5 элементами
            {
                Console.WriteLine($"Имущество: {prop.Name}, Срок использования: {prop.UsagePeriod} - PropertyController.cs:121");
                Console.WriteLine($"Локация: {prop.Location?.Name}, Тип: {prop.PropertyType?.Name} - PropertyController.cs:122");
                Console.WriteLine($"ID локации: {prop.LocationId}, ID типа: {prop.PropertyTypeId} - PropertyController.cs:123");
                Console.WriteLine($"Локация существует: {prop.Location != null}, Тип существует: {prop.PropertyType != null} - PropertyController.cs:124");
                Console.WriteLine($"Назначено пользователю: {prop.AssignedUser?.Name} - PropertyController.cs:125");
                Console.WriteLine($"ID назначенного пользователя: {prop.AssignedUserId} - PropertyController.cs:126");
                Console.WriteLine($"Проверка отображения локации: {prop.Location?.Name}, Типа: {prop.PropertyType?.Name} - PropertyController.cs:127");
            }
            
            // Отладка: проверяем количество локаций и типов имущества
            var locationsCount = await _context.Locations.CountAsync();
            var propertyTypesCount = await _context.PropertyTypes.CountAsync();
            Console.WriteLine($"Количество локаций: {locationsCount}, Количество типов имущества: {propertyTypesCount} - PropertyController.cs:133");
            
            // Отладка: проверяем содержимое списков локаций и типов имущества
            Console.WriteLine($"Количество локаций в ViewBag: {locations.Count} - PropertyController.cs:136");
            Console.WriteLine($"Количество типов имущества в ViewBag: {propertyTypes.Count} - PropertyController.cs:137");
            
            // Отладка: выводим содержимое списков
            foreach (var location in locations)
            {
                Console.WriteLine($"Локация: {location.Id}  {location.Name} - PropertyController.cs:142");
            }
            
            foreach (var propertyType in propertyTypes)
            {
                Console.WriteLine($"Тип имущества: {propertyType.Id}  {propertyType.Name} - PropertyController.cs:147");
            }
            
            // Отладка: выводим содержимое списка пользователей
            foreach (var user in users)
            {
                Console.WriteLine($"Пользователь: {user.Id}  {user.Name} - PropertyController.cs:153");
            }
            
            return View(propertyList);
        }

        public async Task<IActionResult> Details(int id)
        {
            var property = await _context.Properties
                .Include(p => p.PropertyType)
                .Include(p => p.Location)
                .Include(p => p.AssignedUser)
                .Include(p => p.PropertyFiles)
                .FirstOrDefaultAsync(p => p.Id == id);
            
            // Отладка: проверяем загрузку свойства
            Console.WriteLine($"Загружено свойство: {property?.Name} - PropertyController.cs:169");
            Console.WriteLine($"ID локации: {property?.LocationId}, ID типа: {property?.PropertyTypeId} - PropertyController.cs:170");
            Console.WriteLine($"Локация существует: {property?.Location != null}, Тип существует: {property?.PropertyType != null} - PropertyController.cs:171");
                
            if (property == null)
            {
                return NotFound();
            }
            
            Console.WriteLine($"Детали имущества: {property.Name}, Срок использования: {property.UsagePeriod} - PropertyController.cs:178");
            Console.WriteLine($"Локация: {property.Location?.Name}, Тип: {property.PropertyType?.Name} - PropertyController.cs:179");
            
            // Отладка: проверяем связанные данные
            Console.WriteLine($"ID локации: {property.LocationId}, ID типа: {property.PropertyTypeId} - PropertyController.cs:182");
            Console.WriteLine($"Назначено пользователю: {property.AssignedUserId} - PropertyController.cs:183");
            Console.WriteLine($"Назначенный пользователь: {property.AssignedUser?.Name} - PropertyController.cs:184");
            
            // Отладка: проверяем наличие связанных объектов
            Console.WriteLine($"Локация существует: {property.Location != null} - PropertyController.cs:187");
            Console.WriteLine($"Тип имущества существует: {property.PropertyType != null} - PropertyController.cs:188");
            Console.WriteLine($"Назначенный пользователь существует: {property.AssignedUser != null} - PropertyController.cs:189");
            
            // Отладка: проверяем ID связанных объектов
            Console.WriteLine($"ID локации: {property.Location?.Id}, ID типа: {property.PropertyType?.Id} - PropertyController.cs:192");
            Console.WriteLine($"ID назначенного пользователя: {property.AssignedUser?.Id} - PropertyController.cs:193");
            
            // Отладка: проверяем названия связанных объектов
            Console.WriteLine($"Название локации: {property.Location?.Name} - PropertyController.cs:196");
            Console.WriteLine($"Название типа: {property.PropertyType?.Name} - PropertyController.cs:197");
            Console.WriteLine($"Имя назначенного пользователя: {property.AssignedUser?.Name} - PropertyController.cs:198");
            
            // Отладка: проверяем, все ли данные загружены
            Console.WriteLine($"Все данные загружены: Локация={property.Location != null}, Тип={property.PropertyType != null}, Пользователь={property.AssignedUser != null} - PropertyController.cs:201");
            Console.WriteLine($"Проверка отображения локации: {property.Location?.Name}, Типа: {property.PropertyType?.Name} - PropertyController.cs:202");
            
            return View(property);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkDelete([FromBody] BulkDeleteModel model)
        {
            try
            {
                var properties = await _context.Properties
                    .Where(p => model.Ids.Contains(p.Id))
                    .ToListAsync();

                if (!properties.Any())
                {
                    return Json(new { success = false, message = "Записи не найдены" });
                }

                _context.Properties.RemoveRange(properties);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = $"Удалено {properties.Count} записей" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Ошибка при удалении: " + ex.Message });
            }
        }

        public class BulkDeleteModel
        {
            public List<int> Ids { get; set; } = new List<int>();
        }

        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Create()
        {
            var propertyTypes = await _context.PropertyTypes.ToListAsync();
            var locations = await _context.Locations.ToListAsync();
            var users = await _context.Users.Where(u => u.IsActive).ToListAsync();
            
            // Отладка: проверяем загрузку данных для создания
            Console.WriteLine($"Загружено типов имущества для создания: {propertyTypes.Count} - PropertyController.cs:247");
            Console.WriteLine($"Загружено локаций для создания: {locations.Count} - PropertyController.cs:248");
            Console.WriteLine($"Загружено пользователей для создания: {users.Count} - PropertyController.cs:249");
            
            // Отладка: выводим содержимое списков для создания
            foreach (var location in locations)
            {
                Console.WriteLine($"Локация для создания: {location.Id}  {location.Name} - PropertyController.cs:254");
            }
            
            foreach (var propertyType in propertyTypes)
            {
                Console.WriteLine($"Тип имущества для создания: {propertyType.Id}  {propertyType.Name} - PropertyController.cs:259");
            }
            
            foreach (var user in users)
            {
                Console.WriteLine($"Пользователь для создания: {user.Id}  {user.Name} - PropertyController.cs:264");
            }
            
            ViewBag.PropertyTypes = new SelectList(propertyTypes, "Id", "Name");
            ViewBag.Locations = new SelectList(locations, "Id", "Name");
            ViewBag.Users = new SelectList(users, "Id", "Name");
            
            return View(new CreatePropertyDto());
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreatePropertyDto propertyDto)
        {
            // Загружаем связанные данные для проверки валидации
            var propertyTypes = await _context.PropertyTypes.ToListAsync();
            var locations = await _context.Locations.ToListAsync();
            var users = await _context.Users.Where(u => u.IsActive).ToListAsync();
            
            ViewBag.PropertyTypes = new SelectList(propertyTypes, "Id", "Name");
            ViewBag.Locations = new SelectList(locations, "Id", "Name");
            ViewBag.Users = new SelectList(users, "Id", "Name");
            
            // Проверяем, что выбраны обязательные поля
            if (propertyDto.PropertyTypeId == 0)
            {
                ModelState.AddModelError("PropertyTypeId", "Пожалуйста, выберите тип имущества");
            }
            
            if (propertyDto.LocationId == 0)
            {
                ModelState.AddModelError("LocationId", "Пожалуйста, выберите размещение");
            }
            
            // Добавляем отладочную информацию
            Console.WriteLine($"Попытка создания имущества: {propertyDto.Name} - PropertyController.cs:300");
            Console.WriteLine($"PropertyTypeId: {propertyDto.PropertyTypeId} - PropertyController.cs:301");
            Console.WriteLine($"LocationId: {propertyDto.LocationId} - PropertyController.cs:302");
            Console.WriteLine($"InventoryNumber: {propertyDto.InventoryNumber} - PropertyController.cs:303");
            
            // Проверяем валидацию модели
            Console.WriteLine($"ModelState.IsValid: {ModelState.IsValid} - PropertyController.cs:306");
            
            if (ModelState.IsValid)
            {
                try
                {
                    Console.WriteLine("Модель валидна, начинаем сохранение... - PropertyController.cs:312");
                    
                    // Создаем новый экземпляр Property на основе данных из DTO
                    var property = new Property
                    {
                        Name = propertyDto.Name,
                        Description = propertyDto.Description,
                        LocationId = propertyDto.LocationId,
                        PropertyTypeId = propertyDto.PropertyTypeId,
                        AssignedUserId = propertyDto.AssignedUserId,
                        InventoryNumber = propertyDto.InventoryNumber,
                        BalanceDate = propertyDto.BalanceDate?.ToUniversalTime(),
                        UsagePeriod = propertyDto.UsagePeriod,
                        Cost = propertyDto.Cost,
                        LastMaintenanceDate = propertyDto.LastMaintenanceDate?.ToUniversalTime(),
                        ExpiryDate = propertyDto.ExpiryDate?.ToUniversalTime(),
                        QRCode = GenerateQRCode(propertyDto.InventoryNumber), // Генерируем QR код на основе инвентарного номера
                        Barcode = GenerateBarcode(propertyDto.InventoryNumber) // Генерируем штрих-код на основе инвентарного номера
                    };
                    
                    // Отладка: проверяем создание имущества
                    Console.WriteLine($"Создание имущества: {property.Name}, Локация ID: {property.LocationId}, Тип ID: {property.PropertyTypeId} - PropertyController.cs:333");
                    
                    // Добавляем имущество в контекст
                    _context.Properties.Add(property);
                    await _context.SaveChangesAsync();
                    
                    Console.WriteLine($"Имущество сохранено с Id: {property.Id} - PropertyController.cs:339");
                    
                    // Отладка: проверяем сохранение связанных данных
                    var savedProperty = await _context.Properties
                        .Include(p => p.Location)
                        .Include(p => p.PropertyType)
                        .FirstOrDefaultAsync(p => p.Id == property.Id);
                    Console.WriteLine($"Сохраненное имущество: {savedProperty?.Name}, Локация: {savedProperty?.Location?.Name}, Тип: {savedProperty?.PropertyType?.Name} - PropertyController.cs:346");
                    
                    Console.WriteLine("Имущество успешно обновлено с QR и штрихкодом - PropertyController.cs:348");
                    
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    // Логируем ошибку
                    Console.WriteLine($"Ошибка при добавлении имущества: {ex} - PropertyController.cs:355");
                    ModelState.AddModelError("", "Произошла ошибка при добавлении имущества: " + ex.Message);
                }
            }
            else
            {
                Console.WriteLine("Модель не валидна: - PropertyController.cs:361");
                foreach (var key in ModelState.Keys)
                {
                    var state = ModelState[key];
                    if (state.Errors.Count > 0)
                    {
                        Console.WriteLine($"Поле : - PropertyController.cs:367");
                    }
                }
            }
            
            // Если валидация не пройдена или произошла ошибка, возвращаем представление с данными
            // Создаем новый экземпляр CreatePropertyDto для отображения в форме
            var propertyDtoForView = new CreatePropertyDto
            {
                Name = propertyDto.Name,
                Description = propertyDto.Description,
                LocationId = propertyDto.LocationId,
                PropertyTypeId = propertyDto.PropertyTypeId,
                AssignedUserId = propertyDto.AssignedUserId,
                InventoryNumber = propertyDto.InventoryNumber,
                BalanceDate = propertyDto.BalanceDate?.ToUniversalTime(),
                UsagePeriod = propertyDto.UsagePeriod,
                Cost = propertyDto.Cost,
                LastMaintenanceDate = propertyDto.LastMaintenanceDate?.ToUniversalTime(),
                ExpiryDate = propertyDto.ExpiryDate
            };
            
            return View(propertyDtoForView);
        }
        
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Edit(int id)
        {
            var property = await _context.Properties.FindAsync(id);
            if (property == null)
            {
                return NotFound();
            }
            
            // Отладка: проверяем загрузку имущества для редактирования
            Console.WriteLine($"Загружено имущество для редактирования: {property.Name}, Локация ID: {property.LocationId}, Тип ID: {property.PropertyTypeId} - PropertyController.cs:402");
            
            var propertyTypes = await _context.PropertyTypes.ToListAsync();
            var locations = await _context.Locations.ToListAsync();
            var users = await _context.Users.Where(u => u.IsActive).ToListAsync();
            
            // Отладка: проверяем загрузку данных для редактирования
            Console.WriteLine($"Загружено типов имущества для редактирования: {propertyTypes.Count} - PropertyController.cs:409");
            Console.WriteLine($"Загружено локаций для редактирования: {locations.Count} - PropertyController.cs:410");
            Console.WriteLine($"Загружено пользователей для редактирования: {users.Count} - PropertyController.cs:411");
            
            // Отладка: выводим содержимое списков для редактирования
            foreach (var location in locations)
            {
                Console.WriteLine($"Локация для редактирования: {location.Id}  {location.Name} - PropertyController.cs:416");
            }
            
            foreach (var propertyType in propertyTypes)
            {
                Console.WriteLine($"Тип имущества для редактирования: {propertyType.Id}  {propertyType.Name} - PropertyController.cs:421");
            }
            
            foreach (var user in users)
            {
                Console.WriteLine($"Пользователь для редактирования: {user.Id}  {user.Name} - PropertyController.cs:426");
            }
            
            ViewBag.PropertyTypes = new SelectList(propertyTypes, "Id", "Name", property.PropertyTypeId);
            ViewBag.Locations = new SelectList(locations, "Id", "Name", property.LocationId);
            ViewBag.Users = new SelectList(users, "Id", "Name", property.AssignedUserId);
            
            return View(property);
        }
        
        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Property property)
        {
            if (id != property.Id)
            {
                return NotFound();
            }
            
            // Загружаем связанные данные для проверки валидации
            var propertyTypes = await _context.PropertyTypes.ToListAsync();
            var locations = await _context.Locations.ToListAsync();
            var users = await _context.Users.Where(u => u.IsActive).ToListAsync();
            
            ViewBag.PropertyTypes = new SelectList(propertyTypes, "Id", "Name");
            ViewBag.Locations = new SelectList(locations, "Id", "Name");
            ViewBag.Users = new SelectList(users, "Id", "Name");
            
            if (ModelState.IsValid)
            {
                try
                {
                    // Отладка: проверяем обновление имущества
                    Console.WriteLine($"Обновление имущества: {property.Name}, Локация ID: {property.LocationId}, Тип ID: {property.PropertyTypeId} - PropertyController.cs:460");
                    
                    _context.Update(property);
                    await _context.SaveChangesAsync();
                    
                    // Отладка: проверяем сохранение обновленного имущества
                    var updatedProperty = await _context.Properties
                        .Include(p => p.Location)
                        .Include(p => p.PropertyType)
                        .FirstOrDefaultAsync(p => p.Id == property.Id);
                    Console.WriteLine($"Обновленное имущество: {updatedProperty?.Name}, Локация: {updatedProperty?.Location?.Name}, Тип: {updatedProperty?.PropertyType?.Name} - PropertyController.cs:470");
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PropertyExists(property.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction("Index");
            }
            
            return View(property);
        }
        
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var property = await _context.Properties.FindAsync(id);
            if (property == null)
            {
                return NotFound();
            }
            
            // Отладка: проверяем загрузку имущества для удаления
            Console.WriteLine($"Удаление: {property.Name}, Локация: {property.Location?.Name}, Тип: {property.PropertyType?.Name} - PropertyController.cs:499");
            
            return View(property);
        }
        
        [HttpPost, ActionName("Delete")]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var property = await _context.Properties.FindAsync(id);
            if (property != null)
            {
                // Отладка: проверяем удаление имущества
                Console.WriteLine($"Удаление подтверждено: {property.Name}, Локация: {property.Location?.Name}, Тип: {property.PropertyType?.Name} - PropertyController.cs:513");
                
                _context.Properties.Remove(property);
                await _context.SaveChangesAsync();
            }
            
            return RedirectToAction("Index");
        }
        
        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadFile(int propertyId, IFormFile file, string fileType)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("Файл не выбран");
            }
            
            var property = await _context.Properties.FindAsync(propertyId);
            if (property == null)
            {
                return NotFound();
            }
            
            // Отладка: проверяем загрузку имущества для загрузки файла
            Console.WriteLine($"Загрузка файла: {property.Name}, Локация: {property.Location?.Name}, Тип: {property.PropertyType?.Name} - PropertyController.cs:539");
            
            // Создаем директорию для файлов имущества если её нет
            var propertyFilesPath = Path.Combine(_environment.WebRootPath, "property_files");
            if (!Directory.Exists(propertyFilesPath))
            {
                Directory.CreateDirectory(propertyFilesPath);
            }
            
            // Генерируем уникальное имя файла
            var fileName = $"{Guid.NewGuid()}_{file.FileName}";
            var filePath = Path.Combine(propertyFilesPath, fileName);
            
            // Сохраняем файл
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }
            
            // Создаем запись о файле в базе данных
            var propertyFile = new PropertyFile
            {
                PropertyId = propertyId,
                FilePath = $"/property_files/{fileName}",
                FileType = fileType
            };
            
            _context.PropertyFiles.Add(propertyFile);
            await _context.SaveChangesAsync();
            
            return RedirectToAction("Details", new { id = propertyId });
        }
        
        private bool PropertyExists(int id)
        {
            return _context.Properties.Any(e => e.Id == id);
        }
        
        private string GenerateQRCode(string inventoryNumber)
        {
            // Генерация QR кода на основе инвентарного номера
            return $"QR_{inventoryNumber}";
        }
        
        private string GenerateBarcode(string inventoryNumber)
        {
            // Генерация штрих-кода на основе инвентарного номера
            // Используем только допустимые символы для штрих-кода (цифры)
            var cleanInventoryNumber = new string(inventoryNumber.Where(char.IsDigit).ToArray());
            return cleanInventoryNumber;
        }
        
        public IActionResult GenerateQRCodeImage(int id)
        {
            // Получаем имущество по ID
            var property = _context.Properties.FirstOrDefault(p => p.Id == id);
            if (property == null)
            {
                return NotFound();
            }
            
            // Отладка: проверяем загрузку имущества для генерации QR кода
            Console.WriteLine($"Генерация QR кода: {property.Name}, Локация: {property.Location?.Name}, Тип: {property.PropertyType?.Name} - PropertyController.cs:601");
            
            // Генерация изображения QR кода на основе инвентарного номера
            using (var qrGenerator = new QRCodeGenerator())
            using (var qrCodeData = qrGenerator.CreateQrCode(property.InventoryNumber, QRCodeGenerator.ECCLevel.Q))
            using (var qrCode = new QRCode(qrCodeData))
            using (var bitmap = qrCode.GetGraphic(20))
            {
                using (var stream = new MemoryStream())
                {
                    bitmap.Save(stream, ImageFormat.Png);
                    return File(stream.ToArray(), "image/png");
                }
            }
        }
        
        public IActionResult GenerateBarcodeImage(int id)
        {
            // Получаем имущество по ID
            var property = _context.Properties.FirstOrDefault(p => p.Id == id);
            if (property == null)
            {
                Console.WriteLine($"Property with id {id} not found - PropertyController.cs:623");
                return NotFound();
            }
            
            // Отладка: проверяем загрузку имущества для генерации штрих-кода
            Console.WriteLine($"Генерация штрихкода: {property.Name}, Локация: {property.Location?.Name}, Тип: {property.PropertyType?.Name} - PropertyController.cs:628");
            
            // Проверяем, есть ли инвентарный номер
            if (string.IsNullOrEmpty(property.InventoryNumber))
            {
                Console.WriteLine($"Inventory number is null or empty for property id {id} - PropertyController.cs:633");
                // Возвращаем пустое изображение или изображение-заглушку
                using (var bitmap = new Bitmap(300, 100))
                using (var graphics = Graphics.FromImage(bitmap))
                {
                    graphics.Clear(Color.White);
                    using (var font = new Font("Arial", 12))
                    {
                        graphics.DrawString("No Barcode", font, Brushes.Black, new PointF(10, 10));
                    }
                    
                    using (var stream = new MemoryStream())
                    {
                        bitmap.Save(stream, ImageFormat.Png);
                        return File(stream.ToArray(), "image/png");
                    }
                }
            }
            
            // Генерация изображения штрих-кода на основе инвентарного номера
            // Используем Code 128 для генерации штрих-кода
            try
            {
                var writer = new ZXing.Windows.Compatibility.BarcodeWriter()
                {
                    Format = ZXing.BarcodeFormat.CODE_128,
                    Options = new EncodingOptions
                    {
                        Width = 300,
                        Height = 100,
                        Margin = 10
                    }
                };
                
                var bitmap = writer.Write(property.InventoryNumber);
                
                using (var stream = new MemoryStream())
                {
                    bitmap.Save(stream, ImageFormat.Png);
                    Console.WriteLine($"Successfully generated barcode for property id {id} with inventory number {property.InventoryNumber} - PropertyController.cs:672");
                    return File(stream.ToArray(), "image/png");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating barcode for property id {id}: {ex.Message} - PropertyController.cs:678");
                // Возвращаем изображение с ошибкой
                using (var bitmap = new Bitmap(300, 100))
                using (var graphics = Graphics.FromImage(bitmap))
                {
                    graphics.Clear(Color.White);
                    using (var font = new Font("Arial", 12))
                    {
                        graphics.DrawString("Barcode Error", font, Brushes.Black, new PointF(10, 10));
                    }
                    
                    using (var stream = new MemoryStream())
                    {
                        bitmap.Save(stream, ImageFormat.Png);
                        return File(stream.ToArray(), "image/png");
                    }
                }
            }
        }
        
        [Authorize(Roles = "Admin,Manager")]
        public IActionResult Import()
        {
            return View();
        }
        
        [Authorize(Roles = "Admin,Manager")]
        public IActionResult DownloadImportTemplate()
        {
            // Создаем шаблон Excel в памяти
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Имущество");
                
                // Добавляем заголовки
                worksheet.Cell("A1").Value = "Название";
                worksheet.Cell("B1").Value = "Описание";
                worksheet.Cell("C1").Value = "Инвентарный номер";
                worksheet.Cell("D1").Value = "Тип имущества";
                worksheet.Cell("E1").Value = "Размещение";
                worksheet.Cell("F1").Value = "Назначенный пользователь";
                worksheet.Cell("G1").Value = "Дата баланса";
                worksheet.Cell("H1").Value = "Срок использования (месяцев)";
                worksheet.Cell("I1").Value = "Стоимость";
                worksheet.Cell("J1").Value = "Дата последнего обслуживания";
                worksheet.Cell("K1").Value = "Срок годности";
                
                // Устанавливаем ширину колонок
                worksheet.Columns().AdjustToContents();
                
                // Сохраняем в поток
                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    
                    return File(
                        content,
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        "Шаблон_импорта_имущества.xlsx");
                }
            }
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ExportData([FromBody] ExportRequest request)
        {
            try
            {
                var columnMappings = new Dictionary<string, string>
                {
                    ["name"] = "Название",
                    ["type"] = "Тип имущества",
                    ["location"] = "Размещение",
                    ["user"] = "Назначено",
                    ["inventory"] = "Инвентарный номер",
                    ["cost"] = "Стоимость",
                    ["balanceDate"] = "Дата баланса",
                    ["usagePeriod"] = "Срок использования",
                    ["maintenance"] = "Последнее обслуживание",
                    ["expiry"] = "Срок годности",
                    ["status"] = "Статус проверки"
                };

                // Создаем CSV содержимое
                var csvContent = new StringBuilder();
                
                // Заголовки
                var headers = request.Columns
                    .Where(c => columnMappings.ContainsKey(c))
                    .Select(c => columnMappings[c]);
                
                csvContent.AppendLine(string.Join(";", headers));

                // Данные
                foreach (var item in request.Data)
                {
                    var values = request.Columns
                        .Where(c => columnMappings.ContainsKey(c))
                        .Select(c => 
                        {
                            var value = GetPropertyValue(item, c) ?? "";
                            // Экранирование для CSV
                            return $"\"{value.Replace("\"", "\"\"")}\"";
                        });
                    
                    csvContent.AppendLine(string.Join(";", values));
                }

                // Конвертируем в байты
                var data = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csvContent.ToString())).ToArray();
                var stream = new MemoryStream(data);
                
                var fileName = $"{request.FileName}.csv";
                return File(stream, "text/csv; charset=utf-8", fileName);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        private string GetPropertyValue(object obj, string propertyName)
        {
            try
            {
                if (obj is Dictionary<string, string> dict)
                {
                    return dict.ContainsKey(propertyName) ? dict[propertyName] : "";
                }
                
                var property = obj.GetType().GetProperty(propertyName);
                return property?.GetValue(obj)?.ToString() ?? "";
            }
            catch
            {
                return "";
            }
        }

        public class ExportRequest
        {
            public List<Dictionary<string, string>> Data { get; set; }
            public List<string> Columns { get; set; }
            public string Format { get; set; }
            public string FileName { get; set; }
        }
        
        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Import(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                ModelState.AddModelError("", "Выберите файл для загрузки");
                return View();
            }

            // Проверка размера файла
            if (file.Length > 10 * 1024 * 1024) // 10MB
            {
                ModelState.AddModelError("", "Файл слишком большой. Максимальный размер: 10MB");
                return View();
            }

            var extension = Path.GetExtension(file.FileName).ToLower();
            if (extension != ".xlsx" && extension != ".xls")
            {
                ModelState.AddModelError("", "Пожалуйста, выберите файл Excel (.xlsx или .xls)");
                return View();
            }

            var importedCount = 0;
            var errors = new List<string>();

            try
            {
                Console.WriteLine($"Начало импорта файла: {file.FileName}, размер: {file.Length} байт - PropertyController.cs:857");

                // Предзагрузка справочников для производительности
                var propertyTypes = await _context.PropertyTypes.ToDictionaryAsync(pt => pt.Name, pt => pt.Id);
                var locations = await _context.Locations.ToDictionaryAsync(l => l.Name, l => l.Id);
                var users = await _context.Users.Where(u => u.IsActive).ToDictionaryAsync(u => u.Name, u => u.Id);

                Console.WriteLine($"Загружено типов имущества: {propertyTypes.Count} - PropertyController.cs:864");
                Console.WriteLine($"Загружено локаций: {locations.Count} - PropertyController.cs:865");
                Console.WriteLine($"Загружено пользователей: {users.Count} - PropertyController.cs:866");

                using (var stream = new MemoryStream())
                {
                    await file.CopyToAsync(stream);
                    Console.WriteLine("Файл скопирован в MemoryStream - PropertyController.cs:871");

                    using (var workbook = new XLWorkbook(stream))
                    {
                        var worksheet = workbook.Worksheet(1);
                        var rows = worksheet.RowsUsed().Skip(1).ToList(); // Пропускаем заголовок

                        Console.WriteLine($"Найдено строк в файле (без заголовка): {rows.Count} - PropertyController.cs:878");

                        // Ограничение количества строк
                        if (rows.Count > 1000)
                        {
                            rows = rows.Take(1000).ToList();
                            errors.Add("Обработаны только первые 1000 строк файла");
                            Console.WriteLine("Применено ограничение в 1000 строк - PropertyController.cs:885");
                        }

                        var propertiesToAdd = new List<Property>();
                        
                        // Получаем существующие инвентарные номера
                        var inventoryNumbersInFile = rows.Select(r => r.Cell(3).Value.ToString().Trim()).ToList();
                        var existingInventoryNumbers = await _context.Properties
                            .Where(p => inventoryNumbersInFile.Contains(p.InventoryNumber))
                            .Select(p => p.InventoryNumber)
                            .ToListAsync();

                        Console.WriteLine($"Найдено существующих инвентарных номеров: {existingInventoryNumbers.Count} - PropertyController.cs:897");

                        using (var transaction = await _context.Database.BeginTransactionAsync())
                        {
                            try
                            {
                                Console.WriteLine("Начало транзакции - PropertyController.cs:903");

                                foreach (var row in rows)
                                {
                                    try
                                    {
                                        var rowNumber = row.RowNumber();
                                        Console.WriteLine($"Обработка строки {rowNumber} - PropertyController.cs:910");

                                        var validationResult = ValidatePropertyRow(row, rowNumber);
                                        if (!validationResult.isValid)
                                        {
                                            Console.WriteLine($"Строка {rowNumber} не прошла валидацию: {validationResult.errors} - PropertyController.cs:915");
                                            errors.AddRange(validationResult.errors);
                                            continue;
                                        }

                                        var inventoryNumber = row.Cell(3).Value.ToString().Trim();
                                        Console.WriteLine($"Проверка инвентарного номера: {inventoryNumber} - PropertyController.cs:921");

                                        // Проверка дубликатов
                                        if (existingInventoryNumbers.Contains(inventoryNumber) ||
                                            propertiesToAdd.Any(p => p.InventoryNumber == inventoryNumber))
                                        {
                                            var errorMsg = $"Строка {rowNumber}: Имущество с инвентарным номером {inventoryNumber} уже существует";
                                            Console.WriteLine(errorMsg + "");
                                            errors.Add(errorMsg);
                                            continue;
                                        }

                                        // Создание property
                                        var property = CreatePropertyFromRow(row, propertyTypes, locations, users);
                                        Console.WriteLine($"Создано имущество: {property.Name}, InventoryNumber: {property.InventoryNumber} - PropertyController.cs:935");

                                        // Проверяем, что тип имущества и размерование существуют
                                        if (property.PropertyTypeId == 0)
                                        {
                                            var propertyTypeName = row.Cell(4).Value.ToString();
                                            var errorMsg = $"Строка {rowNumber}: Тип имущества '{propertyTypeName}' не найден";
                                            Console.WriteLine(errorMsg + "");
                                            errors.Add(errorMsg);
                                            continue;
                                        }

                                        if (property.LocationId == 0)
                                        {
                                            var locationName = row.Cell(5).Value.ToString();
                                            var errorMsg = $"Строка {rowNumber}: Размещение '{locationName}' не найдено";
                                            Console.WriteLine(errorMsg + "");
                                            errors.Add(errorMsg);
                                            continue;
                                        }

                                        Console.WriteLine($"PropertyTypeId: {property.PropertyTypeId}, LocationId: {property.LocationId} - PropertyController.cs:956");

                                        propertiesToAdd.Add(property);
                                        Console.WriteLine($"Имущество добавлено в список для сохранения. Всего в списке: {propertiesToAdd.Count} - PropertyController.cs:959");

                                        // Пакетное сохранение каждые 100 записей
                                        if (propertiesToAdd.Count >= 100)
                                        {
                                            Console.WriteLine($"Пакетное сохранение {propertiesToAdd.Count} записей - PropertyController.cs:964");
                                            _context.Properties.AddRange(propertiesToAdd);
                                            var savedCount = await _context.SaveChangesAsync();
                                            Console.WriteLine($"Сохранено {savedCount} записей в базу данных - PropertyController.cs:967");
                                            importedCount += propertiesToAdd.Count;
                                            propertiesToAdd.Clear();
                                            Console.WriteLine("Список propertiesToAdd очищен - PropertyController.cs:970");
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        var errorMsg = $"Строка {row.RowNumber()}: Ошибка импорта - {ex.Message}";
                                        Console.WriteLine(errorMsg + "");
                                        Console.WriteLine($"StackTrace: {ex.StackTrace} - PropertyController.cs:977");
                                        errors.Add(errorMsg);
                                    }
                                }

                                // Сохранение оставшихся записей
                                if (propertiesToAdd.Any())
                                {
                                    Console.WriteLine($"Сохранение оставшихся {propertiesToAdd.Count} записей - PropertyController.cs:985");
                                    _context.Properties.AddRange(propertiesToAdd);
                                    var savedCount = await _context.SaveChangesAsync();
                                    Console.WriteLine($"Сохранено {savedCount} оставшихся записей - PropertyController.cs:988");
                                    importedCount += propertiesToAdd.Count;
                                }

                                Console.WriteLine($"Подтверждение транзакции. Импортировано всего: {importedCount} - PropertyController.cs:992");
                                await transaction.CommitAsync();
                                Console.WriteLine("Транзакция подтверждена - PropertyController.cs:994");

                                // Формируем сообщение о результате импорта
                                var message = $"Успешно импортировано {importedCount} записей.";
                                if (errors.Any())
                                {
                                    message += $" Ошибок: {errors.Count}.";
                                    TempData["ImportErrors"] = errors.Take(50).ToList(); // Ограничиваем вывод ошибок
                                    Console.WriteLine($"Есть ошибки импорта: {errors.Count} - PropertyController.cs:1002");
                                }

                                TempData["Message"] = message;
                                Console.WriteLine($"Импорт завершен. Сообщение: {message} - PropertyController.cs:1006");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Ошибка в транзакции: {ex.Message} - PropertyController.cs:1010");
                                Console.WriteLine($"StackTrace транзакции: {ex.StackTrace} - PropertyController.cs:1011");
                                await transaction.RollbackAsync();
                                Console.WriteLine("Транзакция откатана - PropertyController.cs:1013");
                                ModelState.AddModelError("", "Ошибка при сохранении данных: " + ex.Message);
                                return View();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Общая ошибка импорта: {ex.Message} - PropertyController.cs:1023");
                Console.WriteLine($"StackTrace общей ошибки: {ex.StackTrace} - PropertyController.cs:1024");
                ModelState.AddModelError("", "Ошибка при обработке файла: " + ex.Message);
                return View();
            }

            Console.WriteLine("Перенаправление на Index - PropertyController.cs:1029");
            return RedirectToAction("Index");
        }

        private (bool isValid, List<string> errors) ValidatePropertyRow(IXLRow row, int rowNumber)
        {
            var errors = new List<string>();

            var name = row.Cell(1).Value.ToString();
            var inventoryNumber = row.Cell(3).Value.ToString();

            if (string.IsNullOrWhiteSpace(name))
                errors.Add($"Строка {rowNumber}: Название обязательно");

            if (string.IsNullOrWhiteSpace(inventoryNumber))
                errors.Add($"Строка {rowNumber}: Инвентарный номер обязателен");

            if (name?.Length > 200)
                errors.Add($"Строка {rowNumber}: Название слишком длинное (макс. 200 символов)");

            if (inventoryNumber?.Length > 100)
                errors.Add($"Строка {rowNumber}: Инвентарный номер слишком длинный (макс. 100 символов)");

            return (isValid: errors.Count == 0, errors: errors);
        }

        private Property CreatePropertyFromRow(IXLRow row,
            Dictionary<string, int> propertyTypes,
            Dictionary<string, int> locations,
            Dictionary<string, int> users)
        {
            var name = row.Cell(1).Value.ToString();
            var description = row.Cell(2).Value.ToString();
            var inventoryNumber = row.Cell(3).Value.ToString();
            var propertyTypeName = row.Cell(4).Value.ToString();
            var locationName = row.Cell(5).Value.ToString();
            var assignedUserName = row.Cell(6).Value.ToString();

            // Получаем ID из предзагруженных словарей
            propertyTypes.TryGetValue(propertyTypeName, out var propertyTypeId);
            locations.TryGetValue(locationName, out var locationId);
            users.TryGetValue(assignedUserName, out var assignedUserId);

            // Обработка дат и чисел
            DateTime? balanceDate = null;
            var balanceDateStr = row.Cell(7).Value.ToString();
            if (!string.IsNullOrWhiteSpace(balanceDateStr) && DateTime.TryParse(balanceDateStr, out var bd))
            {
                balanceDate = bd.ToUniversalTime();
            }

            int? usagePeriod = null;
            var usagePeriodStr = row.Cell(8).Value.ToString();
            if (!string.IsNullOrWhiteSpace(usagePeriodStr) && int.TryParse(usagePeriodStr, out var up))
            {
                usagePeriod = up;
            }

            decimal? cost = null;
            var costStr = row.Cell(9).Value.ToString();
            if (!string.IsNullOrWhiteSpace(costStr) && decimal.TryParse(costStr, out var c))
            {
                cost = c;
            }

            DateTime? lastMaintenanceDate = null;
            var lastMaintenanceDateStr = row.Cell(10).Value.ToString();
            if (!string.IsNullOrWhiteSpace(lastMaintenanceDateStr) && DateTime.TryParse(lastMaintenanceDateStr, out var lmd))
            {
                lastMaintenanceDate = lmd.ToUniversalTime();
            }

            DateTime? expiryDate = null;
            var expiryDateStr = row.Cell(11).Value.ToString();
            if (!string.IsNullOrWhiteSpace(expiryDateStr) && DateTime.TryParse(expiryDateStr, out var ed))
            {
                expiryDate = ed.ToUniversalTime();
            }

            return new Property
            {
                Name = name.Trim(),
                Description = description?.Trim(),
                InventoryNumber = inventoryNumber.Trim(),
                PropertyTypeId = propertyTypeId,
                LocationId = locationId,
                AssignedUserId = assignedUserId == 0 ? null : assignedUserId,
                BalanceDate = balanceDate,
                UsagePeriod = usagePeriod,
                Cost = cost,
                LastMaintenanceDate = lastMaintenanceDate,
                ExpiryDate = expiryDate,
                QRCode = GenerateQRCode(inventoryNumber),
                Barcode = GenerateBarcode(inventoryNumber)
            };
        }        
        public async Task<IActionResult> PrintQRCodes(int? propertyTypeId, int? locationId, int? userId, int? tagId)
        {
            var properties = _context.Properties
                .Include(p => p.PropertyType)
                .Include(p => p.Location)
                .Include(p => p.AssignedUser)
                .AsQueryable();

            if (propertyTypeId.HasValue)
            {
                properties = properties.Where(p => p.PropertyTypeId == propertyTypeId.Value);
            }

            if (locationId.HasValue)
            {
                properties = properties.Where(p => p.LocationId == locationId.Value);
            }

            if (userId.HasValue)
            {
                properties = properties.Where(p => p.AssignedUserId == userId.Value);
            }

            var propertyList = await properties.ToListAsync();
            
            // Отладка: проверяем загрузку данных для печати QR кодов
            Console.WriteLine($"Печать QR кодов: Загружено {propertyList.Count} имуществ - PropertyController.cs:1151");
            foreach (var prop in propertyList.Take(5))
            {
                Console.WriteLine($"Печать QR кодов: {prop.Name}, Локация: {prop.Location?.Name}, Тип: {prop.PropertyType?.Name} - PropertyController.cs:1154");
            }
            
            // Получаем список активных бирок для выбора
            ViewBag.Tags = _context.Tags.Where(t => t.IsActive).ToList();
            ViewBag.SelectedTagId = tagId;
            
            // Если выбрана бирка, устанавливаем её размеры
            if (tagId.HasValue)
            {
                var selectedTag = _context.Tags.FirstOrDefault(t => t.Id == tagId.Value);
                if (selectedTag != null)
                {
                    ViewBag.TagWidth = selectedTag.Width + "mm";
                    ViewBag.TagHeight = selectedTag.Height + "mm";
                }
            }
            
            return View(propertyList);
        }
        
        public async Task<IActionResult> PrintBarcodes(int? propertyTypeId, int? locationId, int? userId, int? tagId)
        {
            var properties = _context.Properties
                .Include(p => p.PropertyType)
                .Include(p => p.Location)
                .Include(p => p.AssignedUser)
                .AsQueryable();

            // Если выбрана бирка, устанавливаем её размеры
            if (tagId.HasValue)
            {
                var selectedTag = _context.Tags.FirstOrDefault(t => t.Id == tagId.Value);
                if (selectedTag != null)
                {
                    ViewBag.TagWidth = selectedTag.Width + "mm";
                    ViewBag.TagHeight = selectedTag.Height + "mm";
                }
            }

            if (propertyTypeId.HasValue)
            {
                properties = properties.Where(p => p.PropertyTypeId == propertyTypeId.Value);
            }

            if (locationId.HasValue)
            {
                properties = properties.Where(p => p.LocationId == locationId.Value);
            }

            if (userId.HasValue)
            {
                properties = properties.Where(p => p.AssignedUserId == userId.Value);
            }

            var propertyList = await properties.ToListAsync();
            
            // Отладка: проверяем загрузку данных для печати штрих-кодов
            Console.WriteLine($"Печать штрихкодов: Загружено {propertyList.Count} имуществ - PropertyController.cs:1212");
            foreach (var prop in propertyList.Take(5))
            {
                Console.WriteLine($"Печать штрихкодов: {prop.Name}, Локация: {prop.Location?.Name}, Тип: {prop.PropertyType?.Name} - PropertyController.cs:1215");
            }
            
            // Получаем список активных бирок для выбора
            ViewBag.Tags = _context.Tags.Where(t => t.IsActive).ToList();
            ViewBag.SelectedTagId = tagId;
            
            return View(propertyList);
        }
        
        public async Task<IActionResult> ExportBarcodesToDocx(int? propertyTypeId, int? locationId, int? userId)
        {
            var properties = _context.Properties
                .Include(p => p.PropertyType)
                .Include(p => p.Location)
                .Include(p => p.AssignedUser)
                .AsQueryable();

            if (propertyTypeId.HasValue)
            {
                properties = properties.Where(p => p.PropertyTypeId == propertyTypeId.Value);
            }

            if (locationId.HasValue)
            {
                properties = properties.Where(p => p.LocationId == locationId.Value);
            }

            if (userId.HasValue)
            {
                properties = properties.Where(p => p.AssignedUserId == userId.Value);
            }

            var propertyList = await properties.ToListAsync();
            
            // Отладка: проверяем загрузку данных для экспорта бирок
            Console.WriteLine($"Экспорт бирок: Загружено {propertyList.Count} имуществ - PropertyController.cs:1251");
            foreach (var prop in propertyList.Take(5))
            {
                Console.WriteLine($"Экспорт бирок: {prop.Name}, Локация: {prop.Location?.Name}, Тип: {prop.PropertyType?.Name} - PropertyController.cs:1254");
            }
            
            // Генерируем документ
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var documentBytes = _barcodeDocxService.GenerateBarcodeDocument(propertyList, baseUrl);
            
            // Возвращаем файл
            return File(documentBytes, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", "barcodes.docx");
        }
    }
}