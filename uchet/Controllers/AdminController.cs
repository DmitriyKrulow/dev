using Microsoft.AspNetCore.Mvc;
using uchet.Data;
using uchet.Models;
using Microsoft.AspNetCore.Authorization;
using System.Linq;
using System.Collections.Generic;

namespace uchet.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var users = _context.Users.ToList();
            var roles = _context.Roles.ToList();
            ViewBag.Roles = roles;
            return View(users);
        }

        [HttpPost]
        public IActionResult AddUser(string name, string email, int roleId, string password = null)
        {
            // Если пароль не указан, генерируем случайный пароль
            if (string.IsNullOrEmpty(password))
            {
                password = GenerateRandomPassword();
            }
            
            var user = new User
            {
                Name = name,
                Email = email,
                RoleId = roleId,
                Password = password
            };

            _context.Users.Add(user);
            _context.SaveChanges();

            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult ChangeUserRole(int userId, int roleId)
        {
            var user = _context.Users.FirstOrDefault(u => u.Id == userId);
            if (user != null)
            {
                user.RoleId = roleId;
                _context.SaveChanges();
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult EditUser(int userId, string name, string email)
        {
            var user = _context.Users.FirstOrDefault(u => u.Id == userId);
            if (user != null)
            {
                user.Name = name;
                user.Email = email;
                _context.SaveChanges();
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult ToggleUserStatus(int userId)
        {
            var user = _context.Users.FirstOrDefault(u => u.Id == userId);
            if (user != null)
            {
                user.IsActive = !user.IsActive;
                _context.SaveChanges();
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult ResetUserPassword(int userId)
        {
            var user = _context.Users.FirstOrDefault(u => u.Id == userId);
            if (user != null)
            {
                // Генерируем новый случайный пароль
                string newPassword = GenerateRandomPassword();
                user.Password = newPassword;
                _context.SaveChanges();
                
                // Передаем новый пароль в ViewBag для отображения
                ViewBag.ResetPasswordMessage = $"Новый пароль для пользователя {user.Name}: {newPassword}";
            }

            var users = _context.Users.ToList();
            var roles = _context.Roles.ToList();
            ViewBag.Roles = roles;
            return View("Index", users);
        }

        private string GenerateRandomPassword(int length = 8)
        {
            const string validChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*()";
            Random random = new Random();
            char[] chars = new char[length];
            
            for (int i = 0; i < length; i++)
            {
                chars[i] = validChars[random.Next(validChars.Length)];
            }
            
            return new string(chars);
        }
        
        // Новый метод для управления ролями
        public IActionResult RoleManagement()
        {
            var roles = _context.Roles.ToList();
            return View(roles);
        }
        
        // Метод для получения разрешений роли
        [HttpGet]
        public IActionResult GetRolePermissions(int roleId)
        {
            try
            {
                var permissions = _context.RolePermissions
                    .Where(rp => rp.RoleId == roleId)
                    .Select(rp => new { rp.ControllerName, rp.ActionName })
                    .ToList();
                    
                return Json(permissions);
            }
            catch (Exception ex)
            {
                // Логируем ошибку (в реальном приложении используйте логгер)
                Console.WriteLine($"Ошибка при получении разрешений роли {roleId}: {ex.Message} - AdminController.cs:149");
                return Json(new { error = "Ошибка при получении разрешений" });
            }
        }
        
        // Метод для сохранения разрешений роли
        [HttpPost]
        public IActionResult SaveRolePermissions(int roleId, [FromBody] List<RolePermissionDto> permissions)
        {
            try
            {
                // Удаляем существующие разрешения для роли
                var existingPermissions = _context.RolePermissions.Where(rp => rp.RoleId == roleId);
                _context.RolePermissions.RemoveRange(existingPermissions);
                
                // Добавляем новые разрешения
                foreach (var perm in permissions)
                {
                    var rolePermission = new RolePermission
                    {
                        RoleId = roleId,
                        ControllerName = perm.ControllerName,
                        ActionName = perm.ActionName
                    };
                    _context.RolePermissions.Add(rolePermission);
                }
                
                _context.SaveChanges();
                
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                // Логируем ошибку (в реальном приложении используйте логгер)
                Console.WriteLine($"Ошибка при сохранении разрешений роли {roleId}: {ex.Message} - AdminController.cs:183");
                return Json(new { success = false, error = "Ошибка при сохранении разрешений" });
            }
        }
    }
}