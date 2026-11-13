using System;
using System.Linq;
using QRCoder;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VeSuKienWeb.Data;
using VeSuKienWeb.Models;
using VeSuKienWeb.Models.KhachHang;
using System.Text.Json;
using System.Data;
using System.Globalization;
using System.Text;
using VeSuKienWeb.Services;



namespace VeSuKienWeb.Controllers
{
    public class KhachHangController : Controller
    {

private readonly NguCanhSuKien _db;
private readonly ILogger<KhachHangController> _logger;
private readonly GoiYSuKienService _goiYService;

public KhachHangController(
    NguCanhSuKien db,
    ILogger<KhachHangController> logger,
    GoiYSuKienService goiYService)
{
    _db = db;
    _logger = logger;
    _goiYService = goiYService;
}


        private int GetCurrentUserId()
        {
            var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(idStr))
            {
                throw new InvalidOperationException("Chưa đăng nhập.");
            }
            return int.Parse(idStr);
        }

        // ============================================================
        // ========== DANH SÁCH SỰ KIỆN (KHÁCH) =======================
        // ============================================================

 

[AllowAnonymous]
public async Task<IActionResult> Index(string? search)
{
    var now = DateTime.Now;

    var query = _db.SuKien
        .Include(s => s.LoaiSuKien)
        .Where(s => s.TrangThaiDuyet == "DaDuyet");

    if (!string.IsNullOrWhiteSpace(search))
    {
        search = search.Trim();

        query = query.Where(s =>
            s.TenSuKien.Contains(search) ||
            (s.MoTaChiTiet ?? "").Contains(search));
    }

    var list = await query
        .OrderBy(s => s.ThoiGianBatDau)
        .Select(s => new SuKienListItemKhVM
        {
            SuKienId = s.SuKienId,
            TenSuKien = s.TenSuKien,
            TenLoai = s.LoaiSuKien != null
                ? s.LoaiSuKien.TenLoai : null,
            ThoiGianBatDau = s.ThoiGianBatDau,
            ThoiGianKetThuc = s.ThoiGianKetThuc,

            DiaDiem = _db.DiaDiem
                .Where(d => d.SuKienId == s.SuKienId)
                .Select(d => d.TenDiaDiem)
                .FirstOrDefault(),

            GiaTu = _db.LoaiVe
                .Where(l => l.SuKienId == s.SuKienId)
                .Select(l => (decimal?)l.GiaVe)
                .Min() ?? s.GiaVeMacDinh,

            DaKetThuc = s.ThoiGianKetThuc < now,
            AnhSuKien = s.AnhSuKien
        })
        .ToListAsync();


var goiYSuKien = new List<SuKienListItemKhVM>();

if (User.Identity?.IsAuthenticated == true &&
    User.IsInRole("KhachHang") &&
    string.IsNullOrWhiteSpace(search))
{
    var idStr = User.FindFirstValue(
        ClaimTypes.NameIdentifier);

    if (int.TryParse(idStr, out int userId))
    {
        var idsGoiY = await _goiYService.LayGoiYAsync(
            userId,
            topN: 6
        );

        var thuTu = idsGoiY
            .Select((id, index) => new
            {
                Id = id,
                ThuTu = index
            })
            .ToDictionary(x => x.Id, x => x.ThuTu);

        goiYSuKien = list
            .Where(s => thuTu.ContainsKey(s.SuKienId))
            .OrderBy(s => thuTu[s.SuKienId])
            .ToList();
    }
}


    ViewBag.GoiYSuKien = goiYSuKien;
    ViewBag.Search = search;

    return View(list);
}



        // ============================================================
        // ========== CHI TIẾT SỰ KIỆN (KHÁCH) ========================
        // ============================================================

[AllowAnonymous]
public async Task<IActionResult> ChiTiet(int id)
{
    var sk = await _db.SuKien
        .Include(s => s.LoaiSuKien)
        .Include(s => s.ToChucSuKien)
        .FirstOrDefaultAsync(s =>
            s.SuKienId == id &&
            s.TrangThaiDuyet == "DaDuyet");

    if (sk == null)
        return NotFound();

Console.WriteLine("===== KIEM TRA LICH SU XEM =====");
Console.WriteLine($"Da dang nhap: {User.Identity?.IsAuthenticated}");
Console.WriteLine($"User ID: {User.FindFirstValue(ClaimTypes.NameIdentifier)}");
Console.WriteLine($"Vai tro: {User.FindFirstValue(ClaimTypes.Role)}");
Console.WriteLine($"Su kien ID: {id}");
Console.WriteLine("================================");

if (User.Identity?.IsAuthenticated == true &&
    User.IsInRole("KhachHang") &&
    int.TryParse(
        User.FindFirstValue(ClaimTypes.NameIdentifier),
        out int nguoiDungId))
{
    var thoiGianHienTai = DateTime.Now;

    var lichSu = await _db.LichSuXemSuKien
        .Where(x =>
            x.NguoiDungId == nguoiDungId &&
            x.SuKienId == id &&
            x.ThoiGianXem >= thoiGianHienTai.AddMinutes(-30))
        .OrderByDescending(x => x.ThoiGianXem)
        .FirstOrDefaultAsync();

    if (lichSu != null)
    {
        lichSu.ThoiGianXem = thoiGianHienTai;
    }
    else
    {
        _db.LichSuXemSuKien.Add(new LichSuXemSuKien
        {
            NguoiDungId = nguoiDungId,
            SuKienId = id,
            ThoiGianXem = thoiGianHienTai
        });
    }

    await _db.SaveChangesAsync();
}

    var now = DateTime.Now;
    bool coTheDatVe = sk.ThoiGianKetThuc > now;

    var diaDiem = await _db.DiaDiem
        .FirstOrDefaultAsync(d => d.SuKienId == sk.SuKienId);

    var online = await _db.SuKienTrucTuyen
        .FirstOrDefaultAsync(o => o.SuKienId == sk.SuKienId);

    var loaiVes = await _db.LoaiVe
        .Where(l => l.SuKienId == sk.SuKienId)
        .ToListAsync();

    var gheList = new List<DatVeGheItemVM>();

    if (diaDiem != null)
    {
        gheList = await _db.SoDoChoNgoi
            .Include(g => g.LoaiVe)
            .Where(g => g.DiaDiemId == diaDiem.DiaDiemId)
            .OrderBy(g => g.Hang)
            .ThenBy(g => g.Cot)
            .Select(g => new DatVeGheItemVM
            {
                ChoNgoiId = g.ChoNgoiId,
                Hang = g.Hang ?? string.Empty,
                Cot = g.Cot,
                TrangThai = g.TrangThai ?? "Trong",
                TenLoaiVe = g.LoaiVe.TenLoai ?? string.Empty,
                GiaVe = g.LoaiVe.GiaVe
            })
            .ToListAsync();
    }

    ViewBag.SoDoJson = "null";

    var soDo = await _db.SoDoSuKien
        .AsNoTracking()
        .FirstOrDefaultAsync(x => x.SuKienId == sk.SuKienId);

    if (soDo != null && diaDiem != null)
    {
        var gheSoDo = await _db.SoDoChoNgoi
            .AsNoTracking()
            .Where(x => x.DiaDiemId == diaDiem.DiaDiemId)
            .OrderBy(x => x.Hang)
            .ThenBy(x => x.Cot)
.Select(x => new
{
    id = x.ChoNgoiId,
    hang = x.Hang,
    cot = x.Cot,
    x = x.ViTriX,
    y = x.ViTriY,
    trangThai = x.TrangThai,
    khu = x.KhuVucChoNgoi != null
        ? x.KhuVucChoNgoi.TenKhu
        : x.LoaiVe.TenLoai,
    tenLoai = x.LoaiVe.TenLoai,
    giaVe = x.LoaiVe.GiaVe
})
            .ToListAsync();

        var thanhPhan = await _db.ThanhPhanSoDo
            .AsNoTracking()
            .Where(x => x.SoDoId == soDo.SoDoId)
            .Select(x => new
            {
                loai = x.LoaiThanhPhan,
                ten = x.TenHienThi,
                x = x.ViTriX,
                y = x.ViTriY,
                rong = x.ChieuRong,
                cao = x.ChieuCao
            })
            .ToListAsync();

        ViewBag.SoDoJson = JsonSerializer.Serialize(new
        {
            chieuRong = soDo.ChieuRong,
            chieuCao = soDo.ChieuCao,
            ghe = gheSoDo,
            thanhPhan
        });
    }

    var vm = new SuKienChiTietKhVM
    {
        SuKien = sk,
        LoaiSuKien = sk.LoaiSuKien,
        ToChuc = sk.ToChucSuKien,
        DiaDiem = diaDiem,
        SuKienTrucTuyen = online,
        LoaiVes = loaiVes,
        GheList = gheList,
        CoTheDatVe = coTheDatVe,

        DiemTrungBinh = null,
        TongSoDanhGia = 0
    };

    return View(vm);
}



