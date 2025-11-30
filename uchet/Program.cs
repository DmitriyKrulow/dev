using Microsoft.EntityFrameworkCore;
using uchet.Data;
using uchet.Services;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// Добавляем сервисы в контейнер
builder.Services.AddControllersWithViews();

// Контекст базы данных
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Аутентификация
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
    });

// HTTP клиенты и сервисы
builder.Services.AddHttpClient();
builder.Services.AddScoped<BarcodeDocxService>();
builder.Services.AddScoped<IPropertyTransferService, PropertyTransferService>();
builder.Services.AddScoped<IMaintenanceService, MaintenanceService>();

var app = builder.Build();

// Настройка URL
app.Urls.Add("http://localhost:5251");

// Инициализация базы данных
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    
    // Удаляем и пересоздаем базу (ТОЛЬКО ДЛЯ РАЗРАБОТКИ!)
    //context.Database.EnsureDeleted();
    context.Database.EnsureCreated();
    
    Console.WriteLine("База данных создана - Program.cs:43");
}

// Middleware pipeline
app.UseHttpsRedirection();
app.UseStaticFiles(); // Статические файлы
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// Маршрутизация
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();