using Microsoft.EntityFrameworkCore;
using uchet.Data;
using uchet.Services;
using Microsoft.AspNetCore.Authentication.Cookies;

/// <summary>
/// Точка входа в приложение.
/// Настройка сервисов и конвейера обработки HTTP-запросов.
/// </summary>
/// <remarks>
/// В этом файле происходит настройка:
/// - Сервисов приложения (DI-контейнер),
/// - Контекста базы данных,
/// - Аутентификации с использованием куки,
/// - HTTP-клиентов,
/// - Конвейера middleware,
/// - Маршрутизации.
/// </remarks>
var builder = WebApplication.CreateBuilder(args);

// Добавляем сервисы в контейнер
builder.Services.AddControllersWithViews();

/// <summary>
/// Добавляет контекст базы данных <see cref="ApplicationDbContext"/> в контейнер сервисов.
/// Используется провайдер PostgreSQL через Npgsql.
/// Строка подключения берётся из конфигурации по ключу "DefaultConnection".
/// </summary>
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

/// <summary>
/// Настраивает аутентификацию с использованием куки.
/// Пользователь будет перенаправляться на /Account/Login при отсутствии аутентификации.
/// При отсутствии прав доступа — перенаправление на /Account/AccessDenied.
/// </summary>
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
    });

/// <summary>
/// Добавляет фабрику HTTP-клиентов для выполнения HTTP-запросов.
/// Используется сервисом <see cref="BarcodeDocxService"/> для генерации документов.
/// </summary>
builder.Services.AddHttpClient();

/// <summary>
/// Регистрирует сервис <see cref="BarcodeDocxService"/> с областью действия на один запрос (Scoped).
/// Сервис отвечает за генерацию DOCX-документов с штрих-кодами.
/// </summary>
builder.Services.AddScoped<BarcodeDocxService>();

var app = builder.Build();

/// <summary>
/// Настраивает приложение на прослушивание запросов по указанному URL.
/// В данном случае — только локальный хост на порту 5251.
/// </summary>
app.Urls.Add("http://localhost:5251");

/// <summary>
/// Проверяет наличие базы данных и создаёт её (включая таблицы), если она ещё не существует.
/// Используется <see cref="EnsureCreated"/> — подходит для разработки, но не для продакшена.
/// </summary>
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    context.Database.EnsureCreated();
}

/// <summary>
/// Перенаправляет HTTP-запросы на HTTPS (если возможно).
/// </summary>
app.UseHttpsRedirection();

/// <summary>
/// Включает маршрутизацию запросов к контроллерам.
/// </summary>
app.UseRouting();

/// <summary>
/// Включает middleware аутентификации.
/// Должно быть до <see cref="UseAuthorization"/>.
/// </summary>
app.UseAuthentication();

/// <summary>
/// Включает middleware авторизации.
/// Проверяет, имеет ли аутентифицированный пользователь доступ к ресурсу.
/// </summary>
app.UseAuthorization();

/// <summary>
/// Включает раздачу статических файлов (например, из папки wwwroot).
/// </summary>
app.MapStaticAssets();

/// <summary>
/// Настраивает основной маршрут по умолчанию:
/// - Контроллер: Home
/// - Действие: Index
/// - Параметр id: опциональный
/// Также связывает статические ресурсы с маршрутом.
/// </summary>
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

/// <summary>
/// Запускает веб-приложение и начинает прослушивание входящих HTTP-запросов.
/// </summary>
app.Run();
