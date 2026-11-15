
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
using Microsoft.Extensions.Options;
using VeSuKienWeb.Config;
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
        private readonly VnPaySettings _vnPaySettings;

        public ThanhToanController(
            NguCanhSuKien db,
            IVnPayService vnPay,
            ILogger<ThanhToanController> logger,
            IOptions<VnPaySettings> vnPayOptions)
        {
            _db = db;
            _vnPay = vnPay;
            _logger = logger;
            _vnPaySettings = vnPayOptions.Value;
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

        private async Task HuyDonHetHanAsync(DonHang donHang)
        {
            if (donHang.TrangThai == "ThanhCong")
                return;

            foreach (var ve in donHang.Ves)
            {
                if (ve.TrangThai == "ChuaThanhToan")
                    ve.TrangThai = "Huy";
            }

            donHang.TrangThai = "Huy";

            var gheList = await _db.SoDoChoNgoi
                .Where(g => g.DonHangGiuId == donHang.DonHangId)
                .ToListAsync();

            foreach (var ghe in gheList)
            {
                ghe.DonHangGiuId = null;
                ghe.ThoiGianHetHanGiu = null;
            }
        }

 
private async Task<bool> KiemTraVaKhoaSoLuongVeAsync(
    List<Ve> danhSachVe,
    int donHangId,
    DateTime now)
{
    var nhomLoaiVe = danhSachVe
        .GroupBy(v => v.LoaiVeId)
        .OrderBy(g => g.Key)
        .ToList();

    foreach (var nhom in nhomLoaiVe)
    {
        var loaiVe = await _db.LoaiVe
            .FromSqlInterpolated($@"
                SELECT *
                FROM dbo.LoaiVe WITH (UPDLOCK, HOLDLOCK)
                WHERE LoaiVeId = {nhom.Key}
            ")
            .AsNoTracking()
            .FirstOrDefaultAsync();

        if (loaiVe == null)
            return false;

        var thoiDiemHetHan = now.AddMinutes(-5);

        int soLuongDaBan = await _db.Ve.CountAsync(v =>
            v.LoaiVeId == nhom.Key &&
            (v.TrangThai == "DaThanhToan" ||
             v.TrangThai == "DaCheckIn"));

        int soLuongDangGiu = await (
            from ve in _db.Ve
            join don in _db.DonHang
                on ve.DonHangId equals (int?)don.DonHangId
                into donHangJoin
            from don in donHangJoin.DefaultIfEmpty()
            where ve.LoaiVeId == nhom.Key
                && ve.DonHangId != donHangId
                && ve.TrangThai == "ChuaThanhToan"
                && (
                    ve.DonHangId == null ||
                    (don != null &&
                     don.TrangThai == "DangXuLy" &&
                     don.NgayDat > thoiDiemHetHan)
                )
            select ve.VeId
        ).CountAsync();

        if (soLuongDaBan + soLuongDangGiu + nhom.Count()
            > loaiVe.SoLuong)
        {
            return false;
        }
    }

    return true;
}



private async Task<bool> KiemTraVaKhoaGheAsync(
    List<Ve> danhSachVe,
    int donHangId)
{
    var danhSachVeCoGhe = danhSachVe
        .Where(v => v.ChoNgoiId.HasValue)
        .OrderBy(v => v.ChoNgoiId)
        .ToList();

    foreach (var ve in danhSachVeCoGhe)
    {
        int choNgoiId = ve.ChoNgoiId!.Value;

        var ghe = await _db.SoDoChoNgoi
            .FromSqlInterpolated($@"
                SELECT *
                FROM dbo.SoDoChoNgoi WITH (UPDLOCK, HOLDLOCK)
                WHERE ChoNgoiId = {choNgoiId}
            ")
            .FirstOrDefaultAsync();

        if (ghe == null ||
            ghe.TrangThai != "Trong" ||
            (ghe.DonHangGiuId.HasValue &&
             ghe.DonHangGiuId.Value != donHangId))
        {
            return false;
        }

        ghe.TrangThai = "DaDat";
        ghe.DonHangGiuId = null;
        ghe.ThoiGianHetHanGiu = null;
    }

    return true;
}


        private async Task GhiNhanGiaoDichCanDoiSoatAsync(
            List<Ve> danhSachVe,
            string maGiaoDich,
            DateTime ngayThanhToan)
        {
            foreach (var ve in danhSachVe)
            {
                _db.ThanhToan.Add(new ThanhToan
                {
                    VeId = ve.VeId,
                    PhuongThuc = "VNPAY",
                    MaGiaoDich = maGiaoDich,
                    TrangThai = "CanDoiSoat",
                    NgayThanhToan = ngayThanhToan,
                    SoTien = ve.LoaiVe?.GiaVe ?? 0m
                });
            }

            await _db.SaveChangesAsync();
        }

        [Authorize(Roles = "KhachHang")]
        [HttpGet]
        public async Task<IActionResult> VnPay(int donHangId)
        {
            int userId = GetCurrentUserId();

            await using var transaction =
                await _db.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable);

            try
            {
                var donHang = await _db.DonHang
                    .FromSqlInterpolated($@"
                        SELECT *
                        FROM dbo.DonHang WITH (UPDLOCK, HOLDLOCK)
                        WHERE DonHangId = {donHangId}
                          AND NguoiDungId = {userId}
                    ")
                    .Include(d => d.Ves)
                    .FirstOrDefaultAsync();

                if (donHang == null)
                {
                    await transaction.RollbackAsync();
                    return NotFound("Không tìm thấy đơn hàng.");
                }

                if (donHang.TrangThai == "ThanhCong")
                {
                    await transaction.RollbackAsync();

                    return RedirectToAction(
                        "ThanhToanThanhCong",
                        "KhachHang",
                        new { id = donHangId });
                }

                if (donHang.TrangThai != "DangXuLy")
                {
                    await transaction.RollbackAsync();

                    TempData["ThongBao"] =
                        "Đơn hàng đã bị hủy hoặc không còn hiệu lực.";

                    return RedirectToAction(
                        "VeCuaToi", "KhachHang");
                }

                var now = DateTime.Now;
                var expireAt = donHang.NgayDat.AddMinutes(5);

                if (now >= expireAt)
                {
                    await HuyDonHetHanAsync(donHang);

                    await _db.SaveChangesAsync();
                    await transaction.CommitAsync();

                    TempData["ThongBao"] =
                        "Đơn hàng đã hết thời gian thanh toán 5 phút. Vui lòng đặt vé lại.";

                    return RedirectToAction(
                        "VeCuaToi", "KhachHang");
                }

                if (donHang.TongTien <= 0 ||
                    !donHang.Ves.Any() ||
                    donHang.Ves.Any(v =>
                        v.TrangThai != "ChuaThanhToan"))
                {
                    await transaction.RollbackAsync();

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
                        .Where(g => gheIds.Contains(g.ChoNgoiId))
                        .ToListAsync();

                    bool gheKhongHopLe =
                        gheList.Count != gheIds.Count ||
                        gheList.Any(g =>
                            g.TrangThai != "Trong" ||
                            g.DonHangGiuId != donHangId ||
                            !g.ThoiGianHetHanGiu.HasValue ||
                            g.ThoiGianHetHanGiu.Value <= now);

                    if (gheKhongHopLe)
                    {
                        await transaction.RollbackAsync();

                        TempData["ThongBao"] =
                            "Ghế đã hết thời gian giữ hoặc không còn hợp lệ.";

                        return RedirectToAction(
                            "ThanhToan",
                            "KhachHang",
                            new { id = donHangId });
                    }
                }

                var orderDesc =
                    $"Thanh toan don hang {donHangId}";

                string url = _vnPay.CreatePaymentUrl(
                    HttpContext,
                    donHangId,
                    donHang.TongTien,
                    orderDesc,
                    expireAt);

                await transaction.CommitAsync();

                return Redirect(url);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                _logger.LogError(
                    ex,
                    "Lỗi tạo thanh toán VNPay cho đơn {DonHangId}",
                    donHangId);

                TempData["ThongBao"] =
                    "Không thể kết nối VNPay. Vui lòng thử lại.";

                return RedirectToAction(
                    "ThanhToan",
                    "KhachHang",
                    new { id = donHangId });
            }
        }


private async Task<(string Code, string Message)>
    XuLyKetQuaAsync(VnPayReturnResult response)
{
    if (!response.IsValidSignature ||
        string.IsNullOrWhiteSpace(_vnPaySettings.TmnCode) ||
        !string.Equals(
            response.TmnCode,
            _vnPaySettings.TmnCode,
            StringComparison.Ordinal))
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
            .FromSqlInterpolated($@"
                SELECT *
                FROM dbo.DonHang WITH (UPDLOCK, HOLDLOCK)
                WHERE DonHangId = {donHangId}
            ")
            .Include(d => d.Ves)
                .ThenInclude(v => v.LoaiVe)
            .FirstOrDefaultAsync();

        if (donHang == null)
        {
            await transaction.RollbackAsync();
            return ("01", "Order not found");
        }

        if (!response.Amount.HasValue ||
            donHang.TongTien != response.Amount.Value)
        {
            await transaction.RollbackAsync();
            return ("04", "Invalid amount");
        }


if (!response.IsSuccess)
{
    if (donHang.TrangThai == "DangXuLy")
    {
        await HuyDonHetHanAsync(donHang);

        await _db.SaveChangesAsync();
        await transaction.CommitAsync();
    }
    else
    {
        await transaction.RollbackAsync();
    }

    return ("00", "Payment failed; order canceled");
}


        if (string.IsNullOrWhiteSpace(response.TransactionNo))
        {
            await transaction.RollbackAsync();
            return ("99", "Missing transaction number");
        }

        var giaoDichDaTonTai = await (
            from tt in _db.ThanhToan
            join ve in _db.Ve
                on tt.VeId equals ve.VeId
            where tt.PhuongThuc == "VNPAY"
                && tt.MaGiaoDich == response.TransactionNo
            select new
            {
                ve.DonHangId,
                tt.TrangThai
            }
        ).ToListAsync();

        if (giaoDichDaTonTai.Any())
        {
            bool canDoiSoat = giaoDichDaTonTai.Any(x =>
                x.DonHangId == donHangId &&
                x.TrangThai == "CanDoiSoat");

            await transaction.RollbackAsync();

            return canDoiSoat
                ? ("05", "Payment recorded for reconciliation")
                : ("02", "Transaction already recorded");
        }


if (donHang.TrangThai == "ThanhCong")
{
    var vesDaThanhToan = donHang.Ves.ToList();

    if (!vesDaThanhToan.Any())
    {
        await transaction.RollbackAsync();
        return ("99", "Paid order has no tickets");
    }

    await GhiNhanGiaoDichCanDoiSoatAsync(
        vesDaThanhToan,
        response.TransactionNo,
        response.PayDate ?? DateTime.Now
    );

    await transaction.CommitAsync();

    _logger.LogWarning(
        "Phat hien giao dich thanh toan lan hai. Don={DonHangId}, MaGD={MaGD}",
        donHangId,
        response.TransactionNo);

    return (
        "05",
        "Additional payment recorded for reconciliation"
    );
}


        var ves = donHang.Ves.ToList();

        if (!ves.Any() ||
            ves.Any(v =>
                v.TrangThai != "ChuaThanhToan" &&
                v.TrangThai != "Huy"))
        {
            await transaction.RollbackAsync();
            return ("99", "Tickets are not payable");
        }

        var now = DateTime.Now;
        var expireAt = donHang.NgayDat.AddMinutes(5);
        var ngayThanhToan = response.PayDate ?? now;

        bool thanhToanDungHan =
            response.PayDate.HasValue &&
            response.PayDate.Value >= donHang.NgayDat.AddSeconds(-30) &&
            response.PayDate.Value <= expireAt;

        if (!thanhToanDungHan)
        {
            if (donHang.TrangThai == "DangXuLy")
            {
                await HuyDonHetHanAsync(donHang);
            }

            await GhiNhanGiaoDichCanDoiSoatAsync(
                ves,
                response.TransactionNo,
                ngayThanhToan);

            await transaction.CommitAsync();

            _logger.LogWarning(
                "VNPay thanh toan ngoai han. Don={DonHangId}, MaGD={MaGD}",
                donHangId,
                response.TransactionNo);

            return (
                "05",
                "Payment recorded for reconciliation"
            );
        }

        if (donHang.TrangThai != "DangXuLy" &&
            donHang.TrangThai != "Huy")
        {
            await GhiNhanGiaoDichCanDoiSoatAsync(
                ves,
                response.TransactionNo,
                ngayThanhToan);

            await transaction.CommitAsync();

            return (
                "05",
                "Order requires reconciliation"
            );
        }

        bool soLuongVeHopLe =
            await KiemTraVaKhoaSoLuongVeAsync(
                ves,
                donHangId,
                now);

        bool gheHopLe = false;

        if (soLuongVeHopLe)
        {
            gheHopLe = await KiemTraVaKhoaGheAsync(
                ves,
                donHangId);
        }

        if (!soLuongVeHopLe || !gheHopLe)
        {
            if (donHang.TrangThai == "DangXuLy")
            {
                await HuyDonHetHanAsync(donHang);
            }

            await GhiNhanGiaoDichCanDoiSoatAsync(
                ves,
                response.TransactionNo,
                ngayThanhToan);

            await transaction.CommitAsync();

            _logger.LogWarning(
                "VNPay da thu tien nhung ve khong con kha dung. Don={DonHangId}, MaGD={MaGD}",
                donHangId,
                response.TransactionNo);

            return (
                "05",
                "Payment recorded for reconciliation"
            );
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
                NgayThanhToan = ngayThanhToan,
                SoTien = ve.LoaiVe?.GiaVe ?? 0m
            });
        }

        donHang.TrangThai = "ThanhCong";

        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        _logger.LogInformation(
            "VNPay thanh toan thanh cong. Don={DonHangId}, MaGD={MaGD}",
            donHangId,
            response.TransactionNo);

        return ("00", "Confirm Success");
    }
    catch (Exception ex)
    {
        await transaction.RollbackAsync();

        _logger.LogError(
            ex,
            "Loi dong bo VNPay cho don {DonHangId}",
            donHangId);

        return ("99", "System error");
    }
}


        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> VnPayReturn()
        {
            var response = _vnPay.ParseReturn(Request.Query);

            var ketQua = await XuLyKetQuaAsync(response);

            bool thanhToanThanhCong =
                response.ResponseCode == "00" &&
                response.TransactionStatus == "00";

            if ((ketQua.Code == "00" || ketQua.Code == "02") &&
                thanhToanThanhCong &&
                int.TryParse(response.OrderId, out int donHangId))
            {
                var userIdText = User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

                if (int.TryParse(userIdText, out int userId))
                {
                    var donHang = await _db.DonHang
                        .AsNoTracking()
                        .Where(d =>
                            d.DonHangId == donHangId &&
                            d.NguoiDungId == userId)
                        .Select(d => new
                        {
                            d.DonHangId,
                            d.TrangThai
                        })
                        .FirstOrDefaultAsync();

                    if (donHang != null &&
                        donHang.TrangThai == "ThanhCong")
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
                "97" => "Không xác thực được chữ ký hoặc mã website VNPay.",
                "04" => "Số tiền thanh toán không khớp.",
                "01" => "Không tìm thấy đơn hàng.",
                "02" => "Giao dịch đã được hệ thống ghi nhận trước đó.",
                "05" => "VNPay đã ghi nhận thanh toán nhưng đơn hàng không còn đủ điều kiện phát hành vé. Giao dịch đã được lưu để đối soát. Vui lòng không thanh toán lại.",
                "99" => "VNPay đã phản hồi nhưng đơn hàng chưa thể xác nhận. Vui lòng liên hệ bộ phận hỗ trợ.",
                _ => thanhToanThanhCong
                    ? "Thanh toán đã được tiếp nhận."
                    : "Giao dịch chưa thành công: " + response.Message
            };

            return View("VnPayResult");
        }

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
                    RspCode = ketQua.Code == "05"
                        ? "00"
                        : ketQua.Code,

                    Message = ketQua.Code == "05"
                        ? "Payment received and recorded for reconciliation"
                        : ketQua.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Lỗi xử lý VNPay IPN");

                return Json(new
                {
                    RspCode = "99",
                    Message = "System error"
                });
            }
        }
    }
}
