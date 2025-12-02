using Microsoft.AspNetCore.Mvc;
using uchet.Models;
using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using uchet.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace uchet.Controllers
{
    /// <summary>
    /// Контроллер, отвечающий за обработку запросов к главной странице и служебным разделам сайта.
    /// Содержит действия для отображения информационной панели, политики конфиденциальности, контактов,
    /// а также тестовые методы для проверки состояния базы данных.
    /// </summary>
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="HomeController"/>.
        /// </summary>
        /// <param name="logger">Сервис для логирования событий в контроллере.</param>
        /// <param name="context">Контекст базы данных приложения.</param>
        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        /// <summary>
        /// Возвращает представление главной странице с данными информационной панели.
        /// Данные включают общее количество имущества, количество имущества с истёкшим сроком годности,
        /// имущество, подлежащее списанию, а также статистику расходов по месяцам.
        /// </summary>
        /// <returns>Представление <c>Index</c> с моделью <see cref="DashboardViewModel"/>.</returns>
        public IActionResult Index()
        {
            // Создаем модель данных для информационной панели
            var dashboardModel = new DashboardViewModel
            {
                // Общее количество имущества
                TotalPropertyCount = _context.Properties.Count(),
                
                // Общая стоимость всего имущества
                TotalPropertyCost = _context.Properties.Sum(p => p.Cost ?? 0),
                
                // Количество и стоимость имущества, требующего ремонта (с истекшим сроком годности)
                ExpiredPropertyCount = _context.Properties
                    .Where(p => p.ExpiryDate < DateTime.UtcNow && p.ExpiryDate.HasValue)
                    .Count(),
                ExpiredPropertyCost = _context.Properties
                    .Where(p => p.ExpiryDate < DateTime.UtcNow && p.ExpiryDate.HasValue)
                    .Sum(p => p.Cost ?? 0),
                
                // Количество имущества, требующего списания (например, с истекшим сроком более 6 месяцев)
                WriteOffPropertyCount = _context.Properties
                    .Where(p => p.ExpiryDate < DateTime.UtcNow.AddMonths(-6) && p.ExpiryDate.HasValue)
                    .Count(),
                
                // Данные для графика поступлений имущества по месяцам
                MonthlyAcquisitions = GetMonthlyAcquisitions(),
                
                // Данные для круговой диаграммы проверенного/непроверенного имущества
                CheckedPropertyCount = _context.Properties.Count(p => p.IsCheckedInLastInventory),
                UncheckedPropertyCount = _context.Properties.Count(p => !p.IsCheckedInLastInventory)
            };
            
            return View(dashboardModel);
        }
        
        /// <summary>
        /// Возвращает список поступлений имущества по месяцам за последние 12 месяцев.
        /// Каждая запись содержит название месяца, количество поступившего имущества и общую стоимость.
        /// </summary>
        /// <returns>Список объектов <see cref="MonthlyAcquisition"/>, представляющих поступления по месяцам.</returns>
        private List<MonthlyAcquisition> GetMonthlyAcquisitions()
        {
            var acquisitions = new List<MonthlyAcquisition>();
            
            // Получаем данные за последние 12 месяцев
            for (int i = 11; i >= 0; i--)
            {
                var date = DateTime.UtcNow.AddMonths(-i);
                var monthName = date.ToString("MMM yyyy");
                
                // Подсчитываем количество имущества, поступившего в этот месяц
                var count = _context.Properties
                    .Count(p => p.BalanceDate.HasValue && 
                               p.BalanceDate.Value.Year == date.Year && 
                               p.BalanceDate.Value.Month == date.Month);
                
                // Суммируем стоимость имущества, поступившего в этот месяц
                var totalCost = _context.Properties
                    .Where(p => p.BalanceDate.HasValue && 
                               p.BalanceDate.Value.Year == date.Year && 
                               p.BalanceDate.Value.Month == date.Month)
                    .Sum(p => p.Cost ?? 0);
                
                acquisitions.Add(new MonthlyAcquisition 
                { 
                    Month = monthName, 
                    Count = count,
                    TotalCost = totalCost
                });
            }
            
            return acquisitions;
        }

        /// <summary>
        /// Возвращает представление с информацией о политике конфиденциальности.
        /// </summary>
        /// <returns>Представление <c>Privacy</c>.</returns>
        public IActionResult Privacy()
        {
            return View();
        }

        /// <summary>
        /// Возвращает представление с контактной информацией.
        /// </summary>
        /// <returns>Представление <c>Contact</c>.</returns>
        public IActionResult Contact()
        {
            return View();
        }
        
        /// <summary>
        /// Тестовый метод для проверки доступности и содержимого таблицы <c>Inventories</c> и <c>InventoryItems</c>.
        /// Возвращает текстовое содержимое с количеством записей и образцами данных.
        /// Используется для диагностики подключения к базе данных.
        /// </summary>
        /// <returns>Текстовый ответ с информацией о данных в таблицах инвентаризаций.</returns>
        [HttpGet]
        public async Task<IActionResult> TestDatabase()
        {
            var inventoryCount = await _context.Inventories.CountAsync();
            var inventoryItemCount = await _context.InventoryItems.CountAsync();
            
            var result = $"Inventories: {inventoryCount}, InventoryItems: {inventoryItemCount}";
            
            // Also get some sample data
            var sampleInventories = await _context.Inventories.Take(5).ToListAsync();
            var sampleInventoryItems = await _context.InventoryItems.Take(5).ToListAsync();
            
            result += "\n\nSample Inventories:";
            foreach (var inv in sampleInventories)
            {
                result += $"\n- Id: {inv.Id}, Name: {inv.Name}, LocationId: {inv.LocationId}, TotalItems: {inv.TotalItems}, CheckedItems: {inv.CheckedItems}";
            }
            
            result += "\n\nSample InventoryItems:";
            foreach (var item in sampleInventoryItems)
            {
                result += $"\n- Id: {item.Id}, InventoryId: {item.InventoryId}, PropertyId: {item.PropertyId}, IsChecked: {item.IsChecked}";
            }
            
            return Content(result, "text/plain");
        }
        
        /// <summary>
        /// Тестовый метод для проверки доступности и содержимого таблицы <c>Properties</c>.
        /// Возвращает общее количество имущества и количество в определённом месте (LocationId = 1),
        /// а также образцы записей.
        /// </summary>
        /// <returns>Текстовый ответ с информацией о данных в таблице имущества.</returns>
        [HttpGet]
        public async Task<IActionResult> TestProperties()
        {
            var propertyCount = await _context.Properties.CountAsync();
            var location1PropertyCount = await _context.Properties.CountAsync(p => p.LocationId == 1);
            
            var result = $"Total Properties: {propertyCount}, Properties in Location 1: {location1PropertyCount}";
            
            // Also get some sample data
            var sampleProperties = await _context.Properties.Take(5).ToListAsync();
            
            result += "\n\nSample Properties:";
            foreach (var prop in sampleProperties)
            {
                result += $"\n- Id: {prop.Id}, Name: {prop.Name}, LocationId: {prop.LocationId}, InventoryNumber: {prop.InventoryNumber}";
            }
            
            return Content(result, "text/plain");
        }

        /// <summary>
        /// Возвращает страницу с ошибкой приложения.
        /// Отключает кэширование ответа, чтобы предотвратить сохранение информации об ошибках.
        /// </summary>
        /// <returns>Представление <c>Error</c> с моделью <see cref="ErrorViewModel"/>.</returns>
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