        // ============================================================
        // ========== ĐẶT VÉ (OFFLINE + ONLINE) =======================
        // ============================================================


[Authorize(Roles = "KhachHang")]
[HttpGet]
public async Task<IActionResult> DatVe(int id)
{
    var now = DateTime.Now;

    var sk = await _db.SuKien
        .Include(s => s.LoaiSuKien)
        .FirstOrDefaultAsync(s =>
            s.SuKienId == id &&
            s.TrangThaiDuyet == "DaDuyet");

    if (sk == null)
        return NotFound();

    if (sk.ThoiGianKetThuc <= now)
    {
        TempData["ThongBao"] = "Sự kiện đã kết thúc, không thể đặt vé.";
        return RedirectToAction("ChiTiet", new { id });
    }

    var loaiTenLower = (sk.LoaiSuKien?.TenLoai ?? "").ToLower();

    bool laTrucTiep =
        loaiTenLower.Contains("trực tiếp") ||
        loaiTenLower.Contains("truc tiep");

    bool laWorkshop = loaiTenLower.Contains("workshop");
    bool laSuKienCoSoDoGhe = laTrucTiep || laWorkshop;

    var vm = new DatVeKhVM
    {
        SuKienId = sk.SuKienId,
        TenSuKien = sk.TenSuKien,
        LaSuKienTrucTiep = laSuKienCoSoDoGhe,
        LaWorkshop = laWorkshop
    };

    DiaDiem? diaDiem = null;

    if (laSuKienCoSoDoGhe)
    {
        diaDiem = await _db.DiaDiem
            .FirstOrDefaultAsync(d => d.SuKienId == sk.SuKienId);

        if (diaDiem == null)
        {
            TempData["ThongBao"] = "Sự kiện chưa cấu hình địa điểm.";
            return RedirectToAction("ChiTiet", new { id });
        }

        var gheList = await _db.SoDoChoNgoi
            .AsNoTracking()
            .Include(g => g.LoaiVe)
            .Where(g => g.DiaDiemId == diaDiem.DiaDiemId)
            .OrderBy(g => g.Hang)
            .ThenBy(g => g.Cot)
            .Select(g => new
            {
                g.ChoNgoiId,
                g.Hang,
                g.Cot,
                g.TrangThai,
                g.DonHangGiuId,
                g.ThoiGianHetHanGiu,
                TenLoaiVe = g.LoaiVe.TenLoai,
                GiaVe = g.LoaiVe.GiaVe
            })
            .ToListAsync();

        vm.GheList = gheList.Select(g =>
        {
            string trangThai = g.TrangThai ?? "Trong";

            bool dangGiu =
                g.DonHangGiuId.HasValue &&
                g.ThoiGianHetHanGiu.HasValue &&
                g.ThoiGianHetHanGiu.Value > now;

            if (dangGiu)
                trangThai = "DangGiu";

            return new DatVeGheItemVM
            {
                ChoNgoiId = g.ChoNgoiId,
                Hang = g.Hang ?? string.Empty,
                Cot = g.Cot,
                TrangThai = trangThai,
                TenLoaiVe = g.TenLoaiVe ?? string.Empty,
                GiaVe = g.GiaVe
            };
        }).ToList();
    }
    else
    {
        vm.LaSuKienTrucTiep = false;

        var loaiVeMacDinh = await _db.LoaiVe
            .Where(l => l.SuKienId == sk.SuKienId)
            .OrderBy(l => l.GiaVe)
            .FirstOrDefaultAsync();

        if (loaiVeMacDinh == null)
        {
            TempData["ThongBao"] =
                "Sự kiện trực tuyến chưa cấu hình loại vé.";

            return RedirectToAction("ChiTiet", new { id });
        }

        vm.LoaiVeId = loaiVeMacDinh.LoaiVeId;
    }

    ViewBag.SoDoJson = "null";

    var soDo = await _db.SoDoSuKien
        .AsNoTracking()
        .FirstOrDefaultAsync(x => x.SuKienId == sk.SuKienId);

    if (soDo != null && diaDiem != null)
    {
        var gheSoDo = await _db.SoDoChoNgoi
            .AsNoTracking()
            .Where(x => x.DiaDiemId == diaDiem.DiaDiemId)
            .OrderBy(x => x.Hang)
            .ThenBy(x => x.Cot)
            .Select(x => new
            {
                id = x.ChoNgoiId,
                hang = x.Hang,
                cot = x.Cot,
                x = x.ViTriX,
                y = x.ViTriY,

                trangThai =
                    x.DonHangGiuId != null &&
                    x.ThoiGianHetHanGiu > now
                        ? "DangGiu"
                        : x.TrangThai,

                khu = x.KhuVucChoNgoi != null
                    ? x.KhuVucChoNgoi.TenKhu
                    : x.LoaiVe.TenLoai,

                tenLoai = x.LoaiVe.TenLoai,
                giaVe = x.LoaiVe.GiaVe
            })
            .ToListAsync();

        var thanhPhan = await _db.ThanhPhanSoDo
            .AsNoTracking()
            .Where(x => x.SoDoId == soDo.SoDoId)
            .Select(x => new
            {
                loai = x.LoaiThanhPhan,
                ten = x.TenHienThi,
                x = x.ViTriX,
                y = x.ViTriY,
                rong = x.ChieuRong,
                cao = x.ChieuCao
            })
            .ToListAsync();

        ViewBag.SoDoJson = JsonSerializer.Serialize(new
        {
            chieuRong = soDo.ChieuRong,
            chieuCao = soDo.ChieuCao,
            ghe = gheSoDo,
            thanhPhan
        });
    }

    return View(vm);
}

[Authorize(Roles = "KhachHang")]
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> DatVe(DatVeKhVM model)
{
    var sk = await _db.SuKien
        .Include(s => s.LoaiSuKien)
        .FirstOrDefaultAsync(s =>
            s.SuKienId == model.SuKienId &&
            s.TrangThaiDuyet == "DaDuyet");

    if (sk == null)
        return NotFound();

    if (sk.ThoiGianKetThuc <= DateTime.Now)
    {
        TempData["ThongBao"] = "Sự kiện đã kết thúc, không thể đặt vé.";
        return RedirectToAction("ChiTiet", new { id = model.SuKienId });
    }

    var loaiTenLower = (sk.LoaiSuKien?.TenLoai ?? "").ToLower();

    bool laTrucTiep =
        loaiTenLower.Contains("trực tiếp") ||
        loaiTenLower.Contains("truc tiep");

    bool laWorkshop = loaiTenLower.Contains("workshop");
    bool laSuKienCoSoDoGhe = laTrucTiep || laWorkshop;

    int userId = GetCurrentUserId();

    List<int> gheIds = new();

    if (laSuKienCoSoDoGhe)
    {
        gheIds = (model.GheChonIds ?? new List<int>())
            .Distinct()
            .ToList();

        if (gheIds.Count == 0)
        {
            TempData["ThongBao"] = "Vui lòng chọn ít nhất 1 ghế.";
            return RedirectToAction("DatVe", new { id = model.SuKienId });
        }

        if (gheIds.Count > 10)
        {
            TempData["ThongBao"] = "Bạn chỉ được đặt tối đa 10 vé trong một lần.";
            return RedirectToAction("DatVe", new { id = model.SuKienId });
        }
    }
    else
    {
        if (model.SoLuong < 1 || model.SoLuong > 10)
        {
            TempData["ThongBao"] = "Số lượng vé phải từ 1 đến 10.";
            return RedirectToAction("DatVe", new { id = model.SuKienId });
        }

        if (!model.LoaiVeId.HasValue)
        {
            TempData["ThongBao"] = "Sự kiện chưa cấu hình loại vé.";
            return RedirectToAction("ChiTiet", new { id = model.SuKienId });
        }
    }

    // Kiểm tra lịch trùng với sự kiện đã thanh toán
    var suKienTrung = await _db.Ve
        .Include(v => v.SuKien)
        .Where(v =>
            v.NguoiDungId == userId &&
            v.TrangThai == "DaThanhToan" &&
            v.SuKienId != sk.SuKienId &&
            v.SuKien.ThoiGianBatDau < sk.ThoiGianKetThuc &&
            v.SuKien.ThoiGianKetThuc > sk.ThoiGianBatDau)
        .Select(v => v.SuKien)
        .Distinct()
        .ToListAsync();

    if (suKienTrung.Any())
    {
        TempData["CanhBaoTrungLich"] =
            "Lưu ý: Bạn đã có sự kiện khác trong khoảng thời gian này. Nếu tiếp tục, lịch sẽ bị trùng.";
    }

    await using var transaction =
        await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

    try
    {
        var now = DateTime.Now;
        var hetHan = now.AddMinutes(5);

        var donHang = new DonHang
        {
            NguoiDungId = userId,
            NgayDat = now,
            TrangThai = "DangXuLy",
            TongTien = 0
        };

        _db.DonHang.Add(donHang);
        await _db.SaveChangesAsync();

        decimal tongTien = 0;

        if (laSuKienCoSoDoGhe)
        {
            var diaDiem = await _db.DiaDiem
                .FirstOrDefaultAsync(d => d.SuKienId == sk.SuKienId);

            if (diaDiem == null)
            {
                await transaction.RollbackAsync();
                TempData["ThongBao"] = "Sự kiện chưa cấu hình địa điểm.";
                return RedirectToAction("ChiTiet", new { id = model.SuKienId });
            }

            // Xử lý ghế theo thứ tự ID để giảm nguy cơ deadlock
            var gheChon = await _db.SoDoChoNgoi
                .AsNoTracking()
                .Include(g => g.LoaiVe)
                .Where(g =>
                    gheIds.Contains(g.ChoNgoiId) &&
                    g.DiaDiemId == diaDiem.DiaDiemId)
                .OrderBy(g => g.ChoNgoiId)
                .ToListAsync();

            if (gheChon.Count != gheIds.Count)
            {
                await transaction.RollbackAsync();
                TempData["ThongBao"] = "Một số ghế không hợp lệ. Vui lòng chọn lại.";
                return RedirectToAction("DatVe", new { id = model.SuKienId });
            }

            foreach (var ghe in gheChon)
            {
                if (!string.Equals(
                    ghe.TrangThai,
                    "Trong",
                    StringComparison.OrdinalIgnoreCase))
                {
                    await transaction.RollbackAsync();

                    TempData["ThongBao"] =
                        $"Ghế {ghe.Hang}{ghe.Cot} không còn khả dụng.";

                    return RedirectToAction("DatVe", new { id = model.SuKienId });
                }

                // =========================================
                // GIẢI PHÓNG TOÀN BỘ ĐƠN GIỮ GHẾ ĐÃ HẾT HẠN
                // =========================================

                if (ghe.DonHangGiuId.HasValue &&
                    (!ghe.ThoiGianHetHanGiu.HasValue ||
                     ghe.ThoiGianHetHanGiu.Value <= now))
                {
                    int donHangCuId = ghe.DonHangGiuId.Value;

                    // Hủy các vé chưa thanh toán của đơn cũ
                    await _db.Database.ExecuteSqlInterpolatedAsync($@"
                        UPDATE dbo.Ve
                        SET TrangThai = N'Huy'
                        WHERE DonHangId = {donHangCuId}
                          AND TrangThai = N'ChuaThanhToan';
                    ");

                    // Hủy đơn hàng cũ
                    await _db.Database.ExecuteSqlInterpolatedAsync($@"
                        UPDATE dbo.DonHang
                        SET TrangThai = N'Huy'
                        WHERE DonHangId = {donHangCuId}
                          AND TrangThai = N'DangXuLy';
                    ");

                    // Giải phóng tất cả ghế thuộc đơn cũ
                    await _db.Database.ExecuteSqlInterpolatedAsync($@"
                        UPDATE dbo.SoDoChoNgoi
                        SET DonHangGiuId = NULL,
                            ThoiGianHetHanGiu = NULL
                        WHERE DonHangGiuId = {donHangCuId}
                          AND (
                              ThoiGianHetHanGiu IS NULL
                              OR ThoiGianHetHanGiu <= {now}
                          );
                    ");
                }

                // =========================================
                // GIỮ GHẾ 5 PHÚT BẰNG CẬP NHẬT CÓ ĐIỀU KIỆN
                // =========================================

                int soDongCapNhat =
                    await _db.Database.ExecuteSqlInterpolatedAsync($@"
                        UPDATE dbo.SoDoChoNgoi
                        SET DonHangGiuId = {donHang.DonHangId},
                            ThoiGianHetHanGiu = {hetHan}
                        WHERE ChoNgoiId = {ghe.ChoNgoiId}
                          AND TrangThai = N'Trong'
                          AND (
                              DonHangGiuId IS NULL
                              OR ThoiGianHetHanGiu IS NULL
                              OR ThoiGianHetHanGiu <= {now}
                          );
                    ");

                if (soDongCapNhat != 1)
                {
                    await transaction.RollbackAsync();

                    TempData["ThongBao"] =
                        $"Ghế {ghe.Hang}{ghe.Cot} đang được khách khác giữ hoặc đã được đặt. Vui lòng chọn ghế khác.";

                    return RedirectToAction("DatVe", new { id = model.SuKienId });
                }

                // Tạo vé thuộc đơn mới
                var ve = new Ve
                {
                    NguoiDungId = userId,
                    LoaiVeId = ghe.LoaiVeId,
                    SuKienId = sk.SuKienId,
                    ChoNgoiId = ghe.ChoNgoiId,
                    MaVe = $"V{DateTime.Now:yyyyMMddHHmmss}{Guid.NewGuid().ToString("N")[..4]}",
                    NgayDat = now,
                    TrangThai = "ChuaThanhToan",
                    DonHangId = donHang.DonHangId
                };

                _db.Ve.Add(ve);
                tongTien += ghe.LoaiVe.GiaVe;
            }
        }
        else
        {
            // =========================================
            // ĐẶT VÉ TRỰC TUYẾN
            // =========================================

            var loaiVe = await _db.LoaiVe
                .FirstOrDefaultAsync(l =>
                    l.LoaiVeId == model.LoaiVeId!.Value &&
                    l.SuKienId == sk.SuKienId);

            if (loaiVe == null)
            {
                await transaction.RollbackAsync();
                TempData["ThongBao"] = "Loại vé không hợp lệ.";
                return RedirectToAction("DatVe", new { id = model.SuKienId });
            }

            for (int i = 0; i < model.SoLuong; i++)
            {
                var ve = new Ve
                {
                    NguoiDungId = userId,
                    LoaiVeId = loaiVe.LoaiVeId,
                    SuKienId = sk.SuKienId,
                    ChoNgoiId = null,
                    MaVe = $"V{DateTime.Now:yyyyMMddHHmmss}{Guid.NewGuid().ToString("N")[..4]}",
                    NgayDat = now,
                    TrangThai = "ChuaThanhToan",
                    DonHangId = donHang.DonHangId
                };

                _db.Ve.Add(ve);
                tongTien += loaiVe.GiaVe;
            }
        }

        if (tongTien <= 0)
        {
            await transaction.RollbackAsync();
            TempData["ThongBao"] = "Không tạo được vé hợp lệ.";
            return RedirectToAction("ChiTiet", new { id = model.SuKienId });
        }

        donHang.TongTien = tongTien;

        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        return RedirectToAction("ThanhToan", new { id = donHang.DonHangId });
    }
    catch (DbUpdateException ex)
    {
        await transaction.RollbackAsync();

        _logger.LogError(ex, "Lỗi lưu đơn đặt vé.");

        TempData["ThongBao"] =
            "Ghế vừa được khách khác đặt hoặc có lỗi khi lưu dữ liệu. Vui lòng thử lại.";

        return RedirectToAction("DatVe", new { id = model.SuKienId });
    }
    catch (Exception ex)
    {
        await transaction.RollbackAsync();

        _logger.LogError(ex, "Lỗi xử lý đặt vé.");

        TempData["ThongBao"] =
            "Có lỗi xảy ra khi đặt vé. Vui lòng thử lại.";

        return RedirectToAction("DatVe", new { id = model.SuKienId });
    }
}

        // ============================================================
        // ========== THANH TOÁN (GIẢ LẬP QR) =========================
        // ============================================================


[Authorize(Roles = "KhachHang")]
[HttpGet]
public async Task<IActionResult> ThanhToan(int id)
{
    int userId = GetCurrentUserId();

    await using var transaction =
        await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

    try
    {
        var donHang = await _db.DonHang
            .Include(d => d.Ves)
                .ThenInclude(v => v.SuKien)
            .Include(d => d.Ves)
                .ThenInclude(v => v.LoaiVe)
            .Include(d => d.Ves)
                .ThenInclude(v => v.ChoNgoi)
            .FirstOrDefaultAsync(d =>
                d.DonHangId == id &&
                d.NguoiDungId == userId);

        if (donHang == null)
            return NotFound();

        if (donHang.TrangThai == "ThanhCong")
        {
            await transaction.RollbackAsync();
            return RedirectToAction("ThanhToanThanhCong", new { id });
        }

        if (donHang.TrangThai != "DangXuLy")
        {
            await transaction.RollbackAsync();
            TempData["ThongBao"] = "Đơn hàng đã bị hủy hoặc không còn hiệu lực.";
            return RedirectToAction("VeCuaToi");
        }

        if (!donHang.Ves.Any())
        {
            await transaction.RollbackAsync();
            TempData["ThongBao"] = "Đơn hàng không có vé hợp lệ.";
            return RedirectToAction("VeCuaToi");
        }

        var veCoGhe = donHang.Ves
            .Where(v => v.ChoNgoiId.HasValue)
            .ToList();

        if (veCoGhe.Any())
        {
            var gheIds = veCoGhe
                .Select(v => v.ChoNgoiId!.Value)
                .ToList();

            var gheList = await _db.SoDoChoNgoi
                .Where(g => gheIds.Contains(g.ChoNgoiId))
                .ToListAsync();

            bool hetHan = gheList.Count != gheIds.Count ||
                veCoGhe.Any(v =>
                {
                    var ghe = gheList.FirstOrDefault(
                        g => g.ChoNgoiId == v.ChoNgoiId);

                    return ghe == null ||
                           ghe.DonHangGiuId != donHang.DonHangId ||
                           !ghe.ThoiGianHetHanGiu.HasValue ||
                           ghe.ThoiGianHetHanGiu.Value <= DateTime.Now ||
                           ghe.TrangThai != "Trong";
                });

            if (hetHan)
            {
                foreach (var ve in donHang.Ves)
                {
                    if (ve.TrangThai == "ChuaThanhToan")
                        ve.TrangThai = "Huy";
                }

                donHang.TrangThai = "Huy";

                foreach (var ghe in gheList.Where(
                    g => g.DonHangGiuId == donHang.DonHangId))
                {
                    ghe.DonHangGiuId = null;
                    ghe.ThoiGianHetHanGiu = null;
                }

                await _db.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["ThongBao"] =
                    "Thời gian giữ ghế đã hết hạn. Vui lòng đặt vé lại.";

                return RedirectToAction("VeCuaToi");
            }
        }

        await transaction.CommitAsync();

        ViewBag.CanhBaoTrungLich = TempData["CanhBaoTrungLich"];

        var vm = new ThanhToanKhVM
        {
            DonHangId = donHang.DonHangId,
            TongTien = donHang.TongTien,
            Ves = donHang.Ves.Select(v => new VeThanhToanItemVM
            {
                VeId = v.VeId,
                MaVe = v.MaVe,
                TenSuKien = v.SuKien.TenSuKien,
                TenLoaiVe = v.LoaiVe.TenLoai ?? string.Empty,
                Hang = v.ChoNgoi?.Hang,
                Cot = v.ChoNgoi?.Cot,
                GiaVe = v.LoaiVe.GiaVe
            }).ToList()
        };

        return View(vm);
    }
    catch (Exception ex)
    {
        await transaction.RollbackAsync();
        _logger.LogError(ex, "Lỗi kiểm tra đơn hàng trước thanh toán.");

        TempData["ThongBao"] =
            "Không thể kiểm tra đơn hàng. Vui lòng thử lại.";

        return RedirectToAction("VeCuaToi");
    }
}


// ============================================================
// ========== VÉ CỦA TÔI ======================================
// ============================================================

[Authorize(Roles = "KhachHang")]
public async Task<IActionResult> VeCuaToi()
{
    int userId = GetCurrentUserId();
    var now = DateTime.Now;

    var veRaw = await _db.Ve
        .Include(v => v.SuKien)
            .ThenInclude(s => s.LoaiSuKien)
        .Include(v => v.LoaiVe)
        .Include(v => v.ChoNgoi)
        .Where(v => v.NguoiDungId == userId)
        .OrderByDescending(v => v.NgayDat)
        .ToListAsync();

    var list = veRaw
        .GroupBy(v => v.DonHangId.HasValue ? $"DH_{v.DonHangId.Value}" : $"VE_{v.VeId}")
        .Select(g =>
        {
            var first = g.First();
            var tenLoai = first.SuKien.LoaiSuKien != null
                ? first.SuKien.LoaiSuKien.TenLoai
                : string.Empty;

            return new VeCuaToiItemVM
            {
               DangChoHoanVe = g.Any(v =>
    _db.YeuCauHoanHuyVe.Any(y =>
        y.VeId == v.VeId &&
        y.TrangThai == "ChoDuyet")),

DuocXemQr =
    g.All(v => v.TrangThai == "DaThanhToan") &&
    first.SuKien.ThoiGianKetThuc > now &&
    !g.Any(v =>
        _db.YeuCauHoanHuyVe.Any(y =>
            y.VeId == v.VeId &&
            y.TrangThai == "ChoDuyet")),

                VeId = first.VeId,
                SuKienId = first.SuKienId,
                MaVe = first.MaVe,

                TenSuKien = first.SuKien.TenSuKien,
                TenLoaiSuKien = tenLoai,
                AnhSuKien = first.SuKien.AnhSuKien,
                ThoiGianBatDau = first.SuKien.ThoiGianBatDau,
                ThoiGianKetThuc = first.SuKien.ThoiGianKetThuc,

                TenLoaiVe = first.LoaiVe.TenLoai ?? string.Empty,
                GiaVe = g.Sum(x => x.LoaiVe.GiaVe),

                Hang = first.ChoNgoi != null ? first.ChoNgoi.Hang : null,
                Cot = first.ChoNgoi != null ? first.ChoNgoi.Cot : null,

                DanhSachGhe = string.Join(", ",
                    g.Where(x => x.ChoNgoi != null)
                     .Select(x => $"{x.ChoNgoi!.Hang}{x.ChoNgoi.Cot}")
                ),

                TrangThaiVe = first.TrangThai,
                NgayDat = first.NgayDat,

                LaTrucTiep = tenLoai.ToLower().Contains("trực tiếp")
                             || tenLoai.ToLower().Contains("truc tiep"),

                LaTrucTuyen = tenLoai.ToLower().Contains("trực tuyến")
                              || tenLoai.ToLower().Contains("truc tuyen")
                              || tenLoai.ToLower().Contains("online")
                              || tenLoai.ToLower().Contains("hybrid"),

                DiaDiem = _db.DiaDiem
                    .Where(d => d.SuKienId == first.SuKienId)
                    .Select(d => d.TenDiaDiem)
                    .FirstOrDefault(),

                DiaChi = _db.DiaDiem
                    .Where(d => d.SuKienId == first.SuKienId)
                    .Select(d => d.DiaChi)
                    .FirstOrDefault(),

                OnlineNenTang = _db.SuKienTrucTuyen
                    .Where(o => o.SuKienId == first.SuKienId)
                    .Select(o => o.NenTang)
                    .FirstOrDefault(),

                OnlineLinkXem = _db.SuKienTrucTuyen
                    .Where(o => o.SuKienId == first.SuKienId)
                    .Select(o => o.LinkXem)
                    .FirstOrDefault(),

                OnlineMatKhau = _db.SuKienTrucTuyen
                    .Where(o => o.SuKienId == first.SuKienId)
                    .Select(o => o.MatKhauPhong)
                    .FirstOrDefault(),

                SuKienDaKetThuc = first.SuKien.ThoiGianKetThuc < now,


CoTheHuy =
    g.All(v => v.TrangThai == "DaThanhToan") &&
    first.SuKien.ThoiGianBatDau >= now.AddHours(24) &&
    !g.Any(v =>
        _db.YeuCauHoanHuyVe.Any(y =>
            y.VeId == v.VeId &&
            y.TrangThai == "ChoDuyet")),

            };
        })
        .OrderByDescending(x => x.NgayDat)
        .ToList();

    return View(list);
}

[Authorize(Roles = "KhachHang")]
[HttpGet]
public async Task<IActionResult> ChiTietVe(int veId)
{
    int userId = GetCurrentUserId();
    var now = DateTime.Now;

    var veGoc = await _db.Ve
        .Where(v => v.VeId == veId && v.NguoiDungId == userId)
        .FirstOrDefaultAsync();

    if (veGoc == null)
    {
        return NotFound();
    }

    var veRaw = await _db.Ve
        .Include(v => v.SuKien)
            .ThenInclude(s => s.LoaiSuKien)
        .Include(v => v.LoaiVe)
        .Include(v => v.ChoNgoi)
        .Where(v => v.NguoiDungId == userId
            && (
                veGoc.DonHangId.HasValue
                    ? v.DonHangId == veGoc.DonHangId
                    : v.VeId == veGoc.VeId
            ))
        .ToListAsync();

    if (!veRaw.Any())
    {
        return NotFound();
    }

    var first = veRaw.First();

    var tenLoai = first.SuKien.LoaiSuKien != null
        ? first.SuKien.LoaiSuKien.TenLoai
        : string.Empty;

    var ve = new VeCuaToiItemVM
    {
DangChoHoanVe = veRaw.Any(v =>
    _db.YeuCauHoanHuyVe.Any(y =>
        y.VeId == v.VeId &&
        y.TrangThai == "ChoDuyet")),

DuocXemQr =
    veRaw.All(v => v.TrangThai == "DaThanhToan") &&
    first.SuKien.ThoiGianKetThuc > now &&
    !veRaw.Any(v =>
        _db.YeuCauHoanHuyVe.Any(y =>
            y.VeId == v.VeId &&
            y.TrangThai == "ChoDuyet")),

        VeId = first.VeId,
        SuKienId = first.SuKienId,
        MaVe = first.MaVe,

        TenSuKien = first.SuKien.TenSuKien,
        TenLoaiSuKien = tenLoai,
        AnhSuKien = first.SuKien.AnhSuKien,
        ThoiGianBatDau = first.SuKien.ThoiGianBatDau,
        ThoiGianKetThuc = first.SuKien.ThoiGianKetThuc,

        TenLoaiVe = first.LoaiVe.TenLoai ?? string.Empty,
        GiaVe = veRaw.Sum(x => x.LoaiVe.GiaVe),

        Hang = first.ChoNgoi != null ? first.ChoNgoi.Hang : null,
        Cot = first.ChoNgoi != null ? first.ChoNgoi.Cot : null,

        DanhSachGhe = string.Join(", ",
            veRaw.Where(x => x.ChoNgoi != null)
                 .Select(x => $"{x.ChoNgoi!.Hang}{x.ChoNgoi.Cot}")
        ),

        TrangThaiVe = first.TrangThai,
        NgayDat = first.NgayDat,

        LaTrucTiep = tenLoai.ToLower().Contains("trực tiếp")
                     || tenLoai.ToLower().Contains("truc tiep"),

        LaTrucTuyen = tenLoai.ToLower().Contains("trực tuyến")
                      || tenLoai.ToLower().Contains("truc tuyen")
                      || tenLoai.ToLower().Contains("online")
                      || tenLoai.ToLower().Contains("hybrid"),

        DiaDiem = _db.DiaDiem
            .Where(d => d.SuKienId == first.SuKienId)
            .Select(d => d.TenDiaDiem)
            .FirstOrDefault(),

        DiaChi = _db.DiaDiem
            .Where(d => d.SuKienId == first.SuKienId)
            .Select(d => d.DiaChi)
            .FirstOrDefault(),

        OnlineNenTang = _db.SuKienTrucTuyen
            .Where(o => o.SuKienId == first.SuKienId)
            .Select(o => o.NenTang)
            .FirstOrDefault(),

        OnlineLinkXem = _db.SuKienTrucTuyen
            .Where(o => o.SuKienId == first.SuKienId)
            .Select(o => o.LinkXem)
            .FirstOrDefault(),

        OnlineMatKhau = _db.SuKienTrucTuyen
            .Where(o => o.SuKienId == first.SuKienId)
            .Select(o => o.MatKhauPhong)
            .FirstOrDefault(),

        SuKienDaKetThuc = first.SuKien.ThoiGianKetThuc < now,

     
CoTheHuy =
    veRaw.All(v => v.TrangThai == "DaThanhToan") &&
    first.SuKien.ThoiGianBatDau >= now.AddHours(24) &&
    !veRaw.Any(v =>
        _db.YeuCauHoanHuyVe.Any(y =>
            y.VeId == v.VeId &&
            y.TrangThai == "ChoDuyet")),

    };

    return View(ve);
}

   // ✅ XEM MÃ QR

[Authorize(Roles = "KhachHang")]
[HttpGet]
public async Task<IActionResult> XemMaQr(int veId)
{
    int userId = GetCurrentUserId();
    var now = DateTime.Now;

    var ve = await _db.Ve
        .AsNoTracking()
        .Include(v => v.SuKien)
        .FirstOrDefaultAsync(v =>
            v.VeId == veId &&
            v.NguoiDungId == userId);

    if (ve == null)
        return NotFound();

    // Chỉ vé đã thanh toán mới được xem QR.
    if (ve.TrangThai != "DaThanhToan")
    {
        TempData["QrError"] =
            "Vé chưa thanh toán, đã sử dụng hoặc không còn hiệu lực.";

        return RedirectToAction("VeCuaToi");
    }

    // Sự kiện đã kết thúc: không hiển thị QR.
    if (ve.SuKien.ThoiGianKetThuc <= now)
    {
        TempData["QrError"] =
            "Sự kiện đã kết thúc. Mã QR không còn hiệu lực.";

        return RedirectToAction("VeCuaToi");
    }

    // Chờ duyệt hoàn vé: tạm khóa QR.

bool dangChoHoan = await (
    from yc in _db.YeuCauHoanHuyVe
    join veYeuCau in _db.Ve
        on yc.VeId equals veYeuCau.VeId
    where yc.TrangThai == "ChoDuyet"
        && yc.HinhThuc == "HuyVe"
        && (
            ve.DonHangId.HasValue
                ? veYeuCau.DonHangId == ve.DonHangId
                : veYeuCau.VeId == ve.VeId
        )
    select yc.YeuCauId
).AnyAsync();


    // Giữ nguyên định dạng QR hiện có.
    string qrText = $"CHECKIN|{ve.VeId}|{ve.MaVe}";

    using var qrGenerator = new QRCodeGenerator();

    QRCodeData qrCodeData =
        qrGenerator.CreateQrCode(
            qrText,
            QRCodeGenerator.ECCLevel.Q);

    var pngQr = new PngByteQRCode(qrCodeData);
    byte[] qrBytes = pngQr.GetGraphic(20);

    var vm = new MaQrVM
    {
        VeId = ve.VeId,
        MaVe = ve.MaVe,
        QrCodeBase64 = Convert.ToBase64String(qrBytes),
        HetHieuLuc = false
    };

    return View(vm);
}

  // ============================================================
// ========== YÊU CẦU HỦY / HOÀN VÉ (KHÁCH HÀNG) ==============
// ============================================================

[Authorize(Roles = "KhachHang")]
[HttpGet]
public async Task<IActionResult> YeuCauHoanHuy(int veId)
{
    int userId = GetCurrentUserId();

    var veGoc = await _db.Ve
        .Include(v => v.SuKien)
        .Include(v => v.LoaiVe)
        .Include(v => v.ChoNgoi)
        .FirstOrDefaultAsync(v => v.VeId == veId && v.NguoiDungId == userId);

    if (veGoc == null) return NotFound();

    var nhomVe = await _db.Ve
        .Include(v => v.SuKien)
        .Include(v => v.LoaiVe)
        .Include(v => v.ChoNgoi)
        .Where(v => v.NguoiDungId == userId
            && (
                veGoc.DonHangId.HasValue
                    ? v.DonHangId == veGoc.DonHangId
                    : v.VeId == veGoc.VeId
            ))
        .ToListAsync();

    if (!nhomVe.Any()) return NotFound();

    if (nhomVe.Any(v => v.TrangThai != "DaThanhToan") || veGoc.SuKien.ThoiGianBatDau < DateTime.Now.AddHours(24))
    {
        TempData["ThongBao"] = "Vé này không đủ điều kiện để yêu cầu hủy.";
        return RedirectToAction("VeCuaToi");
    }

    bool daCoYeuCauChoDuyet = await _db.YeuCauHoanHuyVe
        .AnyAsync(yc => nhomVe.Select(v => v.VeId).Contains(yc.VeId)
                        && yc.TrangThai == "ChoDuyet");

    if (daCoYeuCauChoDuyet)
    {
        TempData["ThongBao"] = "Bạn đã gửi yêu cầu hủy cho vé này và đang chờ xử lý.";
        return RedirectToAction("VeCuaToi");
    }

    string danhSachGhe = string.Join(", ",
        nhomVe.Where(v => v.ChoNgoi != null)
              .Select(v => $"{v.ChoNgoi!.Hang}{v.ChoNgoi.Cot}")
    );

    var vm = new YeuCauHoanHuyVM
    {
        VeId = veGoc.VeId,
        MaVe = veGoc.MaVe,
        TenSuKien = veGoc.SuKien.TenSuKien,
        ThoiGianBatDau = veGoc.SuKien.ThoiGianBatDau,
        SoTien = nhomVe.Sum(v => v.LoaiVe.GiaVe),

        TenLoaiVe = veGoc.LoaiVe.TenLoai ?? string.Empty,
        DanhSachGhe = string.IsNullOrWhiteSpace(danhSachGhe) ? null : danhSachGhe,
        SoLuongVe = nhomVe.Count
    };

    return View(vm);
}

[Authorize(Roles = "KhachHang")]
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> YeuCauHoanHuy(YeuCauHoanHuyVM model)
{
    int userId = GetCurrentUserId();

    var veGoc = await _db.Ve
        .Include(v => v.SuKien)
        .Include(v => v.LoaiVe)
        .Include(v => v.ChoNgoi)
        .FirstOrDefaultAsync(v => v.VeId == model.VeId && v.NguoiDungId == userId);

    if (veGoc == null) return NotFound();

    var nhomVe = await _db.Ve
        .Include(v => v.SuKien)
        .Include(v => v.LoaiVe)
        .Include(v => v.ChoNgoi)
        .Where(v => v.NguoiDungId == userId
            && (
                veGoc.DonHangId.HasValue
                    ? v.DonHangId == veGoc.DonHangId
                    : v.VeId == veGoc.VeId
            ))
        .ToListAsync();

    if (!nhomVe.Any()) return NotFound();

    string danhSachGhe = string.Join(", ",
        nhomVe.Where(v => v.ChoNgoi != null)
              .Select(v => $"{v.ChoNgoi!.Hang}{v.ChoNgoi.Cot}")
    );

    if (!ModelState.IsValid)
    {
        model.MaVe = veGoc.MaVe;
        model.TenSuKien = veGoc.SuKien.TenSuKien;
        model.ThoiGianBatDau = veGoc.SuKien.ThoiGianBatDau;
        model.SoTien = nhomVe.Sum(v => v.LoaiVe.GiaVe);
        model.TenLoaiVe = veGoc.LoaiVe.TenLoai ?? string.Empty;
        model.DanhSachGhe = string.IsNullOrWhiteSpace(danhSachGhe) ? null : danhSachGhe;
        model.SoLuongVe = nhomVe.Count;

        return View(model);
    }

    if (nhomVe.Any(v => v.TrangThai != "DaThanhToan") || veGoc.SuKien.ThoiGianBatDau < DateTime.Now.AddHours(24))
    {
        TempData["ThongBao"] = "Vé này không đủ điều kiện để yêu cầu hủy.";
        return RedirectToAction("VeCuaToi");
    }

    bool daCoYeuCauChoDuyet = await _db.YeuCauHoanHuyVe
        .AnyAsync(yc => nhomVe.Select(v => v.VeId).Contains(yc.VeId)
                        && yc.TrangThai == "ChoDuyet");

    if (daCoYeuCauChoDuyet)
    {
        TempData["ThongBao"] = "Bạn đã có yêu cầu hủy đang chờ xử lý cho vé này.";
        return RedirectToAction("VeCuaToi");
    }

    foreach (var ve in nhomVe)
    {

var veDaiDien = nhomVe.OrderBy(v => v.VeId).First();

var yeuCau = new YeuCauHoanHuyVe
{
    VeId = veDaiDien.VeId,
    LyDo = model.LyDo,
    NgayYeuCau = DateTime.Now,
    TrangThai = "ChoDuyet",
    HinhThuc = "HuyVe"
};

_db.YeuCauHoanHuyVe.Add(yeuCau);

    }

    await _db.SaveChangesAsync();

    TempData["ThongBao"] = "Gửi yêu cầu hủy vé thành công. Vui lòng chờ hệ thống xử lý.";
    return RedirectToAction("VeCuaToi");
}

        // ============================================================
        // ========== SAU KHI THANH TOÁN THÀNH CÔNG ===================
        // ============================================================

[Authorize(Roles = "KhachHang")]
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> ThanhToanXacNhan(int id)
{
    int userId = GetCurrentUserId();

    await using var transaction =
        await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

    try
    {
        var donHang = await _db.DonHang
            .Include(d => d.Ves)
            .FirstOrDefaultAsync(d =>
                d.DonHangId == id &&
                d.NguoiDungId == userId);

        if (donHang == null)
            return NotFound();

        if (donHang.TrangThai == "ThanhCong")
        {
            await transaction.RollbackAsync();
            TempData["ThongBao"] = "Đơn hàng đã được thanh toán trước đó.";
            return RedirectToAction("ThanhToanThanhCong", new { id });
        }

        if (donHang.TrangThai != "DangXuLy" ||
            !donHang.Ves.Any() ||
            donHang.Ves.Any(v => v.TrangThai != "ChuaThanhToan"))
        {
            await transaction.RollbackAsync();
            TempData["ThongBao"] = "Đơn hàng không còn hợp lệ để thanh toán.";
            return RedirectToAction("VeCuaToi");
        }

        var gheIds = donHang.Ves
            .Where(v => v.ChoNgoiId.HasValue)
            .Select(v => v.ChoNgoiId!.Value)
            .ToList();

        if (gheIds.Any())
        {
            var gheList = await _db.SoDoChoNgoi
                .Where(g => gheIds.Contains(g.ChoNgoiId))
                .ToListAsync();

            bool gheKhongHopLe =
                gheList.Count != gheIds.Count ||
                gheList.Any(g =>
                    g.DonHangGiuId != donHang.DonHangId ||
                    !g.ThoiGianHetHanGiu.HasValue ||
                    g.ThoiGianHetHanGiu.Value <= DateTime.Now ||
                    g.TrangThai != "Trong");

            if (gheKhongHopLe)
            {
                await transaction.RollbackAsync();
                TempData["ThongBao"] =
                    "Thời gian giữ ghế đã hết hạn hoặc ghế không còn thuộc đơn hàng. Vui lòng đặt vé lại.";

                return RedirectToAction("VeCuaToi");
            }

            foreach (var ghe in gheList)
            {
                ghe.TrangThai = "DaDat";
                ghe.DonHangGiuId = null;
                ghe.ThoiGianHetHanGiu = null;
            }
        }

        foreach (var ve in donHang.Ves)
        {
            ve.TrangThai = "DaThanhToan";
        }

        donHang.TrangThai = "ThanhCong";

        _db.ThanhToan.Add(new ThanhToan
        {
            VeId = donHang.Ves.First().VeId,
            PhuongThuc = "QR Demo",
            MaGiaoDich = "DEMO-" + Guid.NewGuid().ToString("N")[..12],
            TrangThai = "ThanhCong",
            NgayThanhToan = DateTime.Now,
            SoTien = donHang.TongTien
        });

        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        TempData["ThongBao"] = "Thanh toán QR Demo thành công.";

        return RedirectToAction("ThanhToanThanhCong", new { id });
    }
    catch (Exception ex)
    {
        await transaction.RollbackAsync();
        _logger.LogError(ex, "Lỗi xác nhận thanh toán QR Demo.");

        TempData["ThongBao"] = "Thanh toán thất bại. Vui lòng thử lại.";

        return RedirectToAction("ThanhToan", new { id });
    }
}

        [Authorize(Roles = "KhachHang")]
        public async Task<IActionResult> ThanhToanThanhCong(int id)
        {
            int userId = GetCurrentUserId();

            var donHang = await _db.DonHang
                .FirstOrDefaultAsync(d => d.DonHangId == id && d.NguoiDungId == userId);

            if (donHang == null) return NotFound();

            return View(donHang);
        }
    }
}
