
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Threading.RateLimiting;

using VeSuKienWeb.Data;
using VeSuKienWeb.Config;
using VeSuKienWeb.Services;

var builder = WebApplication.CreateBuilder(args);

// =====================================================
// 1. CẤU HÌNH DATABASE
// =====================================================

builder.Services.AddDbContext<NguCanhSuKien>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString(
            "DefaultConnection"
        )
    )
);

// =====================================================
// 2. CẤU HÌNH VNPAY
// =====================================================

builder.Services.Configure<VnPaySettings>(
    builder.Configuration.GetSection("VnPay")
);

builder.Services.AddScoped<IVnPayService, VnPayService>();

// =====================================================
// 3. XÁC THỰC COOKIE
// =====================================================

builder.Services
    .AddAuthentication(
        CookieAuthenticationDefaults.AuthenticationScheme
    )
    .AddCookie(options =>
    {
        options.LoginPath = "/TaiKhoan/DangNhap";

        options.LogoutPath = "/TaiKhoan/DangXuat";

        options.AccessDeniedPath =
            "/TaiKhoan/KhongDuQuyen";

        options.Cookie.HttpOnly = true;

        options.SlidingExpiration = true;

        // Kiểm tra trạng thái người dùng trong CSDL
        // khi sử dụng cookie đăng nhập.
        options.Events = new CookieAuthenticationEvents
        {
            OnValidatePrincipal = async context =>
            {
                var principal = context.Principal;

                if (principal?.Identity?.IsAuthenticated != true)
                {
                    context.RejectPrincipal();
                    return;
                }

                // Lấy mã người dùng từ cookie
                var userIdText = principal.FindFirstValue(
                    ClaimTypes.NameIdentifier
                );

                if (!int.TryParse(userIdText, out int userId))
                {
                    context.RejectPrincipal();

                    await context.HttpContext.SignOutAsync(
                        CookieAuthenticationDefaults
                            .AuthenticationScheme
                    );

                    return;
                }

                // Lấy DbContext cho yêu cầu hiện tại
                var db = context.HttpContext.RequestServices
                    .GetRequiredService<NguCanhSuKien>();

                // Chỉ lấy các thông tin cần thiết
                var user = await db.NguoiDung
                    .AsNoTracking()
                    .Where(x => x.NguoiDungId == userId)
                    .Select(x => new
                    {
                        x.NguoiDungId,
                        x.TrangThai,
                        x.VaiTro
                    })
                    .FirstOrDefaultAsync();

                // Nếu tài khoản đã bị xóa
                if (user == null)
                {
                    context.RejectPrincipal();

                    await context.HttpContext.SignOutAsync(
                        CookieAuthenticationDefaults
                            .AuthenticationScheme
                    );

                    return;
                }

                // Nếu tài khoản không còn hoạt động
                if (user.TrangThai != "HoatDong")
                {
                    context.RejectPrincipal();

                    await context.HttpContext.SignOutAsync(
                        CookieAuthenticationDefaults
                            .AuthenticationScheme
                    );

                    return;
                }

                // Kiểm tra vai trò đang lưu trong cookie
                var cookieRole = principal.FindFirstValue(
                    ClaimTypes.Role
                );

                var databaseRole =
                    user.VaiTro ?? "KhachHang";

                // Nếu Admin đã đổi vai trò tài khoản,
                // bắt buộc đăng nhập lại để nhận quyền mới.
                if (cookieRole != databaseRole)
                {
                    context.RejectPrincipal();

                    await context.HttpContext.SignOutAsync(
                        CookieAuthenticationDefaults
                            .AuthenticationScheme
                    );

                    return;
                }

                // Tài khoản hợp lệ:
                // tiếp tục sử dụng cookie hiện tại.
            }
        };
    });

// =====================================================
// 4. PHÂN QUYỀN
// =====================================================

builder.Services.AddAuthorization();

// =====================================================
// 5. MVC
// =====================================================

builder.Services.AddControllersWithViews();

// =====================================================
// 6. DỊCH VỤ GỢI Ý SỰ KIỆN
// =====================================================

builder.Services.AddScoped<GoiYSuKienService>();

// =====================================================
// 7. KHÁM PHÁ SỰ KIỆN THÔNG MINH
// =====================================================

builder.Services.AddHttpClient<AiIntentService>();

// Tìm kiếm và xếp hạng Top-3 từ SQL Server
builder.Services.AddScoped<KhamPhaSuKienService>();

// =====================================================
// 8. RATE LIMITER
// =====================================================

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = 429;

    options.AddFixedWindowLimiter(
        "kham-pha",
        limiter =>
        {
            limiter.PermitLimit = 8;

            limiter.Window =
                TimeSpan.FromMinutes(1);

            limiter.QueueLimit = 0;

            limiter.AutoReplenishment = true;
        }
    );
});

// =====================================================
// 9. DỊCH VỤ GIẢI PHÓNG GHẾ HẾT HẠN
// =====================================================

builder.Services.AddHostedService<
    GiaiPhongGheHetHanService
>();

// =====================================================
// 10. BUILD APP
// =====================================================

var app = builder.Build();

// =====================================================
// 11. MIDDLEWARE
// =====================================================

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.UseRateLimiter();

// Authentication phải chạy trước Authorization
app.UseAuthentication();

app.UseAuthorization();

// =====================================================
// 12. ROUTE MẶC ĐỊNH
// =====================================================

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
);

// =====================================================
// 13. RUN APP
// =====================================================

app.Run();
