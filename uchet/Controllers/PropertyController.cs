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

            var propertyList = await properties.ToListAsync();
            Console.WriteLine($"Количество имущества: {propertyList.Count} - PropertyController.cs:64");
            foreach (var prop in propertyList)
            {
                Console.WriteLine($"Имущество: {prop.Name}, Срок использования: {prop.UsagePeriod} - PropertyController.cs:67");
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
                
            if (property == null)
            {
                return NotFound();
            }
            
            Console.WriteLine($"Детали имущества: {property.Name}, Срок использования: {property.UsagePeriod} - PropertyController.cs:86");
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
            Console.WriteLine($"Попытка создания имущества: {propertyDto.Name} - PropertyController.cs:130");
            Console.WriteLine($"PropertyTypeId: {propertyDto.PropertyTypeId} - PropertyController.cs:131");
            Console.WriteLine($"LocationId: {propertyDto.LocationId} - PropertyController.cs:132");
            Console.WriteLine($"InventoryNumber: {propertyDto.InventoryNumber} - PropertyController.cs:133");
            
            // Проверяем валидацию модели
            Console.WriteLine($"ModelState.IsValid: {ModelState.IsValid} - PropertyController.cs:136");
            
            if (ModelState.IsValid)
            {
                try
                {
                    Console.WriteLine("Модель валидна, начинаем сохранение... - PropertyController.cs:142");
                    
                    // Создаем новый экземпляр Property на основе данных из DTO
                    var property = new Property
                    {
                        Name = propertyDto.Name,
                        Description = propertyDto.Description,
                        LocationId = propertyDto.LocationId,
                        PropertyTypeId = propertyDto.PropertyTypeId,
                        AssignedUserId = propertyDto.AssignedUserId,
                        InventoryNumber = propertyDto.InventoryNumber,
                        BalanceDate = propertyDto.BalanceDate,
                        UsagePeriod = propertyDto.UsagePeriod,
                        Cost = propertyDto.Cost,
                        LastMaintenanceDate = propertyDto.LastMaintenanceDate,
                        ExpiryDate = propertyDto.ExpiryDate,
                        QRCode = GenerateQRCode(propertyDto.InventoryNumber), // Генерируем QR код на основе инвентарного номера
                        Barcode = GenerateBarcode(propertyDto.InventoryNumber) // Генерируем штрих-код на основе инвентарного номера
                    };
                    
                    // Добавляем имущество в контекст
                    _context.Properties.Add(property);
                    await _context.SaveChangesAsync();
                    
                    Console.WriteLine($"Имущество сохранено с Id: {property.Id} - PropertyController.cs:166");
                    
                    Console.WriteLine("Имущество успешно обновлено с QR и штрихкодом - PropertyController.cs:168");
                    
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    // Логируем ошибку
                    Console.WriteLine($"Ошибка при добавлении имущества: {ex} - PropertyController.cs:175");
                    ModelState.AddModelError("", "Произошла ошибка при добавлении имущества: " + ex.Message);
                }
            }
            else
            {
                Console.WriteLine("Модель не валидна: - PropertyController.cs:181");
                foreach (var key in ModelState.Keys)
                {
                    var state = ModelState[key];
                    if (state.Errors.Count > 0)
                    {
                        Console.WriteLine("Поле - PropertyController.cs:187" + key + ": " + string.Join(", ", state.Errors.Select(e => e.ErrorMessage)) + "");
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
                BalanceDate = propertyDto.BalanceDate,
                UsagePeriod = propertyDto.UsagePeriod,
                Cost = propertyDto.Cost,
                LastMaintenanceDate = propertyDto.LastMaintenanceDate,
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
                Console.WriteLine($"Property with id {id} not found - PropertyController.cs:394");
                return NotFound();
            }
            
            // Проверяем, есть ли инвентарный номер
            if (string.IsNullOrEmpty(property.InventoryNumber))
            {
                Console.WriteLine($"Inventory number is null or empty for property id {id} - PropertyController.cs:401");
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
                    Console.WriteLine($"Successfully generated barcode for property id {id} with inventory number {property.InventoryNumber} - PropertyController.cs:440");
                    return File(stream.ToArray(), "image/png");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating barcode for property id {id}: {ex.Message} - PropertyController.cs:446");
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
            using (var workbook = new ClosedXML.Excel.XLWorkbook())
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
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Import(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                ModelState.AddModelError("", "Выберите файл для загрузки");
                return View();
            }
            
            // Проверяем расширение файла
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
                using (var stream = new MemoryStream())
                {
                    await file.CopyToAsync(stream);
                    
                    using (var workbook = new XLWorkbook(stream))
                    {
                        var worksheet = workbook.Worksheet(1);
                        var rows = worksheet.RowsUsed();
                        
                        // Пропускаем заголовок
                        foreach (var row in rows.Skip(1))
                        {
                            try
                            {
                                var name = row.Cell(1).Value.ToString();
                                var description = row.Cell(2).Value.ToString();
                                var inventoryNumber = row.Cell(3).Value.ToString();
                                
                                // Проверяем обязательные поля
                                if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(inventoryNumber))
                                {
                                    errors.Add($"Строка {row.RowNumber()}: Название и инвентарный номер обязательны");
                                    continue;
                                }
                                
                                // Проверяем, существует ли уже имущество с таким инвентарным номером
                                if (_context.Properties.Any(p => p.InventoryNumber == inventoryNumber))
                                {
                                    errors.Add($"Строка {row.RowNumber()}: Имущество с инвентарным номером {inventoryNumber} уже существует");
                                    continue;
                                }
                                
                                // Получаем тип имущества
                                var propertyTypeName = row.Cell(4).Value.ToString();
                                var propertyType = await _context.PropertyTypes.FirstOrDefaultAsync(pt => pt.Name == propertyTypeName);
                                
                                // Получаем размещение
                                var locationName = row.Cell(5).Value.ToString();
                                var location = await _context.Locations.FirstOrDefaultAsync(l => l.Name == locationName);
                                
                                // Получаем назначенного пользователя (если указан)
                                User assignedUser = null;
                                var assignedUserName = row.Cell(6).Value.ToString();
                                if (!string.IsNullOrWhiteSpace(assignedUserName))
                                {
                                    assignedUser = await _context.Users.FirstOrDefaultAsync(u => u.Name == assignedUserName);
                                }
                                
                                // Получаем дату баланса (если указана)
                                DateTime? balanceDate = null;
                                var balanceDateStr = row.Cell(7).Value.ToString();
                                if (!string.IsNullOrWhiteSpace(balanceDateStr) && DateTime.TryParse(balanceDateStr, out var bd))
                                {
                                    balanceDate = bd;
                                }
                                
                                // Получаем срок использования (если указан)
                                int? usagePeriod = null;
                                var usagePeriodStr = row.Cell(8).Value.ToString();
                                if (!string.IsNullOrWhiteSpace(usagePeriodStr) && int.TryParse(usagePeriodStr, out var up))
                                {
                                    usagePeriod = up;
                                }
                                
                                // Получаем стоимость (если указана)
                                decimal? cost = null;
                                var costStr = row.Cell(9).Value.ToString();
                                if (!string.IsNullOrWhiteSpace(costStr) && decimal.TryParse(costStr, out var c))
                                {
                                    cost = c;
                                }
                                
                                // Получаем дату последнего обслуживания (если указана)
                                DateTime? lastMaintenanceDate = null;
                                var lastMaintenanceDateStr = row.Cell(10).Value.ToString();
                                if (!string.IsNullOrWhiteSpace(lastMaintenanceDateStr) && DateTime.TryParse(lastMaintenanceDateStr, out var lmd))
                                {
                                    lastMaintenanceDate = lmd;
                                }
                                
                                // Получаем срок годности (если указан)
                                DateTime? expiryDate = null;
                                var expiryDateStr = row.Cell(11).Value.ToString();
                                if (!string.IsNullOrWhiteSpace(expiryDateStr) && DateTime.TryParse(expiryDateStr, out var ed))
                                {
                                    expiryDate = ed;
                                }
                                
                                // Создаем новое имущество
                                var property = new Property
                                {
                                    Name = name,
                                    Description = description,
                                    InventoryNumber = inventoryNumber,
                                    PropertyTypeId = propertyType?.Id ?? 0,
                                    LocationId = location?.Id ?? 0,
                                    AssignedUserId = assignedUser?.Id,
                                    BalanceDate = balanceDate,
                                    UsagePeriod = usagePeriod,
                                    Cost = cost,
                                    LastMaintenanceDate = lastMaintenanceDate,
                                    ExpiryDate = expiryDate,
                                    QRCode = GenerateQRCode(inventoryNumber),
                                    Barcode = GenerateBarcode(inventoryNumber)
                                };
                                
                                // Проверяем, что тип имущества и размещение существуют
                                if (property.PropertyTypeId == 0)
                                {
                                    errors.Add($"Строка {row.RowNumber()}: Тип имущества '{propertyTypeName}' не найден");
                                    continue;
                                }
                                
                                if (property.LocationId == 0)
                                {
                                    errors.Add($"Строка {row.RowNumber()}: Размещение '{locationName}' не найдено");
                                    continue;
                                }
                                
                                _context.Properties.Add(property);
                                importedCount++;
                            }
                            catch (Exception ex)
                            {
                                errors.Add($"Строка {row.RowNumber()}: Ошибка импорта - {ex.Message}");
                            }
                        }
                    }
                }
                
                // Сохраняем изменения в базе данных
                await _context.SaveChangesAsync();
                
                // Формируем сообщение о результате импорта
                var message = $"Успешно импортировано {importedCount} записей.";
                if (errors.Any())
                {
                    message += $" Ошибок: {errors.Count}.";
                    TempData["ImportErrors"] = errors;
                }
                
                TempData["Message"] = message;
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Ошибка при импорте файла: " + ex.Message);
                return View();
            }
            
            return RedirectToAction("Index");
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
            
            // Получаем список активных бирок для выбора
            ViewBag.Tags = _context.Tags.Where(t => t.IsActive).ToList();
            ViewBag.SelectedTagId = tagId;
            
            return View(propertyList);
        }
    }
}