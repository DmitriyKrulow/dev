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

            if (file.Length > 10 * 1024 * 1024)
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
                var propertyTypes = await _context.PropertyTypes.ToDictionaryAsync(pt => pt.Name, pt => pt.Id);
                var locations = await _context.Locations.ToDictionaryAsync(l => l.Name, l => l.Id);
                var users = await _context.Users.Where(u => u.IsActive).ToDictionaryAsync(u => u.Name, u => u.Id);

                using (var stream = new MemoryStream())
                {
                    await file.CopyToAsync(stream);

                    using (var workbook = new XLWorkbook(stream))
                    {
                        var worksheet = workbook.Worksheet(1);
                        var rows = worksheet.RowsUsed().Skip(1).ToList();

                        if (rows.Count > 1000)
                        {
                            rows = rows.Take(1000).ToList();
                            errors.Add("Обработаны только первые 1000 строк файла");
                        }

                        var propertiesToAdd = new List<Property>();
                        
                        var inventoryNumbersInFile = rows.Select(r => r.Cell(3).Value.ToString().Trim()).ToList();
                        var existingInventoryNumbers = await _context.Properties
                            .Where(p => inventoryNumbersInFile.Contains(p.InventoryNumber))
                            .Select(p => p.InventoryNumber)
                            .ToListAsync();

                        using (var transaction = await _context.Database.BeginTransactionAsync())
                        {
                            try
                            {
                                foreach (var row in rows)
                                {
                                    try
                                    {
                                        var rowNumber = row.RowNumber();

                                        var validationResult = ValidatePropertyRow(row, rowNumber);
                                        if (!validationResult.isValid)
                                        {
                                            errors.AddRange(validationResult.errors);
                                            continue;
                                        }

                                        var inventoryNumber = row.Cell(3).Value.ToString().Trim();

                                        if (existingInventoryNumbers.Contains(inventoryNumber) ||
                                            propertiesToAdd.Any(p => p.InventoryNumber == inventoryNumber))
                                        {
                                            var errorMsg = $"Строка {rowNumber}: Имущество с инвентарным номером {inventoryNumber} уже существует";
                                            errors.Add(errorMsg);
                                            continue;
                                        }

                                        var property = CreatePropertyFromRow(row, propertyTypes, locations, users);

                                        if (property.PropertyTypeId == 0)
                                        {
                                            var propertyTypeName = row.Cell(4).Value.ToString();
                                            var errorMsg = $"Строка {rowNumber}: Тип имущества '{propertyTypeName}' не найден";
                                            errors.Add(errorMsg);
                                            continue;
                                        }

                                        if (property.LocationId == 0)
                                        {
                                            var locationName = row.Cell(5).Value.ToString();
                                            var errorMsg = $"Строка {rowNumber}: Размещение '{locationName}' не найдено";
                                            errors.Add(errorMsg);
                                            continue;
                                        }

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
                                        var errorMsg = $"Строка {row.RowNumber()}: Ошибка импорта - {ex.Message}";
                                        errors.Add(errorMsg);
                                    }
                                }

                                if (propertiesToAdd.Any())
                                {
                                    _context.Properties.AddRange(propertiesToAdd);
                                    await _context.SaveChangesAsync();
                                    importedCount += propertiesToAdd.Count;
                                }

                                await transaction.CommitAsync();

                                var message = $"Успешно импортировано {importedCount} записей.";
                                if (errors.Any())
                                {
                                    message += $" Ошибок: {errors.Count}.";
                                    TempData["ImportErrors"] = errors.Take(50).ToList();
                                }

                                TempData["Message"] = message;
                            }
                            catch (Exception ex)
                            {
                                await transaction.RollbackAsync();
                                ModelState.AddModelError("", "Ошибка при сохранении данных: " + ex.Message);
                                return View();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Ошибка при обработке файла: " + ex.Message);
                return View();
            }

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

            propertyTypes.TryGetValue(propertyTypeName, out var propertyTypeId);
            locations.TryGetValue(locationName, out var locationId);
            users.TryGetValue(assignedUserName, out var assignedUserId);

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