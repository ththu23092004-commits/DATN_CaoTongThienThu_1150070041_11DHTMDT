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

        // ========== ĐĂNG KÝ ==========

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

            // Kiểm tra trùng email
            var tonTai = await _db.NguoiDung.AnyAsync(x => x.Email == model.Email);
            if (tonTai)
            {
                ModelState.AddModelError(string.Empty, "Email đã được sử dụng.");
                return View(model);
            }

            // Tạo user mới
            var nguoiDung = new NguoiDung
            {
                HoTen = model.HoTen,
                Email = model.Email,
                DienThoai = model.DienThoai,
                MatKhau = MatKhauHelper.BamMatKhau(model.MatKhau),
                VaiTro = "KhachHang",
                NgayTao = DateTime.Now
            };

            _db.NguoiDung.Add(nguoiDung);
            await _db.SaveChangesAsync();

            TempData["ThongBao"] = "Đăng ký thành công, hãy đăng nhập.";
            return RedirectToAction("DangNhap");
        }

        // ========== ĐĂNG NHẬP ==========

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

            var matKhauBam = MatKhauHelper.BamMatKhau(model.MatKhau);

            var user = await _db.NguoiDung
                .FirstOrDefaultAsync(x => x.Email == model.Email && x.MatKhau == matKhauBam);

            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Sai email hoặc mật khẩu.");
                return View(model);
            }
if (user.TrangThai == "BiKhoa")
{
    ModelState.AddModelError(string.Empty, "Tài khoản đã bị khóa. Vui lòng liên hệ quản trị viên.");
    return View(model);
}
            // Tạo claims chứa thông tin user + VaiTro
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.NguoiDungId.ToString()),
                new Claim(ClaimTypes.Name, user.HoTen),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.VaiTro ?? "KhachHang")
            };

            var identity = new ClaimsIdentity(
                claims, CookieAuthenticationDefaults.AuthenticationScheme);

            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal);

            // Điều hướng theo vai trò
            var role = user.VaiTro ?? "KhachHang";

            if (role == "Admin")
                return RedirectToAction("Index", "Admin");

            if (role == "ToChuc")
                return RedirectToAction("Index", "BanToChuc");

            // Mặc định: Khách hàng → xem danh sách sự kiện
            // 🔧 CHỈ SỬA DUY NHẤT DÒNG NÀY
            return RedirectToAction("Index", "KhachHang");
        }

        // ========== KHÔNG ĐỦ QUYỀN ==========

        public IActionResult KhongDuQuyen()
        {
            return Content("Bạn không có quyền truy cập trang này.");
        }

        // ========== ĐĂNG XUẤT ==========

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DangXuat()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }
    }
}
