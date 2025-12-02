using System.Drawing;
using System.Drawing.Imaging;
using QRCoder;
using Microsoft.AspNetCore.Mvc;
using uchet.Data;
using uchet.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using ZXing;
using ZXing.Common;
using ClosedXML.Excel;
using uchet.Services;
using System.Runtime.Versioning;
using System.Text;
using System.Globalization;

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

        /// <summary>
        /// Массовое изменение статуса проверки имущества
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkUpdateCheckStatus([FromBody] BulkCheckStatusRequest request)
        {
            try
            {
                var properties = await _context.Properties
                    .Where(p => request.Ids.Contains(p.Id))
                    .ToListAsync();

                if (!properties.Any())
                {
                    return Json(new { success = false, message = "Записи не найдены" });
                }

                foreach (var property in properties)
                {
                    property.IsCheckedInLastInventory = request.IsChecked;
                    property.LastInventoryCheckDate = request.IsChecked ? DateTime.UtcNow : null;
                }

                _context.Properties.UpdateRange(properties);
                await _context.SaveChangesAsync();

                var statusText = request.IsChecked ? "проверено" : "не проверено";
                return Json(new { success = true, message = $"Статус проверки обновлен для {properties.Count} записей" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Ошибка при обновлении статуса: " + ex.Message });
            }
        }

        public class BulkCheckStatusRequest
        {
            public List<int> Ids { get; set; } = new List<int>();
            public bool IsChecked { get; set; }
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
            var propertyTypes = await _context.PropertyTypes.ToListAsync();
            var locations = await _context.Locations.ToListAsync();
            var users = await _context.Users.Where(u => u.IsActive).ToListAsync();
            
            ViewBag.PropertyTypes = new SelectList(propertyTypes, "Id", "Name");
            ViewBag.Locations = new SelectList(locations, "Id", "Name");
            ViewBag.Users = new SelectList(users, "Id", "Name");
            
            if (propertyDto.PropertyTypeId == 0)
            {
                ModelState.AddModelError("PropertyTypeId", "Пожалуйста, выберите тип имущества");
            }
            
            if (propertyDto.LocationId == 0)
            {
                ModelState.AddModelError("LocationId", "Пожалуйста, выберите размещение");
            }
            
            if (ModelState.IsValid)
            {
                try
                {
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
                        QRCode = GenerateQRCode(propertyDto.InventoryNumber),
                        Barcode = GenerateBarcode(propertyDto.InventoryNumber)
                    };
                    
                    _context.Properties.Add(property);
                    await _context.SaveChangesAsync();
                    
                    TempData["SuccessMessage"] = "Имущество успешно создано!";
                    return RedirectToAction("Details", new { id = property.Id });
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Произошла ошибка при создании имущества: " + GetUserFriendlyErrorMessage(ex));
                }
            }
            
            return View(propertyDto);
        }
        
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Edit(int id)
        {
            var property = await _context.Properties.FindAsync(id);
            if (property == null)
            {
                return NotFound();
            }

            var editDto = new EditPropertyDto
            {
                Id = property.Id,
                Name = property.Name,
                Description = property.Description,
                InventoryNumber = property.InventoryNumber,
                BalanceDate = property.BalanceDate,
                UsagePeriod = property.UsagePeriod,
                Cost = property.Cost,
                LastMaintenanceDate = property.LastMaintenanceDate,
                ExpiryDate = property.ExpiryDate,
                PropertyTypeId = property.PropertyTypeId,
                LocationId = property.LocationId,
                AssignedUserId = property.AssignedUserId
            };

            await LoadViewBagData(property.PropertyTypeId, property.LocationId, property.AssignedUserId);
            return View(editDto);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EditPropertyDto editDto)
        {
            if (id != editDto.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingProperty = await _context.Properties.FindAsync(id);
                    if (existingProperty == null)
                    {
                        return NotFound();
                    }

                    // Обновляем свойства
                    existingProperty.Name = editDto.Name;
                    existingProperty.Description = editDto.Description;
                    existingProperty.InventoryNumber = editDto.InventoryNumber;
                    existingProperty.BalanceDate = editDto.BalanceDate?.ToUniversalTime();
                    existingProperty.UsagePeriod = editDto.UsagePeriod;
                    existingProperty.Cost = editDto.Cost;
                    existingProperty.LastMaintenanceDate = editDto.LastMaintenanceDate?.ToUniversalTime();
                    existingProperty.ExpiryDate = editDto.ExpiryDate?.ToUniversalTime();
                    existingProperty.PropertyTypeId = editDto.PropertyTypeId;
                    existingProperty.LocationId = editDto.LocationId;
                    existingProperty.AssignedUserId = editDto.AssignedUserId;

                    // Обновляем QR и штрих-коды
                    existingProperty.QRCode = GenerateQRCode(editDto.InventoryNumber);
                    existingProperty.Barcode = GenerateBarcode(editDto.InventoryNumber);

                    _context.Properties.Update(existingProperty);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Изменения успешно сохранены!";
                    return RedirectToAction("Details", new { id = editDto.Id });
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    if (!PropertyExists(editDto.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        ModelState.AddModelError("", "Запись была изменена другим пользователем. Пожалуйста, обновите страницу и попробуйте снова.");
                        Console.WriteLine($"DbUpdateConcurrencyException: {ex.Message} - PropertyController.cs:315");
                    }
                }
                catch (DbUpdateException ex)
                {
                    ModelState.AddModelError("", $"Ошибка базы данных при сохранении: {GetUserFriendlyErrorMessage(ex)}");
                    Console.WriteLine($"DbUpdateException: {ex.Message} - PropertyController.cs:321");
                    if (ex.InnerException != null)
                    {
                        Console.WriteLine($"Inner Exception: {ex.InnerException.Message} - PropertyController.cs:324");
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Ошибка при сохранении: {GetUserFriendlyErrorMessage(ex)}");
                    Console.WriteLine($"Exception: {ex.Message} - PropertyController.cs:330");
                    if (ex.InnerException != null)
                    {
                        Console.WriteLine($"Inner Exception: {ex.InnerException.Message} - PropertyController.cs:333");
                    }
                }
            }

            await LoadViewBagData(editDto.PropertyTypeId, editDto.LocationId, editDto.AssignedUserId);
            return View(editDto);
        }

        private string GetUserFriendlyErrorMessage(Exception ex)
        {
            // Обработка специфических ошибок базы данных
            if (ex is DbUpdateException dbUpdateEx)
            {
                var innerEx = dbUpdateEx.InnerException;
                
                // Проверка на нарушение уникальности инвентарного номера
                if (innerEx != null && innerEx.Message.Contains("IX_Properties_InventoryNumber"))
                {
                    return "Имущество с таким инвентарным номером уже существует. Пожалуйста, используйте другой номер.";
                }
                
                // Проверка на нарушение внешних ключей
                if (innerEx != null && (innerEx.Message.Contains("FOREIGN KEY") || innerEx.Message.Contains("REFERENCES")))
                {
                    return "Ошибка связанных данных. Убедитесь, что выбранные тип имущества и размещение существуют.";
                }
                
                // Проверка на ограничения целостности
                if (innerEx != null && innerEx.Message.Contains("constraint"))
                {
                    return "Нарушение ограничений базы данных. Проверьте введенные данные.";
                }
                
                return innerEx?.Message ?? dbUpdateEx.Message;
            }
            
            return ex.Message;
        }

        private async Task LoadViewBagData(int? propertyTypeId = null, int? locationId = null, int? assignedUserId = null)
        {
            var propertyTypes = await _context.PropertyTypes.ToListAsync();
            var locations = await _context.Locations.ToListAsync();
            var users = await _context.Users.Where(u => u.IsActive).ToListAsync();
            
            ViewBag.PropertyTypes = new SelectList(propertyTypes, "Id", "Name", propertyTypeId);
            ViewBag.Locations = new SelectList(locations, "Id", "Name", locationId);
            ViewBag.Users = new SelectList(users, "Id", "Name", assignedUserId);
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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var property = await _context.Properties.FindAsync(id);
            if (property != null)
            {
                try
                {
                    _context.Properties.Remove(property);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Имущество успешно удалено!";
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = $"Ошибка при удалении: {GetUserFriendlyErrorMessage(ex)}";
                }
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
            
            var propertyFilesPath = Path.Combine(_environment.WebRootPath, "property_files");
            if (!Directory.Exists(propertyFilesPath))
            {
                Directory.CreateDirectory(propertyFilesPath);
            }
            
            var fileName = $"{Guid.NewGuid()}_{file.FileName}";
            var filePath = Path.Combine(propertyFilesPath, fileName);
            
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }
            
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
            return $"QR_{inventoryNumber}";
        }
        
        private string GenerateBarcode(string inventoryNumber)
        {
            var cleanInventoryNumber = new string(inventoryNumber.Where(char.IsDigit).ToArray());
            return cleanInventoryNumber;
        }
        
        public IActionResult GenerateQRCodeImage(int id)
        {
            var property = _context.Properties.FirstOrDefault(p => p.Id == id);
            if (property == null)
            {
                return NotFound();
            }

            try
            {
                var qrGenerator = new QRCodeGenerator();
                var qrCodeData = qrGenerator.CreateQrCode(property.InventoryNumber ?? property.Id.ToString(), QRCodeGenerator.ECCLevel.Q);
                
                var qrCode = new PngByteQRCode(qrCodeData);
                var qrCodeImage = qrCode.GetGraphic(20);
                
                return File(qrCodeImage, "image/png");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка генерации QR кода: {ex.Message} - PropertyController.cs:498");
                return GenerateFallbackImage($"QR: {property.InventoryNumber}");
            }
        }
        

        // В PropertyController.cs замените метод GenerateBarcodeImage:

        public IActionResult GenerateBarcodeImage(int id)
        {
            var property = _context.Properties.FirstOrDefault(p => p.Id == id);
            if (property == null)
            {
                return NotFound();
            }

            try
            {
                // Используем SkiaSharp для кроссплатформенной генерации изображений
                var barcodeText = property.InventoryNumber ?? property.Id.ToString();
                var cleanBarcodeText = new string(barcodeText.Where(c => char.IsLetterOrDigit(c)).ToArray());
                
                if (string.IsNullOrEmpty(cleanBarcodeText))
                {
                    cleanBarcodeText = property.Id.ToString();
                }

                // Генерируем простой текстовый штрих-код
                return GenerateSimpleBarcodeImage(cleanBarcodeText);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка генерации штрихкода: {ex.Message} - PropertyController.cs:530");
                return GenerateFallbackImage($"Barcode: {property.InventoryNumber}");
            }
        }

        private IActionResult GenerateSimpleBarcodeImage(string text)
        {
            // Создаем простое изображение с текстом штрих-кода
            var width = 200;
            var height = 80;
            
            using (var bitmap = new System.Drawing.Bitmap(width, height))
            using (var graphics = System.Drawing.Graphics.FromImage(bitmap))
            {
                graphics.Clear(System.Drawing.Color.White);
                
                // Рисуем границу
                using (var pen = new System.Drawing.Pen(System.Drawing.Color.Black, 1))
                {
                    graphics.DrawRectangle(pen, 0, 0, width - 1, height - 1);
                }
                
                // Рисуем текст
                using (var font = new System.Drawing.Font("Arial", 10))
                using (var brush = new System.Drawing.SolidBrush(System.Drawing.Color.Black))
                using (var format = new System.Drawing.StringFormat()
                {
                    Alignment = System.Drawing.StringAlignment.Center,
                    LineAlignment = System.Drawing.StringAlignment.Center
                })
                {
                    graphics.DrawString(text, font, brush, new System.Drawing.RectangleF(0, 0, width, height), format);
                }
                
                // Добавляем полосы штрих-кода (упрощенная версия)
                var random = new Random(text.GetHashCode());
                for (int i = 10; i < width - 10; i += 2)
                {
                    if (random.Next(0, 2) == 1)
                    {
                        using (var pen = new System.Drawing.Pen(System.Drawing.Color.Black, 1))
                        {
                            graphics.DrawLine(pen, i, 20, i, height - 20);
                        }
                    }
                }
                
                using (var stream = new MemoryStream())
                {
                    bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                    return File(stream.ToArray(), "image/png");
                }
            }
        }

        private IActionResult GenerateFallbackImage(string text)
        {
            using (var bitmap = new Bitmap(300, 100))
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.Clear(Color.White);
                using (var font = new Font("Arial", 12))
                using (var brush = new SolidBrush(Color.Black))
                using (var format = new StringFormat()
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                })
                {
                    graphics.DrawString(text, font, brush, new RectangleF(0, 0, 300, 100), format);
                }
                
                using (var stream = new MemoryStream())
                {
                    bitmap.Save(stream, ImageFormat.Png);
                    return File(stream.ToArray(), "image/png");
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
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Имущество");
                
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
                
                worksheet.Columns().AdjustToContents();
                
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
                // 🔥 Вот сюда вставляем:
            Console.WriteLine(">>> ExportData вызван");

            // Чтобы увидеть, пришли ли данные:
            if (request == null)
            {
                Console.WriteLine(">>> Ошибка: request равен null");
                return BadRequest(new { error = "Данные не получены" });
            }

            Console.WriteLine($">>> Файл: {request.FileName}, Колонки: {string.Join(", ", request.Columns ?? new List<string>())}");
            Console.WriteLine($">>> Количество строк данных: {request.Data?.Count}");
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

                var csvContent = new StringBuilder();
                
                var headers = request.Columns
                    .Where(c => columnMappings.ContainsKey(c))
                    .Select(c => columnMappings[c]);
                
                csvContent.AppendLine(string.Join(";", headers));

                foreach (var item in request.Data)
                {
                    var values = request.Columns
                        .Where(c => columnMappings.ContainsKey(c))
                        .Select(c => 
                        {
                            var value = GetPropertyValue(item, c) ?? "";
                            return $"\"{value.Replace("\"", "\"\"")}\"";
                        });
                    
                    csvContent.AppendLine(string.Join(";", values));
                }

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
//---
        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Import(IFormFile file)
        {
            Console.WriteLine(">>> Import вызван");

            if (file == null)
            {
                Console.WriteLine(">>> Ошибка: файл не выбран");
                ModelState.AddModelError("", "Выберите файл для загрузки");
                return View();
            }

            Console.WriteLine($">>> Загружен файл: {file.FileName ?? "null"}, размер: {file.Length}");

            if (file.Length == 0)
            {
                Console.WriteLine(">>> Ошибка: файл пустой");
                ModelState.AddModelError("", "Файл пустой. Нечего импортировать.");
                return View();
            }

            if (file.Length > 10 * 1024 * 1024) // 10 МБ
            {
                Console.WriteLine(">>> Ошибка: файл слишком большой");
                ModelState.AddModelError("", "Файл слишком большой. Максимальный размер: 10 МБ");
                return View();
            }

            // --- Проверка расширения ---
            var fileName = file.FileName?.Trim();
            if (string.IsNullOrEmpty(fileName))
            {
                Console.WriteLine(">>> Ошибка: имя файла пустое или null");
                ModelState.AddModelError("", "Имя файла не указано или повреждено");
                return View();
            }

            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            Console.WriteLine($">>> Расширение файла: '{extension}'");

            if (extension != ".xlsx" && extension != ".xls")
            {
                Console.WriteLine(">>> Ошибка: неверное расширение файла");
                ModelState.AddModelError("", "Поддерживаются только файлы Excel: .xlsx или .xls");
                return View();
            }

            var importedCount = 0;
            var errors = new List<string>();

            try
            {
                Console.WriteLine(">>> Загружаем справочники из БД...");

                var propertyTypes = await _context.PropertyTypes
                    .ToDictionaryAsync(pt => pt.Name.Trim(), pt => pt.Id);
                Console.WriteLine($">>> Загружено PropertyTypes: {propertyTypes.Count}");

                var locations = await _context.Locations
                    .ToDictionaryAsync(l => l.Name.Trim(), l => l.Id);
                Console.WriteLine($">>> Загружено Locations: {locations.Count}");

                var users = await _context.Users
                    .Where(u => u.IsActive)
                    .ToDictionaryAsync(u => u.Name.Trim(), u => u.Id);
                Console.WriteLine($">>> Загружено активных Users: {users.Count}");

                using (var stream = new MemoryStream())
                {
                    Console.WriteLine(">>> Копируем файл в MemoryStream...");
                    await file.CopyToAsync(stream);
                    Console.WriteLine($">>> Копирование завершено. Размер в памяти: {stream.Length} байт");

                    if (stream.Length == 0)
                    {
                        Console.WriteLine(">>> Ошибка: MemoryStream пустой");
                        ModelState.AddModelError("", "Ошибка чтения файла: пустой поток");
                        return View();
                    }

                    stream.Position = 0; // Важно: сбросить позицию

                    using (var workbook = new XLWorkbook(stream))
                    {
                        Console.WriteLine($">>> Excel-файл открыт. Количество листов: {workbook.Worksheets.Count()}");

                        var worksheet = workbook.Worksheet(1);
                        if (worksheet == null)
                        {
                            Console.WriteLine(">>> Ошибка: не удалось получить первый лист");
                            errors.Add("Файл не содержит ни одного листа");
                            TempData["ImportErrors"] = errors;
                            TempData["Message"] = "Импорт не выполнен: нет данных";
                            return RedirectToAction("Index");
                        }

                        Console.WriteLine($">>> Активный лист: '{worksheet.Name}'");

                        var rows = worksheet.RowsUsed().ToList();
                        Console.WriteLine($">>> Всего строк с данными: {rows.Count}");

                        if (rows.Count < 2)
                        {
                            Console.WriteLine(">>> Ошибка: нет данных для импорта (только заголовки или пусто)");
                            errors.Add("Файл не содержит данных для импорта");
                            TempData["ImportErrors"] = errors;
                            TempData["Message"] = "Импорт не выполнен: файл пустой";
                            return RedirectToAction("Index");
                        }

                        // Пропускаем первую строку (заголовки)
                        var dataRows = rows.Skip(1).ToList();
                        Console.WriteLine($">>> Строк для импорта: {dataRows.Count}");

                        var inventoryNumbersInFile = dataRows
                            .Select(r => r.Cell(3).GetString()?.Trim())
                            .Where(s => !string.IsNullOrEmpty(s))
                            .ToList();

                        var existingInventoryNumbers = await _context.Properties
                            .Where(p => inventoryNumbersInFile.Contains(p.InventoryNumber))
                            .Select(p => p.InventoryNumber)
                            .ToListAsync();

                        var propertiesToAdd = new List<Property>();

                        using (var transaction = await _context.Database.BeginTransactionAsync())
                        {
                            try
                            {
                                foreach (var row in dataRows)
                                {
                                    var rowNumber = row.RowNumber();

                                    try
                                    {
                                        var name = row.Cell(1).GetString()?.Trim() ?? "";
                                        var inventoryNumber = row.Cell(3).GetString()?.Trim() ?? "";

                                        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(inventoryNumber))
                                        {
                                            errors.Add($"Строка {rowNumber}: Отсутствует название или инвентарный номер");
                                            continue;
                                        }

                                        if (existingInventoryNumbers.Contains(inventoryNumber) ||
                                            propertiesToAdd.Any(p => p.InventoryNumber == inventoryNumber))
                                        {
                                            errors.Add($"Строка {rowNumber}: Инвентарный номер '{inventoryNumber}' уже существует");
                                            continue;
                                        }

                                        var property = CreatePropertyFromRow(row, propertyTypes, locations, users);
                                        propertiesToAdd.Add(property);

                                        if (propertiesToAdd.Count >= 100)
                                        {
                                            _context.Properties.AddRange(propertiesToAdd);
                                            await _context.SaveChangesAsync();
                                            importedCount += propertiesToAdd.Count;
                                            propertiesToAdd.Clear();
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine($">>> Ошибка в строке {row.RowNumber()}: {ex.Message}");
                                        errors.Add($"Строка {row.RowNumber()}: {ex.Message}");
                                    }
                                }

                                if (propertiesToAdd.Any())
                                {
                                    // 🔍 Проверим, что все PropertyTypeId > 0
                                    var invalidTypeId = propertiesToAdd.FirstOrDefault(p => p.PropertyTypeId == 0);
                                    if (invalidTypeId != null)
                                    {
                                        throw new InvalidOperationException(
                                            $"Найдено имущество с PropertyTypeId = 0. Название: {invalidTypeId.Name}, Инвентарный: {invalidTypeId.InventoryNumber}");
                                    }

                                    // ✅ Обнуляем навигационные свойства (на всякий случай)
                                    foreach (var property in propertiesToAdd)
                                    {
                                        property.PropertyType = null;
                                        property.Location = null;
                                        property.AssignedUser = null;
                                    }

                                    // 🧪 Перед сохранением — посмотрим первые 3
                                    Console.WriteLine(">>> Перед сохранением:");
                                    foreach (var p in propertiesToAdd.Take(3))
                                    {
                                        Console.WriteLine($">>>   '{p.Name}', TypeId={p.PropertyTypeId}, LocId={p.LocationId}, Inv={p.InventoryNumber}");
                                    }

                                    _context.Properties.AddRange(propertiesToAdd);
                                    await _context.SaveChangesAsync(); // 🔥 Ошибка будет здесь
                                    importedCount += propertiesToAdd.Count;
                                }


                                await transaction.CommitAsync();

                                var message = $"Импорт завершён: {importedCount} записей добавлено.";
                                if (errors.Any())
                                {
                                    message += $" Ошибок: {errors.Count}.";
                                    TempData["ImportErrors"] = errors.Take(50).ToList();
                                }
                                TempData["Message"] = message;
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($">>> Ошибка при сохранении: {ex.Message}");
                                Console.WriteLine($"StackTrace: {ex.StackTrace}");

                                if (ex.InnerException != null)
                                {
                                    Console.WriteLine($">>> Внутренняя ошибка (InnerException): {ex.InnerException.Message}");
                                    Console.WriteLine($"Inner StackTrace: {ex.InnerException.StackTrace}");
                                }

                                await transaction.RollbackAsync();
                                ModelState.AddModelError("", "Ошибка при сохранении: " + ex.Message + 
                                    (ex.InnerException != null ? " | Детали: " + ex.InnerException.Message : ""));
                                return View();
                            }

                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($">>> Критическая ошибка при импорте: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                ModelState.AddModelError("", "Ошибка при обработке файла: " + ex.Message);
                return View();
            }

            return RedirectToAction("Index");
        }

//----------------
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


        private Property CreatePropertyFromRow(
            IXLRow row,
            Dictionary<string, int> propertyTypes,
            Dictionary<string, int> locations,
            Dictionary<string, int> users)
        {
            // Используем GetValue<string>() — безопасный способ получения строки
            var name = row.Cell(1).GetValue<string>()?.Trim() ?? "";
            var description = row.Cell(2).GetValue<string>()?.Trim() ?? "";
            var inventoryNumber = row.Cell(3).GetValue<string>()?.Trim() ?? "";
            var propertyTypeName = row.Cell(4).GetValue<string>()?.Trim() ?? "";
            
            Console.WriteLine($">>> [DEBUG] Тип имущества из Excel: '{propertyTypeName}' (длина: {propertyTypeName.Length})");
            
            var locationName = row.Cell(5).GetValue<string>()?.Trim() ?? "";
            var assignedUserName = row.Cell(6).GetValue<string>()?.Trim() ?? "";
            var balanceDateStr = row.Cell(7).GetValue<string>()?.Trim();
            var usagePeriodStr = row.Cell(8).GetValue<string>()?.Trim();
            var costStr = row.Cell(9).GetValue<string>()?.Trim();
            var lastMaintenanceDateStr = row.Cell(10).GetValue<string>()?.Trim();
            var expiryDateStr = row.Cell(11).GetValue<string>()?.Trim();

            Console.WriteLine($">>> [DEBUG] Чтение строки: Название='{name}', Тип='{propertyTypeName}', Инвентарный='{inventoryNumber}'");
            
            // Проверка обязательных полей
            if (string.IsNullOrWhiteSpace(name))
                throw new InvalidOperationException("Название имущества обязательно");

            if (string.IsNullOrWhiteSpace(inventoryNumber))
                throw new InvalidOperationException("Инвентарный номер обязателен");

            // 🔴 Проверка: если тип имущества не указан
            if (string.IsNullOrWhiteSpace(propertyTypeName))
            {
                throw new InvalidOperationException(
                    $"Тип имущества не указан. Название: '{name}', Инвентарный: '{inventoryNumber}'");
            }

            // --- Парсинг дат ---
            DateTime? balanceDate = null;
            if (!string.IsNullOrWhiteSpace(balanceDateStr))
            {
                if (!DateTime.TryParse(balanceDateStr, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out var parsed))
                {
                    throw new InvalidOperationException($"Невозможно распознать дату баланса: '{balanceDateStr}'");
                }
                balanceDate = parsed.ToUniversalTime();
            }

            DateTime? lastMaintenanceDate = null;
            if (!string.IsNullOrWhiteSpace(lastMaintenanceDateStr))
            {
                if (!DateTime.TryParse(lastMaintenanceDateStr, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out var parsed))
                {
                    throw new InvalidOperationException($"Невозможно распознать дату обслуживания: '{lastMaintenanceDateStr}'");
                }
                lastMaintenanceDate = parsed.ToUniversalTime();
            }

            DateTime? expiryDate = null;
            if (!string.IsNullOrWhiteSpace(expiryDateStr))
            {
                if (!DateTime.TryParse(expiryDateStr, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out var parsed))
                {
                    throw new InvalidOperationException($"Невозможно распознать срок годности: '{expiryDateStr}'");
                }
                expiryDate = parsed.ToUniversalTime();
            }

            // --- Парсинг чисел ---
            int? usagePeriod = null;
            if (!string.IsNullOrWhiteSpace(usagePeriodStr))
            {
                if (!int.TryParse(usagePeriodStr, out var up))
                {
                    throw new InvalidOperationException($"Невозможно распознать срок использования: '{usagePeriodStr}'");
                }
                usagePeriod = up;
            }

            decimal? cost = null;
            if (!string.IsNullOrWhiteSpace(costStr))
            {
                if (!decimal.TryParse(costStr, NumberStyles.Currency, new CultureInfo("ru-RU"), out var c))
                {
                    throw new InvalidOperationException($"Невозможно распознать стоимость: '{costStr}'");
                }
                cost = c;
            }


            // --- Проверка справочников ---
            Console.WriteLine($">>> Ищем в propertyTypes: ключи = [{string.Join(", ", propertyTypes.Keys)}]");

            if (!propertyTypes.TryGetValue(propertyTypeName, out var propertyTypeId))
            {
                throw new InvalidOperationException($"Тип имущества '{propertyTypeName}' не найден в справочнике");
            }

            if (!locations.TryGetValue(locationName, out var locationId))
            {
                throw new InvalidOperationException($"Размещение '{locationName}' не найдено в справочнике");
            }

            int assignedUserId = 0; // Объявляем вне

            if (!string.IsNullOrWhiteSpace(assignedUserName))
            {
                if (!users.TryGetValue(assignedUserName, out var userId))
                {
                    throw new InvalidOperationException($"Пользователь '{assignedUserName}' не найден в справочнике");
                }
                assignedUserId = userId;
            }


            // --- Логи для отладки ---
            Console.WriteLine($">>> [DEBUG] PropertyTypeId: {propertyTypeId} для '{propertyTypeName}'");
            Console.WriteLine($">>> [DEBUG] LocationId: {locationId} для '{locationName}'");
            Console.WriteLine($">>> [DEBUG] AssignedUserId: {(string.IsNullOrWhiteSpace(assignedUserName) ? 0 : assignedUserId)} для '{assignedUserName ?? "null"}'");
            Console.WriteLine($">>> Создание Property: Name='{name}', PropertyTypeId={propertyTypeId}");
            // ---

            // --- Создание объекта ---
            return new Property
            {
                Name = name,
                Description = description,
                InventoryNumber = inventoryNumber,
                PropertyTypeId = propertyTypeId,
                LocationId = locationId,
                AssignedUserId = string.IsNullOrWhiteSpace(assignedUserName) ? null : (int?)assignedUserId,
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
            
            ViewBag.Tags = _context.Tags.Where(t => t.IsActive).ToList();
            ViewBag.SelectedTagId = tagId;
            
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
            
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var documentBytes = _barcodeDocxService.GenerateBarcodeDocument(propertyList, baseUrl);
            
            return File(documentBytes, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", "barcodes.docx");
        }

        /// <summary>
        /// Передача имущества другому пользователю
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TransferProperty(int id, [FromBody] AssignPropertyRequest request)
        {
            try
            {
                var property = await _context.Properties
                    .Include(p => p.AssignedUser)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (property == null)
                {
                    return Json(new { success = false, message = "Имущество не найдено" });
                }

                // Получаем текущего пользователя (от кого передается имущество)
                var currentUserName = User.Identity.Name;
                var currentUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Name == currentUserName);

                if (currentUser == null)
                {
                    return Json(new { success = false, message = "Пользователь не найден" });
                }

                // Получаем пользователя, которому передается имущество
                var targetUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == request.PropertyId); // PropertyId здесь используется как UserId

                if (targetUser == null)
                {
                    return Json(new { success = false, message = "Целевой пользователь не найден" });
                }

                // Создаем запись в истории передач
                var transfer = new PropertyTransfer
                {
                    PropertyId = id,
                    FromUserId = property.AssignedUserId ?? currentUser.Id, // Если имущество никому не назначено, считаем что передает текущий пользователь
                    ToUserId = targetUser.Id,
                    TransferDate = DateTime.UtcNow,
                    Notes = $"Передача имущества пользователю {targetUser.Name}"
                };

                _context.PropertyTransfers.Add(transfer);

                // Обновляем назначенного пользователя у имущества
                property.AssignedUserId = targetUser.Id;
                _context.Properties.Update(property);

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = $"Имущество успешно передано пользователю {targetUser.Name}" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Ошибка при передаче имущества: {ex.Message}" });
            }
        }

        /// <summary>
        /// История передач имущества
        /// </summary>
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> TransferHistory(int id)
        {
            var property = await _context.Properties
                .Include(p => p.AssignedUser)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (property == null)
            {
                return NotFound();
            }

            var transferHistory = await _context.PropertyTransfers
                .Include(t => t.FromUser)
                .Include(t => t.ToUser)
                .Where(t => t.PropertyId == id)
                .OrderByDescending(t => t.TransferDate)
                .ToListAsync();

            ViewBag.Property = property;
            return View(transferHistory);
        }

        /// <summary>
        /// Быстрая передача имущества (форма)
        /// </summary>
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> QuickTransfer(int id)
        {
            var property = await _context.Properties
                .Include(p => p.AssignedUser)
                .Include(p => p.PropertyType)
                .Include(p => p.Location)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (property == null)
            {
                return NotFound();
            }

            var availableUsers = await _context.Users
                .Where(u => u.IsActive && u.Id != property.AssignedUserId)
                .OrderBy(u => u.Name)
                .ToListAsync();

            ViewBag.AvailableUsers = new SelectList(availableUsers, "Id", "Name");
            return View(property);
        }

        /// <summary>
        /// Получение списка пользователей для передачи (AJAX)
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<JsonResult> GetAvailableUsers(int propertyId)
        {
            var property = await _context.Properties
                .FirstOrDefaultAsync(p => p.Id == propertyId);

            if (property == null)
            {
                return Json(new { success = false, message = "Имущество не найдено" });
            }

            var users = await _context.Users
                .Where(u => u.IsActive && u.Id != property.AssignedUserId)
                .OrderBy(u => u.Name)
                .Select(u => new { u.Id, u.Name, u.Email })
                .ToListAsync();

            return Json(new { success = true, users });
        }

        /// <summary>
        /// Массовая передача имущества
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkTransfer([FromBody] BulkTransferRequest request)
        {
            try
            {
                var currentUserName = User.Identity.Name;
                var currentUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Name == currentUserName);

                if (currentUser == null)
                {
                    return Json(new { success = false, message = "Пользователь не найден" });
                }

                var targetUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == request.TargetUserId);

                if (targetUser == null)
                {
                    return Json(new { success = false, message = "Целевой пользователь не найден" });
                }

                var properties = await _context.Properties
                    .Where(p => request.PropertyIds.Contains(p.Id))
                    .ToListAsync();

                if (!properties.Any())
                {
                    return Json(new { success = false, message = "Имущество не найдено" });
                }

                var transfers = new List<PropertyTransfer>();
                var updatedProperties = new List<Property>();

                foreach (var property in properties)
                {
                    // Создаем запись передачи
                    var transfer = new PropertyTransfer
                    {
                        PropertyId = property.Id,
                        FromUserId = property.AssignedUserId ?? currentUser.Id,
                        ToUserId = targetUser.Id,
                        TransferDate = DateTime.UtcNow,
                        Notes = request.Notes ?? $"Массовая передача пользователю {targetUser.Name}"
                    };
                    transfers.Add(transfer);

                    // Обновляем назначение
                    property.AssignedUserId = targetUser.Id;
                    updatedProperties.Add(property);
                }

                // Сохраняем все изменения в транзакции
                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        _context.PropertyTransfers.AddRange(transfers);
                        _context.Properties.UpdateRange(updatedProperties);
                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();
                    }
                    catch
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
                }

                return Json(new { 
                    success = true, 
                    message = $"Успешно передано {properties.Count} единиц имущества пользователю {targetUser.Name}" 
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Ошибка при массовой передаче: {ex.Message}" });
            }
        }

        // Модель для массовой передачи
        public class BulkTransferRequest
        {
            public List<int> PropertyIds { get; set; } = new List<int>();
            public int TargetUserId { get; set; }
            public string Notes { get; set; }
        }
    }
}