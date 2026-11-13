using VeSuKienWeb.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using VeSuKienWeb.Data;
using VeSuKienWeb.Services;

namespace VeSuKienWeb.Controllers
{
    public class ThanhToanController : Controller
    {
        private readonly NguCanhSuKien _db;
        private readonly IVnPayService _vnPay;

        private int GetCurrentUserId()
        {
            var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(idStr))
            {
                throw new InvalidOperationException("Chưa đăng nhập.");
            }
            return int.Parse(idStr);
        }

        public ThanhToanController(NguCanhSuKien db, IVnPayService vnPay)
        {
            _db = db;
            _vnPay = vnPay;
        }

        // 🔵 Tạo URL thanh toán VNPAY (Sandbox thật)
        [Authorize(Roles = "KhachHang")]
        public async Task<IActionResult> VnPay(int donHangId)
        {
            var userId = GetCurrentUserId();

            var donHang = await _db.DonHang
                .FirstOrDefaultAsync(d => d.DonHangId == donHangId && d.NguoiDungId == userId);

            if (donHang == null)
                return NotFound("Không tìm thấy đơn hàng.");

            if (donHang.TongTien <= 0)
                return BadRequest("Đơn hàng không hợp lệ.");

            if (donHang.TrangThai == "ThanhCong")
                return RedirectToAction("ThanhToanThanhCong", "KhachHang", new { id = donHang.DonHangId });

            var orderDesc = $"Thanh toan don hang {donHang.DonHangId}";

            var url = _vnPay.CreatePaymentUrl(
                HttpContext,
                donHang.DonHangId,
                donHang.TongTien,
                orderDesc
            );

            Console.WriteLine("==================================");
            Console.WriteLine("VNPAY URL:");
            Console.WriteLine(url);
            Console.WriteLine("==================================");

            return Redirect(url); // ✔ Chỉ redirect VNPAY thật
        }

        // 🔵 Return URL
        [HttpGet]
        [Authorize(Roles = "KhachHang")]
        public async Task<IActionResult> VnPayReturn()
        {
            var userId = GetCurrentUserId();
            var response = _vnPay.ParseReturn(Request.Query);

            if (!int.TryParse(response.OrderId, out var donHangId))
            {
                ViewBag.Message = "Mã đơn hàng không hợp lệ.";
                return View("VnPayResult");
            }

            var donHang = await _db.DonHang
                .Include(d => d.Ves)
                    .ThenInclude(v => v.LoaiVe)
                .Include(d => d.Ves)
                    .ThenInclude(v => v.ChoNgoi)
                .FirstOrDefaultAsync(d => d.DonHangId == donHangId && d.NguoiDungId == userId);

            if (donHang == null)
            {
                ViewBag.Message = "Không tìm thấy đơn hàng trong hệ thống.";
                return View("VnPayResult");
            }

            if (response.IsSuccess)
            {
                if (donHang.TrangThai == "ThanhCong")
                    return RedirectToAction("ThanhToanThanhCong", "KhachHang", new { id = donHang.DonHangId });

                donHang.TrangThai = "ThanhCong";

                if (donHang.Ves != null)
                {
                    foreach (var ve in donHang.Ves)
                    {
                        ve.TrangThai = "DaThanhToan";
                        if (ve.ChoNgoiId.HasValue && ve.ChoNgoi != null)
                        {
                            ve.ChoNgoi.TrangThai = "DaDat";
                        }

                        _db.ThanhToan.Add(new ThanhToan
                        {
                            VeId = ve.VeId,
                            PhuongThuc = "VNPAY",
                            MaGiaoDich = response.TransactionNo,
                            TrangThai = "ThanhCong",
                            SoTien = ve.LoaiVe?.GiaVe ?? 0,
                            NgayThanhToan = DateTime.Now
                        });
                    }
                }

                await _db.SaveChangesAsync();
                return RedirectToAction("ThanhToanThanhCong", "KhachHang", new { id = donHang.DonHangId });
            }
            else
            {
                donHang.TrangThai = "Huy";
                await _db.SaveChangesAsync();

                ViewBag.Message = "Thanh toán thất bại: " + response.Message;
            }

            ViewBag.DonHangId = donHang.DonHangId;
            ViewBag.TransactionNo = response.TransactionNo;
            ViewBag.ResponseCode = response.ResponseCode;

            return View("VnPayResult");
        }
    }
}
