using Microsoft.EntityFrameworkCore;
using uchet.Data;
using uchet.Services;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Добавляем контекст базы данных
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Добавляем аутентификацию
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
    });

// Добавляем IHttpClientFactory для сервиса генерации документов
builder.Services.AddHttpClient();
builder.Services.AddScoped<BarcodeDocxService>();
var app = builder.Build();

// Настройка URL для Kestrel
app.Urls.Add("http://localhost:5251");

// Проверяем и создаем таблицы при необходимости
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    context.Database.EnsureCreated();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication(); // Добавляем аутентификацию
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
