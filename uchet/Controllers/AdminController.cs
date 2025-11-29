using Microsoft.AspNetCore.Mvc;
using uchet.Data;
using uchet.Models;
using Microsoft.AspNetCore.Authorization;
using System.Linq;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

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
            var users = _context.Users.Include(u => u.Location).ToList();
            var roles = _context.Roles.ToList();
            var locations = _context.Locations.ToList();
            ViewBag.Roles = roles;
            ViewBag.Locations = locations;
            return View(users);
        }

        [HttpPost]
        public IActionResult AddUser(string name, string email, int roleId, int? locationId, string? password = null)
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
                LocationId = locationId,
                Password = password!
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
        public IActionResult EditUser(int userId, string name, string email, int? locationId)
        {
            var user = _context.Users.FirstOrDefault(u => u.Id == userId);
            if (user != null)
            {
                user.Name = name;
                user.Email = email;
                user.LocationId = locationId;
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
        
        // Методы для управления справочниками
        public IActionResult Reference()
        {
            var locations = _context.Locations.ToList();
            var propertyTypes = _context.PropertyTypes.ToList();
            ViewBag.Locations = locations;
            ViewBag.PropertyTypes = propertyTypes;
            return View();
        }
        
        [HttpPost]
        public IActionResult AddLocation(string name, string description)
        {
            var location = new Location
            {
                Name = name,
                Description = description
            };
            
            _context.Locations.Add(location);
            _context.SaveChanges();
            
            return RedirectToAction("Reference");
        }
        
        [HttpPost]
        public IActionResult EditLocation(int id, string name, string description)
        {
            var location = _context.Locations.FirstOrDefault(l => l.Id == id);
            if (location != null)
            {
                location.Name = name;
                location.Description = description;
                _context.SaveChanges();
            }
            
            return RedirectToAction("Reference");
        }
        
        [HttpPost]
        public IActionResult DeleteLocation(int id)
        {
            var location = _context.Locations.FirstOrDefault(l => l.Id == id);
            if (location != null)
            {
                _context.Locations.Remove(location);
                _context.SaveChanges();
            }
            
            return RedirectToAction("Reference");
        }
        
        [HttpPost]
        public IActionResult AddPropertyType(string name, string description)
        {
            var propertyType = new PropertyType
            {
                Name = name,
                Description = description
            };
            
            _context.PropertyTypes.Add(propertyType);
            _context.SaveChanges();
            
            return RedirectToAction("Reference");
        }
        
        [HttpPost]
        public IActionResult EditPropertyType(int id, string name, string description)
        {
            var propertyType = _context.PropertyTypes.FirstOrDefault(pt => pt.Id == id);
            if (propertyType != null)
            {
                propertyType.Name = name;
                propertyType.Description = description;
                _context.SaveChanges();
            }
            
            return RedirectToAction("Reference");
        }
        
        [HttpPost]
        public IActionResult DeletePropertyType(int id)
        {
            var propertyType = _context.PropertyTypes.FirstOrDefault(pt => pt.Id == id);
            if (propertyType != null)
            {
                _context.PropertyTypes.Remove(propertyType);
                _context.SaveChanges();
            }
            
            return RedirectToAction("Reference");
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
                Console.WriteLine($"Ошибка при получении разрешений роли {roleId}: {ex.Message} - AdminController.cs:248");
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
                Console.WriteLine($"Ошибка при сохранении разрешений роли {roleId}: {ex.Message} - AdminController.cs:282");
                return Json(new { success = false, error = "Ошибка при сохранении разрешений" });
            }
        }
    }
}