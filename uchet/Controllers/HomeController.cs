using Microsoft.AspNetCore.Mvc;
using uchet.Models;
using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using uchet.Data;
using Microsoft.EntityFrameworkCore;

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
            // Получаем количество имущества, подлежащего списанию
            // Для примера считаем имущество с истекшим сроком годности
            var expiredPropertyCount = _context.Properties
                .Where(p => p.ExpiryDate < DateTime.UtcNow)
                .Count();
            
            ViewBag.ExpiredPropertyCount = expiredPropertyCount;
            return View();
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

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
