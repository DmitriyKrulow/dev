using Microsoft.AspNetCore.Mvc;
using uchet.Models;
using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using uchet.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace uchet.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public IActionResult Index()
        {
            // Создаем модель данных для информационной панели
            var dashboardModel = new DashboardViewModel
            {
                // Общее количество имущества
                TotalPropertyCount = _context.Properties.Count(),
                
                // Количество имущества, требующего ремонта (с истекшим сроком годности)
                ExpiredPropertyCount = _context.Properties
                    .Where(p => p.ExpiryDate < DateTime.UtcNow && p.ExpiryDate.HasValue)
                    .Count(),
                
                // Количество имущества, требующего списания (например, с истекшим сроком более 6 месяцев)
                WriteOffPropertyCount = _context.Properties
                    .Where(p => p.ExpiryDate < DateTime.UtcNow.AddMonths(-6) && p.ExpiryDate.HasValue)
                    .Count(),
                
                // Данные для графика расходов по месяцам (пример за последние 12 месяцев)
                MonthlyExpenses = GetMonthlyExpenses()
            };
            
            return View(dashboardModel);
        }
        
        // Метод для получения данных о расходах по месяцам
        private List<MonthlyExpense> GetMonthlyExpenses()
        {
            var expenses = new List<MonthlyExpense>();
            
            // Получаем данные за последние 12 месяцев
            for (int i = 11; i >= 0; i--)
            {
                var date = DateTime.UtcNow.AddMonths(-i);
                var monthName = date.ToString("MMM yyyy");
                
                // Суммируем стоимость имущества, приобретенного в этот месяц
                var expense = _context.Properties
                    .Where(p => p.BalanceDate.HasValue && 
                               p.BalanceDate.Value.Year == date.Year && 
                               p.BalanceDate.Value.Month == date.Month)
                    .Sum(p => p.Cost ?? 0);
                
                expenses.Add(new MonthlyExpense 
                { 
                    Month = monthName, 
                    Expense = expense 
                });
            }
            
            return expenses;
        }

        [Authorize]
        public IActionResult Privacy()
        {
            return View();
        }

        [Authorize]
        public IActionResult Contact()
        {
            return View();
        }
        
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

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
