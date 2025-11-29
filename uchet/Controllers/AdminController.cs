using Microsoft.AspNetCore.Mvc;
using uchet.Data;
using uchet.Models;
using Microsoft.AspNetCore.Authorization;
using System.Linq;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace uchet.Controllers
{
    /// <summary>
    /// Контроллер, предоставляющий административные функции для управления пользователями, ролями, 
    /// справочниками (места, типы имущества) и разрешениями.
    /// Доступ к контроллеру разрешён только пользователям с ролью "Admin".
    /// </summary>
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="AdminController"/>.
        /// </summary>
        /// <param name="context">Контекст базы данных приложения.</param>
        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Возвращает представление со списком всех пользователей системы.
        /// Также передаёт в представление список ролей и местоположений для использования в выпадающих списках.
        /// </summary>
        /// <returns>Представление <c>Index</c> со списком пользователей.</returns>
        public IActionResult Index()
        {
            var users = _context.Users.Include(u => u.Location).ToList();
            var roles = _context.Roles.ToList();
            var locations = _context.Locations.ToList();
            ViewBag.Roles = roles;
            ViewBag.Locations = locations;
            return View(users);
        }

        /// <summary>
        /// Обрабатывает добавление нового пользователя в систему.
        /// Если пароль не указан, генерируется случайный.
        /// </summary>
        /// <param name="name">Имя пользователя.</param>
        /// <param name="email">Электронная почта пользователя.</param>
        /// <param name="roleId">Идентификатор роли пользователя.</param>
        /// <param name="locationId">Идентификатор местоположения (необязательно).</param>
        /// <param name="password">Пароль пользователя (опционально, по умолчанию — генерируется).</param>
        /// <returns>Перенаправление на действие <c>Index</c>.</returns>
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

        /// <summary>
        /// Изменяет роль указанного пользователя.
        /// </summary>
        /// <param name="userId">Идентификатор пользователя.</param>
        /// <param name="roleId">Новый идентификатор роли.</param>
        /// <returns>Перенаправление на действие <c>Index</c>.</returns>
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

        /// <summary>
        /// Обновляет основные данные пользователя: имя, email и местоположение.
        /// </summary>
        /// <param name="userId">Идентификатор пользователя.</param>
        /// <param name="name">Новое имя пользователя.</param>
        /// <param name="email">Новый email пользователя.</param>
        /// <param name="locationId">Новое местоположение (может быть null).</param>
        /// <returns>Перенаправление на действие <c>Index</c>.</returns>
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

        /// <summary>
        /// Переключает статус активности пользователя (активен/неактивен).
        /// </summary>
        /// <param name="userId">Идентификатор пользователя.</param>
        /// <returns>Перенаправление на действие <c>Index</c>.</returns>
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

        /// <summary>
        /// Сбрасывает пароль пользователя, генерируя новый случайный.
        /// Новый пароль возвращается в представление через <see cref="ViewBag"/>.
        /// </summary>
        /// <param name="userId">Идентификатор пользователя, чей пароль нужно сбросить.</param>
        /// <returns>Представление <c>Index</c> с сообщением о новом пароле.</returns>
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

        /// <summary>
        /// Генерирует случайный пароль указанной длины.
        /// Используется при добавлении и сбросе пароля пользователя.
        /// </summary>
        /// <param name="length">Длина пароля (по умолчанию — 8 символов).</param>
        /// <returns>Сгенерированный строковый пароль.</returns>
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
        
        /// <summary>
        /// Возвращает представление для управления ролями системы.
        /// Отображает список всех существующих ролей.
        /// </summary>
        /// <returns>Представление <c>RoleManagement</c> со списком ролей.</returns>
        public IActionResult RoleManagement()
        {
            var roles = _context.Roles.ToList();
            return View(roles);
        }
        
        /// <summary>
        /// Возвращает представление для управления справочниками системы:
        /// местоположения и типы имущества.
        /// Передаёт в представление списки соответствующих сущностей.
        /// </summary>
        /// <returns>Представление <c>Reference</c> с данными справочников.</returns>
        public IActionResult Reference()
        {
            var locations = _context.Locations.ToList();
            var propertyTypes = _context.PropertyTypes.ToList();
            ViewBag.Locations = locations;
            ViewBag.PropertyTypes = propertyTypes;
            return View();
        }
        
        /// <summary>
        /// Добавляет новое местоположение в систему.
        /// </summary>
        /// <param name="name">Название местоположения.</param>
        /// <param name="description">Описание местоположения (необязательно).</param>
        /// <returns>Перенаправление на действие <c>Reference</c>.</returns>
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
        
        /// <summary>
        /// Обновляет данные существующего местоположения.
        /// </summary>
        /// <param name="id">Идентификатор местоположения.</param>
        /// <param name="name">Новое название.</param>
        /// <param name="description">Новое описание.</param>
        /// <returns>Перенаправление на действие <c>Reference</c>.</returns>
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
        
        /// <summary>
        /// Удаляет местоположение по его идентификатору.
        /// </summary>
        /// <param name="id">Идентификатор удаляемого местоположения.</param>
        /// <returns>Перенаправление на действие <c>Reference</c>.</returns>
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
        
        /// <summary>
        /// Добавляет новый тип имущества в систему.
        /// </summary>
        /// <param name="name">Название типа имущества.</param>
        /// <param name="description">Описание типа (необязательно).</param>
        /// <returns>Перенаправление на действие <c>Reference</c>.</returns>
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
        
        /// <summary>
        /// Обновляет данные существующего типа имущества.
        /// </summary>
        /// <param name="id">Идентификатор типа имущества.</param>
        /// <param name="name">Новое название.</param>
        /// <param name="description">Новое описание.</param>
        /// <returns>Перенаправление на действие <c>Reference</c>.</returns>
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
        
        /// <summary>
        /// Удаляет тип имущества по его идентификатору.
        /// </summary>
        /// <param name="id">Идентификатор удаляемого типа имущества.</param>
        /// <returns>Перенаправление на действие <c>Reference</c>.</returns>
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
        
        /// <summary>
        /// Получает список разрешений для указанной роли в формате JSON.
        /// Используется для динамической загрузки разрешений на странице управления ролями.
        /// </summary>
        /// <param name="roleId">Идентификатор роли.</param>
        /// <returns>JSON-ответ со списком контроллеров и действий, доступных роли.</returns>
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
                Console.WriteLine($"Ошибка при получении разрешений роли {roleId}: {ex.Message} - AdminController.cs:353");
                return Json(new { error = "Ошибка при получении разрешений" });
            }
        }
        
        /// <summary>
        /// Сохраняет обновлённый список разрешений для роли.
        /// Сначала удаляет все текущие разрешения роли, затем добавляет новые.
        /// </summary>
        /// <param name="roleId">Идентификатор роли.</param>
        /// <param name="permissions">Список новых разрешений в формате DTO.</param>
        /// <returns>JSON-ответ об успешности операции.</returns>
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
                Console.WriteLine($"Ошибка при сохранении разрешений роли {roleId}: {ex.Message} - AdminController.cs:393");
                return Json(new { success = false, error = "Ошибка при сохранении разрешений" });
            }
        }
    }
}
