using Microsoft.AspNetCore.Mvc;
using uchet.Data;
using uchet.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;

namespace uchet.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        {
            // Проверяем email и пароль пользователя
            var user = _context.Users.FirstOrDefault(u => u.Email == email && u.Password == password);

            if (user != null)
            {
                // Загружаем роль пользователя
                _context.Entry(user).Reference(u => u.Role).Load();
                
                // Создаем клеймы для пользователя
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.Name),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Role, user.Role?.Name ?? "User")
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties();

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                return RedirectToAction("Index", "Home");
            }

            ViewBag.Error = "Неверный email или пароль";
            return View();
        }

        [HttpGet]
        [Authorize]
        public IActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        [Authorize]
        public IActionResult ChangePassword(string oldPassword, string newPassword, string confirmNewPassword)
        {
            // Получаем ID текущего пользователя
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var user = _context.Users.FirstOrDefault(u => u.Id == userId);

            // Проверяем старый пароль
            if (user.Password != oldPassword)
            {
                ViewBag.Error = "Неверный старый пароль";
                return View();
            }

            // Проверяем совпадение нового пароля и подтверждения
            if (newPassword != confirmNewPassword)
            {
                ViewBag.Error = "Новый пароль и подтверждение не совпадают";
                return View();
            }

            // Обновляем пароль
            user.Password = newPassword;
            _context.SaveChanges();

            ViewBag.Success = "Пароль успешно изменен";
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }
    }
}