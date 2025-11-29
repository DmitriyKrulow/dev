using Microsoft.AspNetCore.Mvc;
using uchet.Data;
using uchet.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;

namespace uchet.Controllers
{
    /// <summary>
    /// Контроллер, отвечающий за аутентификацию пользователей: вход, выход и изменение пароля.
    /// Обеспечивает безопасный доступ к системе с использованием аутентификации на основе куки.
    /// </summary>
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="AccountController"/>.
        /// </summary>
        /// <param name="context">Контекст базы данных приложения, используемый для доступа к пользователям и ролям.</param>
        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Возвращает представление формы входа в систему.
        /// Доступен всем пользователям без аутентификации.
        /// </summary>
        /// <returns>Представление <c>Login</c>.</returns>
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        /// <summary>
        /// Обрабатывает отправку формы входа. Проверяет электронную почту и пароль пользователя.
        /// При успешной аутентификации создает cookie-аутентификацию и перенаправляет на главную страницу.
        /// </summary>
        /// <param name="email">Электронная почта пользователя.</param>
        /// <param name="password">Пароль пользователя.</param>
        /// <returns>
        /// Перенаправление на главную страницу при успешном входе;
        /// иначе — возврат представления с сообщением об ошибке.
        /// </returns>
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

        /// <summary>
        /// Возвращает представление для изменения пароля.
        /// Доступ разрешён только аутентифицированным пользователям.
        /// </summary>
        /// <returns>Представление <c>ChangePassword</c>.</returns>
        [HttpGet]
        [Authorize]
        public IActionResult ChangePassword()
        {
            return View();
        }

        /// <summary>
        /// Обрабатывает запрос на изменение пароля.
        /// Проверяет старый пароль, соответствие нового пароля и его подтверждения.
        /// При успешной проверке обновляет пароль в базе данных.
        /// </summary>
        /// <param name="oldPassword">Текущий пароль пользователя.</param>
        /// <param name="newPassword">Новый пароль.</param>
        /// <param name="confirmNewPassword">Подтверждение нового пароля.</param>
        /// <returns>
        /// Текущее представление с сообщением об ошибке при неудаче;
        /// или с сообщением об успешном изменении при успехе.
        /// </returns>
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

        /// <summary>
        /// Обрабатывает выход пользователя из системы.
        /// Удаляет аутентификационные куки и перенаправляет на главную страницу.
        /// </summary>
        /// <returns>Перенаправление на действие <c>Index</c> контроллера <c>Home</c>.</returns>
        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }
    }
}
