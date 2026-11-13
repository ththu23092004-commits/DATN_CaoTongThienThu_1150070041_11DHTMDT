
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VeSuKienWeb.Data;
using VeSuKienWeb.Models;
using VeSuKienWeb.Services;

namespace VeSuKienWeb.Controllers
{
    public class ThanhToanController : Controller
    {
        private readonly NguCanhSuKien _db;
        private readonly IVnPayService _vnPay;
        private readonly ILogger<ThanhToanController> _logger;

        public ThanhToanController(
            NguCanhSuKien db,
            IVnPayService vnPay,
            ILogger<ThanhToanController> logger)
        {
            _db = db;
            _vnPay = vnPay;
            _logger = logger;
        }

        private int GetCurrentUserId()
        {
            var idStr = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (!int.TryParse(idStr, out int userId))
            {
                throw new InvalidOperationException(
                    "Chưa đăng nhập.");
            }

            return userId;
        }

        // ===================================================
        // TẠO YÊU CẦU THANH TOÁN VNPAY SANDBOX
        // ===================================================

        [Authorize(Roles = "KhachHang")]
        [HttpGet]
        public async Task<IActionResult> VnPay(int donHangId)
        {
            int userId = GetCurrentUserId();

            var donHang = await _db.DonHang
                .Include(d => d.Ves)
                .FirstOrDefaultAsync(d =>
                    d.DonHangId == donHangId &&
                    d.NguoiDungId == userId);

            if (donHang == null)
                return NotFound("Không tìm thấy đơn hàng.");

            if (donHang.TrangThai == "ThanhCong")
            {
                return RedirectToAction(
                    "ThanhToanThanhCong",
                    "KhachHang",
                    new { id = donHangId });
            }

            if (donHang.TrangThai != "DangXuLy" ||
                donHang.TongTien <= 0 ||
                !donHang.Ves.Any() ||
                donHang.Ves.Any(v =>
                    v.TrangThai != "ChuaThanhToan"))
            {
                TempData["ThongBao"] =
                    "Đơn hàng không còn hợp lệ để thanh toán.";

                return RedirectToAction(
                    "VeCuaToi", "KhachHang");
            }

            var gheIds = donHang.Ves
                .Where(v => v.ChoNgoiId.HasValue)
                .Select(v => v.ChoNgoiId!.Value)
                .Distinct()
                .ToList();

            if (gheIds.Any())
            {
                var gheList = await _db.SoDoChoNgoi
                    .AsNoTracking()
                    .Where(g => gheIds.Contains(g.ChoNgoiId))
                    .ToListAsync();

                bool gheKhongHopLe =
                    gheList.Count != gheIds.Count ||
                    gheList.Any(g =>
                        g.TrangThai != "Trong" ||
                        g.DonHangGiuId != donHangId ||
                        !g.ThoiGianHetHanGiu.HasValue ||
                        g.ThoiGianHetHanGiu.Value <= DateTime.Now);

                if (gheKhongHopLe)
                {
                    TempData["ThongBao"] =
                        "Ghế đã hết thời gian giữ hoặc không còn hợp lệ.";

                    return RedirectToAction(
                        "ThanhToan",
                        "KhachHang",
                        new { id = donHangId });
                }
            }

            try
            {
                var orderDesc =
                    $"Thanh toan don hang {donHangId}";

                string url = _vnPay.CreatePaymentUrl(
                    HttpContext,
                    donHangId,
                    donHang.TongTien,
                    orderDesc);

                return Redirect(url);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Loi tao thanh toan VNPAY cho don {DonHangId}",
                    donHangId);

                TempData["ThongBao"] =
                    "Không thể kết nối VNPay. Vui lòng thử lại.";

                return RedirectToAction(
                    "ThanhToan",
                    "KhachHang",
                    new { id = donHangId });
            }
        }

        // ===================================================
        // XỬ LÝ KẾT QUẢ THANH TOÁN
        // DÙNG CHUNG CHO RETURN VÀ IPN
        // ===================================================

        private async Task<(string Code, string Message)>
            XuLyKetQuaAsync(VnPayReturnResult response)
        {
            if (!response.IsValidSignature)
            {
                return ("97", "Invalid signature");
            }

            if (!int.TryParse(response.OrderId, out int donHangId) ||
                donHangId <= 0)
            {
                return ("01", "Order not found");
            }

            await using var transaction =
                await _db.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable);

