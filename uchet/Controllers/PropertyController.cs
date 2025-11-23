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

namespace uchet.Controllers
{
    [Authorize]
    public class PropertyController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public PropertyController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        public async Task<IActionResult> Index(int? propertyTypeId, int? locationId, int? userId)
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

            var propertyTypes = await _context.PropertyTypes.ToListAsync();
            var locations = await _context.Locations.ToListAsync();
            var users = await _context.Users.Where(u => u.IsActive).ToListAsync();

            ViewBag.PropertyTypes = new SelectList(propertyTypes, "Id", "Name", propertyTypeId);
            ViewBag.Locations = new SelectList(locations, "Id", "Name", locationId);
            ViewBag.Users = new SelectList(users, "Id", "Name", userId);

            return View(await properties.ToListAsync());
        }

        public async Task<IActionResult> Details(int id)
        {
            var property = await _context.Properties
                .Include(p => p.PropertyType)
                .Include(p => p.Location)
                .Include(p => p.AssignedUser)
                .Include(p => p.PropertyFiles)
                .FirstOrDefaultAsync(p => p.Id == id);
                
            if (property == null)
            {
                return NotFound();
            }
            
            return View(property);
        }

        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Create()
        {
            var propertyTypes = await _context.PropertyTypes.ToListAsync();
            var locations = await _context.Locations.ToListAsync();
            var users = await _context.Users.Where(u => u.IsActive).ToListAsync();
            
            ViewBag.PropertyTypes = new SelectList(propertyTypes, "Id", "Name");
            ViewBag.Locations = new SelectList(locations, "Id", "Name");
            ViewBag.Users = new SelectList(users, "Id", "Name");
            
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Property property)
        {
            // Загружаем связанные данные для проверки валидации
            var propertyTypes = await _context.PropertyTypes.ToListAsync();
            var locations = await _context.Locations.ToListAsync();
            var users = await _context.Users.Where(u => u.IsActive).ToListAsync();
            
            ViewBag.PropertyTypes = new SelectList(propertyTypes, "Id", "Name");
            ViewBag.Locations = new SelectList(locations, "Id", "Name");
            ViewBag.Users = new SelectList(users, "Id", "Name");
            
            // Проверяем, что выбраны обязательные поля
            if (property.PropertyTypeId == 0)
            {
                ModelState.AddModelError("PropertyTypeId", "Пожалуйста, выберите тип имущества");
            }
            
            if (property.LocationId == 0)
            {
                ModelState.AddModelError("LocationId", "Пожалуйста, выберите размещение");
            }
            
            // Добавляем отладочную информацию
            Console.WriteLine($"Попытка создания имущества: {property.Name} - PropertyController.cs:117");
            Console.WriteLine($"PropertyTypeId: {property.PropertyTypeId} - PropertyController.cs:118");
            Console.WriteLine($"LocationId: {property.LocationId} - PropertyController.cs:119");
            Console.WriteLine($"InventoryNumber: {property.InventoryNumber} - PropertyController.cs:120");
            
            // Проверяем валидацию модели
            Console.WriteLine($"ModelState.IsValid: {ModelState.IsValid} - PropertyController.cs:123");
            
            if (ModelState.IsValid)
            {
                try
                {
                    Console.WriteLine("Модель валидна, начинаем сохранение... - PropertyController.cs:129");
                    
                    // Добавляем имущество в контекст
                    _context.Properties.Add(property);
                    await _context.SaveChangesAsync();
                    
                    Console.WriteLine($"Имущество сохранено с Id: {property.Id} - PropertyController.cs:135");
                    
                    // Генерация QR кода
                    property.QRCode = GenerateQRCode(property.Id);
                    
                    // Генерация штрих-кода
                    property.Barcode = GenerateBarcode(property.Id);
                    
                    Console.WriteLine($"Сгенерированы QR: {property.QRCode}, Barcode: {property.Barcode} - PropertyController.cs:143");
                    
                    // Обновляем имущество с QR кодом и штрих-кодом
                    _context.Update(property);
                    await _context.SaveChangesAsync();
                    
                    Console.WriteLine("Имущество успешно обновлено с QR и штрихкодом - PropertyController.cs:149");
                    
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    // Логируем ошибку
                    Console.WriteLine($"Ошибка при добавлении имущества: {ex} - PropertyController.cs:156");
                    ModelState.AddModelError("", "Произошла ошибка при добавлении имущества: " + ex.Message);
                }
            }
            else
            {
                Console.WriteLine("Модель не валидна: - PropertyController.cs:162");
                foreach (var key in ModelState.Keys)
                {
                    var state = ModelState[key];
                    if (state.Errors.Count > 0)
                    {
                        Console.WriteLine("Поле - PropertyController.cs:168" + key + ": " + string.Join(", ", state.Errors.Select(e => e.ErrorMessage)) + "");
                    }
                }
            }
            
            // Если валидация не пройдена или произошла ошибка, возвращаем представление с данными
            return View(property);
        }
        
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Edit(int id)
        {
            var property = await _context.Properties.FindAsync(id);
            if (property == null)
            {
                return NotFound();
            }
            
            var propertyTypes = await _context.PropertyTypes.ToListAsync();
            var locations = await _context.Locations.ToListAsync();
            var users = await _context.Users.Where(u => u.IsActive).ToListAsync();
            
            ViewBag.PropertyTypes = new SelectList(propertyTypes, "Id", "Name", property.PropertyTypeId);
            ViewBag.Locations = new SelectList(locations, "Id", "Name", property.LocationId);
            ViewBag.Users = new SelectList(users, "Id", "Name", property.AssignedUserId);
            
            return View(property);
        }
        
        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]
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
                    _context.Update(property);
                    await _context.SaveChangesAsync();
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
            
            return View(property);
        }
        
        [HttpPost, ActionName("Delete")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var property = await _context.Properties.FindAsync(id);
            if (property != null)
            {
                _context.Properties.Remove(property);
                await _context.SaveChangesAsync();
            }
            
            return RedirectToAction("Index");
        }
        
        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]
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
        
        private string GenerateQRCode(int propertyId)
        {
            // Здесь будет реализация генерации QR кода
            // Пока возвращаем заглушку
            return $"QR_{propertyId}";
        }
        
        private string GenerateBarcode(int propertyId)
        {
            // Здесь будет реализация генерации штрих-кода
            // Пока возвращаем заглушку
            return $"BAR_{propertyId}";
        }
        
        [Authorize(Roles = "Admin,Manager")]
        public IActionResult Import()
        {
            return View();
        }
        
        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Import(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                ModelState.AddModelError("", "Выберите файл для загрузки");
                return View();
            }
            
            // Здесь будет реализация импорта из Excel
            // Пока возвращаем заглушку
            TempData["Message"] = "Импорт из Excel будет реализован позже";
            return RedirectToAction("Index");
        }
    }
}