using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using VeSuKienWeb.Data;
using VeSuKienWeb.Config;
using VeSuKienWeb.Services;

using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
var builder = WebApplication.CreateBuilder(args);

// =======================
//  CẤU HÌNH DATABASE
// =======================
builder.Services.AddDbContext<NguCanhSuKien>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));


// =======================
//  CẤU HÌNH VNPAY
//  (Đọc từ appsettings.json -> "VnPay")
// =======================
builder.Services.Configure<VnPaySettings>(builder.Configuration.GetSection("VnPay"));
builder.Services.AddScoped<IVnPayService, VnPayService>();


// =======================
//  XÁC THỰC COOKIE
// =======================
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/TaiKhoan/DangNhap";
        options.LogoutPath = "/TaiKhoan/DangXuat";
        options.AccessDeniedPath = "/TaiKhoan/KhongDuQuyen";

        // Quan trọng: tránh lỗi loop login khi VNPAY redirect
        options.Cookie.HttpOnly = true;
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();


// =======================
//  MVC
// =======================
builder.Services.AddControllersWithViews();
builder.Services.AddScoped<GoiYSuKienService>();


builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = 429;

    options.AddFixedWindowLimiter("kham-pha", limiter =>
    {
        limiter.PermitLimit = 8;
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.QueueLimit = 0;
        limiter.AutoReplenishment = true;
    });
});


builder.Services.AddControllersWithViews();
builder.Services.AddScoped<GoiYSuKienService>();

// Khám phá sự kiện thông minh
builder.Services.AddHttpClient<AiIntentService>();

// Tìm kiếm và xếp hạng Top-3 từ SQL Server
builder.Services.AddScoped<KhamPhaSuKienService>();
// =======================
//  BUILD APP
// =======================

builder.Services.AddHostedService<GiaiPhongGheHetHanService>();
var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseRateLimiter();
// Authentication luôn trước Authorization
app.UseAuthentication();
app.UseAuthorization();


// =======================
// ROUTE MẶC ĐỊNH
// =======================
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");


// =======================
// RUN APP
// =======================
app.Run();