            try
            {
                var donHang = await _db.DonHang
                    .Include(d => d.Ves)
                        .ThenInclude(v => v.LoaiVe)
                    .FirstOrDefaultAsync(d =>
                        d.DonHangId == donHangId);

                if (donHang == null)
                {
                    await transaction.RollbackAsync();
                    return ("01", "Order not found");
                }

                if (!response.Amount.HasValue ||
                    donHang.TongTien != response.Amount.Value)
                {
                    await transaction.RollbackAsync();

                    _logger.LogWarning(
                        "VNPAY sai so tien. Don={DonHangId}",
                        donHangId);

                    return ("04", "Invalid amount");
                }

                if (donHang.TrangThai == "ThanhCong")
                {
                    await transaction.RollbackAsync();

                    return ("02", "Order already confirmed");
                }

                // Không hủy hoặc đổi trạng thái đơn chỉ vì
                // nhận kết quả thanh toán thất bại.
                if (!response.IsSuccess)
                {
                    await transaction.RollbackAsync();

                    return ("00", "Payment result received");
                }

                // Đơn đã hủy không được tự động khôi phục.
                if (donHang.TrangThai != "DangXuLy")
                {
                    await transaction.RollbackAsync();

                    _logger.LogWarning(
                        "VNPAY thanh cong nhung don {DonHangId} da {Status}. MaGD={TransactionNo}",
                        donHangId,
                        donHang.TrangThai,
                        response.TransactionNo);

                    return ("99", "Order is no longer payable");
                }

                var ves = donHang.Ves.ToList();

                if (!ves.Any() ||
                    ves.Any(v =>
                        v.TrangThai != "ChuaThanhToan"))
                {
                    await transaction.RollbackAsync();

                    return ("99", "Tickets are not payable");
                }

                if (string.IsNullOrWhiteSpace(
                    response.TransactionNo))
                {
                    await transaction.RollbackAsync();

                    return ("99", "Missing transaction number");
                }

                var veIds = ves.Select(v => v.VeId).ToList();

                bool giaoDichDaTonTai =
                    await _db.ThanhToan.AnyAsync(t =>
                        t.PhuongThuc == "VNPAY" &&
                        t.MaGiaoDich == response.TransactionNo);

                if (giaoDichDaTonTai)
                {
                    await transaction.RollbackAsync();

                    return ("02", "Transaction already recorded");
                }

                var gheIds = ves
                    .Where(v => v.ChoNgoiId.HasValue)
                    .Select(v => v.ChoNgoiId!.Value)
                    .Distinct()
                    .ToList();

                var gheList = await _db.SoDoChoNgoi
                    .Where(g => gheIds.Contains(g.ChoNgoiId))
                    .ToListAsync();

                if (gheIds.Any())
                {
                    bool gheKhongHopLe =
                        gheList.Count != gheIds.Count ||
                        gheList.Any(g =>
                            g.TrangThai != "Trong" ||
                            g.DonHangGiuId != donHangId ||
                            !g.ThoiGianHetHanGiu.HasValue ||
                            g.ThoiGianHetHanGiu.Value <= DateTime.Now);

                    if (gheKhongHopLe)
                    {
                        await transaction.RollbackAsync();

                        _logger.LogWarning(
                            "VNPAY thanh cong nhung ghe khong hop le. Don={DonHangId}, MaGD={TransactionNo}",
                            donHangId,
                            response.TransactionNo);

                        return ("99", "Seats are no longer reserved");
                    }
                }

                foreach (var ghe in gheList)
                {
                    ghe.TrangThai = "DaDat";
                    ghe.DonHangGiuId = null;
                    ghe.ThoiGianHetHanGiu = null;
                }

                foreach (var ve in ves)
                {
                    ve.TrangThai = "DaThanhToan";

                    _db.ThanhToan.Add(new ThanhToan
                    {
                        VeId = ve.VeId,
                        PhuongThuc = "VNPAY",
                        MaGiaoDich = response.TransactionNo,
                        TrangThai = "ThanhCong",
                        NgayThanhToan = DateTime.Now,
                        SoTien = ve.LoaiVe?.GiaVe ?? 0m
                    });
                }

                donHang.TrangThai = "ThanhCong";

                await _db.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation(
                    "VNPAY thanh cong. Don={DonHangId}, MaGD={TransactionNo}",
                    donHangId,
                    response.TransactionNo);

                return ("00", "Confirm Success");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                _logger.LogError(
                    ex,
                    "Loi dong bo VNPAY cho don {DonHangId}",
                    donHangId);

                return ("99", "System error");
            }
        }

        // ===================================================
        // RETURN URL - HIỂN THỊ KẾT QUẢ CHO KHÁCH HÀNG
        // ===================================================

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> VnPayReturn()
        {
            var response = _vnPay.ParseReturn(Request.Query);

            var ketQua = await XuLyKetQuaAsync(response);

            if (ketQua.Code == "00" && response.IsSuccess &&
                int.TryParse(response.OrderId, out int donHangId))
            {
                var userIdText = User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

                if (int.TryParse(userIdText, out int userId))
                {
                    bool laChuDon = await _db.DonHang
                        .AsNoTracking()
                        .AnyAsync(d =>
                            d.DonHangId == donHangId &&
                            d.NguoiDungId == userId);

                    if (laChuDon)
                    {
                        return RedirectToAction(
                            "ThanhToanThanhCong",
                            "KhachHang",
                            new { id = donHangId });
                    }
                }
            }

            ViewBag.DonHangId = response.OrderId;
            ViewBag.TransactionNo = response.TransactionNo;
            ViewBag.ResponseCode = response.ResponseCode;

            ViewBag.Message = ketQua.Code switch
            {
                "97" => "Không xác thực được chữ ký VNPay.",
                "04" => "Số tiền thanh toán không khớp.",
                "01" => "Không tìm thấy đơn hàng.",
                "02" => "Giao dịch đã được hệ thống ghi nhận trước đó.",
                "99" => "VNPay đã phản hồi nhưng đơn hàng chưa thể xác nhận. Vui lòng kiểm tra trạng thái vé, không thanh toán lại ngay.",
                _ => response.IsSuccess
                    ? "Thanh toán thành công tại VNPay."
                    : "Giao dịch chưa thành công: " + response.Message
            };

            return View("VnPayResult");
        }

        // ===================================================
        // IPN - VNPAY THÔNG BÁO TRỰC TIẾP CHO MÁY CHỦ
        // ===================================================

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> VnPayIpn()
        {
            try
            {
                var response = _vnPay.ParseReturn(Request.Query);

                var ketQua = await XuLyKetQuaAsync(response);

                return Json(new
                {
                    RspCode = ketQua.Code,
                    Message = ketQua.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Loi xu ly VNPAY IPN");

                return Json(new
                {
                    RspCode = "99",
                    Message = "System error"
                });
            }
        }
    }
}
