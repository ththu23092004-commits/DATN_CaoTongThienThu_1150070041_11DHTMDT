
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VeSuKienWeb.Data;
using VeSuKienWeb.Helpers;
using VeSuKienWeb.Models;
using VeSuKienWeb.Models.TaiKhoan;

namespace VeSuKienWeb.Controllers
{
    public class TaiKhoanController : Controller
    {
        private readonly NguCanhSuKien _db;

        public TaiKhoanController(NguCanhSuKien db)
        {
            _db = db;
        }

        // ================= ĐĂNG KÝ =================

        [HttpGet]
        public IActionResult DangKy()
        {
            return View(new DangKyYeuCau());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DangKy(DangKyYeuCau model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var email = model.Email.Trim();

            var tonTai = await _db.NguoiDung
                .AnyAsync(x => x.Email == email);

            if (tonTai)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Email đã được sử dụng.");

                return View(model);
            }

            var nguoiDung = new NguoiDung
            {
                HoTen = model.HoTen.Trim(),
                Email = email,
                DienThoai = model.DienThoai,
                MatKhau = MatKhauHelper.BamMatKhau(model.MatKhau),
                VaiTro = "KhachHang",
                NgayTao = DateTime.Now,
                TrangThai = "HoatDong"
            };

            _db.NguoiDung.Add(nguoiDung);
            await _db.SaveChangesAsync();

            TempData["ThongBao"] =
                "Đăng ký thành công, hãy đăng nhập để thiết lập sở thích sự kiện.";

            return RedirectToAction("DangNhap");
        }

        // ================= ĐĂNG NHẬP =================

        [HttpGet]
        public IActionResult DangNhap()
        {
            return View(new DangNhapYeuCau());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DangNhap(DangNhapYeuCau model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var email = model.Email.Trim();

            var matKhauBam =
                MatKhauHelper.BamMatKhau(model.MatKhau);

            var user = await _db.NguoiDung
                .FirstOrDefaultAsync(x =>
                    x.Email == email &&
                    x.MatKhau == matKhauBam);

            if (user == null)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Sai email hoặc mật khẩu.");

                return View(model);
            }

            if (user.TrangThai == "BiKhoa")
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Tài khoản đã bị khóa. Vui lòng liên hệ quản trị viên.");

                return View(model);
            }

            var claims = new List<Claim>
            {
                new Claim(
                    ClaimTypes.NameIdentifier,
                    user.NguoiDungId.ToString()),

                new Claim(ClaimTypes.Name, user.HoTen),

                new Claim(ClaimTypes.Email, user.Email),

                new Claim(
                    ClaimTypes.Role,
                    user.VaiTro ?? "KhachHang")
            };

            var identity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme);

            var principal = new ClaimsPrincipal(identity);

            // CẤU HÌNH GHI NHỚ ĐĂNG NHẬP
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = model.GhiNhoDangNhap,
                AllowRefresh = true
            };

            // Nếu người dùng tích ghi nhớ đăng nhập:
            // Cookie có thời hạn 30 ngày.
            if (model.GhiNhoDangNhap)
            {
                authProperties.ExpiresUtc =
                    DateTimeOffset.UtcNow.AddDays(30);
            }

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                authProperties);

            var role = user.VaiTro ?? "KhachHang";

            if (role == "Admin")
                return RedirectToAction("Index", "Admin");

            if (role == "ToChuc")
                return RedirectToAction("Index", "BanToChuc");

            if (role == "KhachHang")
            {
                bool daHoanThanhKhaoSat =
                    await _db.HoSoSoThich.AnyAsync(x =>
                        x.NguoiDungId == user.NguoiDungId &&
                        x.DaHoanThanhKhaoSat);

                if (!daHoanThanhKhaoSat)
                {
                    return RedirectToAction(
                        "Index",
                        "KhaoSatSoThich");
                }

                return RedirectToAction(
                    "Index",
                    "KhachHang");
            }

            return RedirectToAction("Index", "Home");
        }

        // ================= KHÔNG ĐỦ QUYỀN =================

        public IActionResult KhongDuQuyen()
        {
            return Content("Bạn không có quyền truy cập trang này.");
        }

        // ================= ĐĂNG XUẤT =================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DangXuat()
        {
            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme);

            return RedirectToAction("Index", "Home");
        }
    }
}
