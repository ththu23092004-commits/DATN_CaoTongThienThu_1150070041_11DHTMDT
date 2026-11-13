using System.Security.Claims;
using System.IO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using VeSuKienWeb.Data;
using VeSuKienWeb.Models;
using VeSuKienWeb.Models.BanToChuc;
using System.Text.Json;

namespace VeSuKienWeb.Controllers
{
    [Authorize(Roles = "ToChuc")]
    public class BanToChucController : Controller
    {
        private readonly NguCanhSuKien _db;

        public BanToChucController(NguCanhSuKien db)
        {
            _db = db;
        }

public sealed class SoDoDuLieu
{
    public int Width { get; set; }
    public int Height { get; set; }
    public string? Layout { get; set; }
    public List<GheDuLieu> Seats { get; set; } = new();
    public List<ThanhPhanDuLieu> Components { get; set; } = new();

    public SoDoDuLieu() { }
}

public sealed class GheDuLieu
{
    public string Row { get; set; } = "";
    public int Col { get; set; }
    public string Code { get; set; } = "";
    public decimal X { get; set; }
    public decimal Y { get; set; }
    public string Zone { get; set; } = "";

    public GheDuLieu() { }
}

public sealed class ThanhPhanDuLieu
{
    public string Type { get; set; } = "";
    public string Name { get; set; } = "";
    public decimal X { get; set; }
    public decimal Y { get; set; }
    public decimal Width { get; set; }
    public decimal Height { get; set; }

    public ThanhPhanDuLieu() { }
}

private static SoDoDuLieu? DocSoDo(string? json)
{
    if (string.IsNullOrWhiteSpace(json) || json.Length > 200000)
        return null;

    try
    {
        var data = JsonSerializer.Deserialize<SoDoDuLieu>(
            json,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

        if (data == null ||
            data.Width != 1000 ||
            data.Height != 700 ||
            data.Seats == null ||
            data.Components == null ||
            data.Seats.Count is < 1 or > 1200 ||
            data.Components.Count > 20)
        {
            return null;
        }

        var codes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var seat in data.Seats)
        {
            if (seat == null ||
                string.IsNullOrWhiteSpace(seat.Row) ||
                seat.Row.Length != 1 ||
                seat.Row[0] < 'A' ||
                seat.Row[0] > 'Y' ||
                seat.Col < 1 ||
                seat.Col > 40 ||
                seat.Code != $"{seat.Row}{seat.Col}" ||
                (seat.Zone != "VIP" && seat.Zone != "Thuong") ||
                seat.X < 0 || seat.X > 1000 ||
                seat.Y < 0 || seat.Y > 700 ||
                !codes.Add(seat.Code))
            {
                return null;
            }
        }

var allowed = new HashSet<string>
{
    "SanKhau",
    "ManHinh",
    "LoiDi",
    "CuaVao",
    "CuaThoatHiem",
    "VatCan",
    "BanTron",
    "BanChuU",
    "BanChuToa",
    "CuaRaVao"
};

        foreach (var part in data.Components)
        {
            if (part == null ||
                !allowed.Contains(part.Type) ||
                string.IsNullOrWhiteSpace(part.Name) ||
                part.Name.Length > 100 ||
                part.X < 0 ||
                part.Y < 0 ||
                part.Width <= 0 ||
                part.Height <= 0 ||
                part.X + part.Width > 1000 ||
                part.Y + part.Height > 700)
            {
                return null;
            }
        }

        return data;
    }
catch (JsonException)
{
    return null;
}
catch (NotSupportedException)
{
    return null;
}
}
private static string TaoTenHangGhe(int index)
{
    var result = string.Empty;
    index++;
    while (index > 0)
    {
        index--;
        result = (char)('A' + (index % 26)) + result;
        index /= 26;
    }
    return result;
}

private static async Task<string?> KiemTraAnhSuKienAsync(IFormFile file)
{
    if (file.Length <= 0 || file.Length > 5 * 1024 * 1024)
        return null;

    var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
    if (ext is not ".jpg" and not ".jpeg" and not ".png" and not ".webp")
        return null;

    var header = new byte[12];
    await using var stream = file.OpenReadStream();
    var read = await stream.ReadAsync(header, 0, header.Length);

    bool jpg = read >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF;
    bool png = read >= 8 && header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47 && header[4] == 0x0D && header[5] == 0x0A && header[6] == 0x1A && header[7] == 0x0A;
    bool webp = read >= 12 && header[0] == 0x52 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x46 && header[8] == 0x57 && header[9] == 0x45 && header[10] == 0x42 && header[11] == 0x50;

    if ((ext is ".jpg" or ".jpeg") && jpg) return ".jpg";
    if (ext == ".png" && png) return ".png";
    if (ext == ".webp" && webp) return ".webp";
    return null;
}

private async Task NapDanhSachChuDeAsync()
{
    ViewBag.ChuDes = await _db.ChuDeSuKien
        .AsNoTracking()
        .Where(x => x.TrangThai)
        .OrderBy(x => x.TenChuDe)
        .ToListAsync();
}
        private int GetCurrentUserId()
        
        {
            var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(idStr))
            {
                throw new InvalidOperationException("Không tìm thấy Id người dùng đang đăng nhập.");
            }
            return int.Parse(idStr);
        }

        private async Task<int> GetOrCreateToChucIdAsync()
        {
            var userId = GetCurrentUserId();
            

            var toChuc = await _db.ToChucSuKien.FirstOrDefaultAsync(t => t.NguoiDungId == userId);
            if (toChuc != null) return toChuc.ToChucId;

            var user = await _db.NguoiDung.FindAsync(userId);
            toChuc = new ToChucSuKien
            {
                TenToChuc = user?.HoTen ?? "Tổ chức sự kiện",
                EmailLienHe = user?.Email,
                NguoiDungId = userId
            };

            _db.ToChucSuKien.Add(toChuc);
            await _db.SaveChangesAsync();

            return toChuc.ToChucId;
        }

        // ========= TRANG DANH SÁCH SỰ KIỆN CỦA TỔ CHỨC =========
        // => Chính là "Danh sách sự kiện của tôi"
        public async Task<IActionResult> Index(string? trangThaiDuyet, string? thoiGian)
        {
            var toChucId = await GetOrCreateToChucIdAsync();

            var query = _db.SuKien
                .Include(s => s.LoaiSuKien)
                .Where(s => s.ToChucId == toChucId)
                .AsQueryable();

            if (!string.IsNullOrEmpty(trangThaiDuyet))
            {
                query = query.Where(s => s.TrangThaiDuyet == trangThaiDuyet);
            }

            if (!string.IsNullOrEmpty(thoiGian))
            {
                var now = DateTime.Now;
                if (thoiGian == "SapDienRa")
                {
                    query = query.Where(s => s.ThoiGianKetThuc >= now);
                }
                else if (thoiGian == "DaDienRa")
                {
                    query = query.Where(s => s.ThoiGianKetThuc < now);
                }
            }

            var list = await query
                .OrderByDescending(s => s.NgayDang)
                .ToListAsync();

            ViewBag.TrangThaiDuyet = trangThaiDuyet;
            ViewBag.ThoiGian = thoiGian;

            return View(list);
        }

        // ========= XÓA / ẨN SỰ KIỆN (THEO NGHIỆP VỤ BẠN YÊU CẦU) =========
        [HttpPost]
        [ValidateAntiForgeryToken]
        
        public async Task<IActionResult> XoaSuKien(int id)
        {
            var toChucId = await GetOrCreateToChucIdAsync();

            var sk = await _db.SuKien
                .FirstOrDefaultAsync(s => s.SuKienId == id && s.ToChucId == toChucId);

            if (sk == null)
            {
                TempData["ThongBao"] = "Không tìm thấy sự kiện.";
                return RedirectToAction("Index");
            }

            // Kiểm tra xem sự kiện đã có vé nào chưa
            bool daCoVe = await _db.Ve.AnyAsync(v => v.SuKienId == sk.SuKienId);

            // 🔸 Nếu ĐÃ CÓ VÉ → chỉ ẨN (TrangThaiDuyet = 'An')
            if (daCoVe)
            {
                if (sk.TrangThaiDuyet != "An")
                {
                    sk.TrangThaiDuyet = "An";
                    await _db.SaveChangesAsync();
                    TempData["ThongBao"] = "Sự kiện đã được ẩn khỏi danh sách hiển thị cho khách hàng.";
                }
                else
                {
                    TempData["ThongBao"] = "Sự kiện này đã được ẩn trước đó.";
                }

                return RedirectToAction("Index");
            }

            // 🔹 Nếu CHƯA CÓ VÉ → XÓA HẲN
            var suKienId = sk.SuKienId;

            // Xóa các cấu hình con phụ thuộc (tránh lỗi FK)
            // 1. Sự kiện trực tuyến
            var onlineConfigs = await _db.SuKienTrucTuyen
                .Where(o => o.SuKienId == suKienId)
                .ToListAsync();
            if (onlineConfigs.Any())
                _db.SuKienTrucTuyen.RemoveRange(onlineConfigs);

            // 2. Địa điểm + sơ đồ chỗ ngồi
            var diaDiems = await _db.DiaDiem
                .Where(d => d.SuKienId == suKienId)
                .ToListAsync();
            if (diaDiems.Any())
            {
                var diaDiemIds = diaDiems.Select(d => d.DiaDiemId).ToList();

                var gheList = await _db.SoDoChoNgoi
                    .Where(g => diaDiemIds.Contains(g.DiaDiemId))
                    .ToListAsync();

                if (gheList.Any())
                    _db.SoDoChoNgoi.RemoveRange(gheList);

                _db.DiaDiem.RemoveRange(diaDiems);
            }

            // 3. Loại vé
            var loaiVes = await _db.LoaiVe
                .Where(l => l.SuKienId == suKienId)
                .ToListAsync();
            if (loaiVes.Any())
                _db.LoaiVe.RemoveRange(loaiVes);

            // Chỉ xóa hẳn khi chưa phát sinh vé; mọi thao tác xóa nằm trong cùng transaction.
            await using var deleteTransaction = await _db.Database.BeginTransactionAsync();

            // Xóa các dữ liệu phụ thuộc không gắn với vé.
            await _db.Database.ExecuteSqlInterpolatedAsync(
                $"DELETE FROM dbo.NhacSuKien WHERE SuKienId = {suKienId}");
            await _db.Database.ExecuteSqlInterpolatedAsync(
                $"DELETE FROM dbo.DanhGiaSuKien WHERE SuKienId = {suKienId}");

            // Xóa các dữ liệu phụ thuộc trực tiếp của sơ đồ và chủ đề.
            var soDo = await _db.SoDoSuKien
                .FirstOrDefaultAsync(x => x.SuKienId == suKienId);
            if (soDo != null)
            {
                var khuVucs = await _db.KhuVucChoNgoi.Where(x => x.SoDoId == soDo.SoDoId).ToListAsync();
                var thanhPhans = await _db.ThanhPhanSoDo.Where(x => x.SoDoId == soDo.SoDoId).ToListAsync();
                _db.KhuVucChoNgoi.RemoveRange(khuVucs);
                _db.ThanhPhanSoDo.RemoveRange(thanhPhans);
                _db.SoDoSuKien.Remove(soDo);
            }

            var lienKetChuDe = await _db.SuKienChuDe
                .Where(x => x.SuKienId == suKienId)
                .ToListAsync();
            _db.SuKienChuDe.RemoveRange(lienKetChuDe);

            _db.SuKien.Remove(sk);
            try
            {
                await _db.SaveChangesAsync();
                await deleteTransaction.CommitAsync();
                TempData["ThongBao"] = "Đã xóa sự kiện thành công.";
            }
            catch (DbUpdateException)
            {
                await deleteTransaction.RollbackAsync();
                _db.ChangeTracker.Clear();
                var eventAgain = await _db.SuKien.FirstOrDefaultAsync(x => x.SuKienId == suKienId && x.ToChucId == toChucId);
                if (eventAgain != null)
                {
                    eventAgain.TrangThaiDuyet = "An";
                    await _db.SaveChangesAsync();
                }
                TempData["ThongBao"] = "Sự kiện còn dữ liệu liên quan nên hệ thống đã ẩn sự kiện thay vì xóa hẳn.";
            }

            return RedirectToAction("Index");
        }

        // ========= GET: TẠO MỚI SỰ KIỆN =========
        [HttpGet]
        public async Task<IActionResult> TaoSuKien()
        {
            // Đảm bảo luôn có đủ các loại sự kiện mặc định trong dropdown.
            var defaultLoai = new[] { "Trực tiếp", "Trực tuyến", "Workshop" };
            var existingLoai = await _db.LoaiSuKien
                .Select(x => x.TenLoai)
                .ToListAsync();

            var needAdds = defaultLoai
                .Where(name => !existingLoai.Any(e => string.Equals((e ?? string.Empty).Trim(), name, StringComparison.OrdinalIgnoreCase)))
                .Select(name => new LoaiSuKien { TenLoai = name })
                .ToList();

            if (needAdds.Any())
            {
                _db.LoaiSuKien.AddRange(needAdds);
                await _db.SaveChangesAsync();
            }

            var loaiHienThi = await _db.LoaiSuKien
                .Where(x => x.TenLoai != null &&
                            !x.TenLoai.Trim().ToLower().Contains("hybrid"))
                .ToListAsync();

            ViewBag.LoaiId = new SelectList(loaiHienThi, "LoaiId", "TenLoai");
await NapDanhSachChuDeAsync();
            var model = new TaoSuKienYeuCau
            {
                ThoiGianBatDau = DateTime.Now.AddDays(1),
                ThoiGianKetThuc = DateTime.Now.AddDays(1).AddHours(2),
                TenLoaiVe1 = null,
                TenLoaiVe2 = "Thường",

                SoHang = 6,
                SoCotMoiHang = 10,
                HangVipTu = 1,
                HangVipDen = 2
            };

            return View(model);
        }

        // ========= POST: TẠO MỚI SỰ KIỆN =========
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TaoSuKien(TaoSuKienYeuCau model, IFormFile? AnhSuKienFile)
        {
            model.ChuDeIds = (model.ChuDeIds ?? new List<int>())
    .Distinct()
    .ToList();

            string? anhExtension = null;
            if (AnhSuKienFile != null && AnhSuKienFile.Length > 0)
            {
                anhExtension = await KiemTraAnhSuKienAsync(AnhSuKienFile);
                if (anhExtension == null)
                    ModelState.AddModelError("AnhSuKienFile", "Ảnh phải là JPG, PNG hoặc WEBP hợp lệ và không vượt quá 5 MB.");
            }

var chuDeHopLe = await _db.ChuDeSuKien
    .Where(x => model.ChuDeIds.Contains(x.ChuDeId) && x.TrangThai)
    .Select(x => x.ChuDeId)
    .ToListAsync();

if (model.ChuDeIds.Count == 0)
{
    ModelState.AddModelError(
        nameof(model.ChuDeIds),
        "Vui lòng chọn ít nhất một chủ đề cho sự kiện.");
}
else if (chuDeHopLe.Count != model.ChuDeIds.Count)
{
    ModelState.AddModelError(
        nameof(model.ChuDeIds),
        "Có chủ đề không hợp lệ hoặc đã ngừng hoạt động.");
}
            if (!ModelState.IsValid)
            {
                var loaiHienThi = await _db.LoaiSuKien
                    .Where(x => x.TenLoai != null &&
                                !x.TenLoai.Trim().ToLower().Contains("hybrid"))
                    .ToListAsync();
                ViewBag.LoaiId = new SelectList(loaiHienThi, "LoaiId", "TenLoai", model.LoaiId);
                await NapDanhSachChuDeAsync();
                return View(model);
            }

            if (model.ThoiGianKetThuc <= model.ThoiGianBatDau)
            {
                ModelState.AddModelError(string.Empty, "Thời gian kết thúc phải sau thời gian bắt đầu.");
                var loaiHienThi = await _db.LoaiSuKien
                    .Where(x => x.TenLoai != null &&
                                !x.TenLoai.Trim().ToLower().Contains("hybrid"))
                    .ToListAsync();
                ViewBag.LoaiId = new SelectList(loaiHienThi, "LoaiId", "TenLoai", model.LoaiId);
                await NapDanhSachChuDeAsync();
                return View(model);
            }

            var toChucId = await GetOrCreateToChucIdAsync();

            var loai = await _db.LoaiSuKien.FirstOrDefaultAsync(l => l.LoaiId == model.LoaiId);
            if (loai == null)
            {
                ModelState.AddModelError("LoaiId", "Loại sự kiện không hợp lệ.");
                var loaiHienThi = await _db.LoaiSuKien
                    .Where(x => x.TenLoai != null &&
                                !x.TenLoai.Trim().ToLower().Contains("hybrid"))
                    .ToListAsync();
                ViewBag.LoaiId = new SelectList(loaiHienThi, "LoaiId", "TenLoai", model.LoaiId);
                await NapDanhSachChuDeAsync();
                return View(model);
            }

            var tenLoaiLower = (loai.TenLoai ?? "").Trim().ToLower();

            bool laTrucTiep = tenLoaiLower.Contains("trực tiếp") || tenLoaiLower.Contains("truc tiep");
            bool laWorkshop = tenLoaiLower.Contains("workshop");


SoDoDuLieu? soDoWorkshop = null;

if (laWorkshop)
{
    soDoWorkshop = DocSoDo(model.SoDoJson);

    if (soDoWorkshop == null)
    {
        ModelState.AddModelError(
            "SoDoJson",
            "Sơ đồ Workshop không hợp lệ. Vui lòng thiết kế lại.");
    }
    else
    {
        string layout = (soDoWorkshop.Layout ?? "")
            .Trim()
            .ToLowerInvariant();

        int soBanTron = soDoWorkshop.Components.Count(
            x => x.Type == "BanTron");

        int soThanhPhanBanU = soDoWorkshop.Components.Count(
            x => x.Type == "BanChuU");

        if (layout == "u")
        {
            if (soThanhPhanBanU != 3 || soBanTron != 0)
            {
                ModelState.AddModelError(
                    "SoDoJson",
                    "Sơ đồ bàn chữ U phải có đủ 3 cạnh và không được có bàn tròn.");
            }
        }
        else if (layout == "round")
        {
            if (soBanTron < 1 || soThanhPhanBanU != 0)
            {
                ModelState.AddModelError(
                    "SoDoJson",
                    "Sơ đồ bàn tròn không hợp lệ.");
            }
        }
        else
        {
            ModelState.AddModelError(
                "SoDoJson",
                "Kiểu bố trí Workshop không hợp lệ.");
        }

        int tongGhe = soDoWorkshop.Seats.Count;

        if (soDoWorkshop.Seats.Any(x => x.Zone != "Thuong"))
        {
            ModelState.AddModelError(
                "SoDoJson",
                "Workshop chỉ được sử dụng ghế thường.");
        }

        if (model.SoChoNgoi != tongGhe)
        {
            ModelState.AddModelError(
                "SoChoNgoi",
                "Tổng số chỗ không khớp với số ghế trên sơ đồ.");
        }

        // Workshop chỉ có một loại vé Thường.
        if (!string.IsNullOrWhiteSpace(model.TenLoaiVe1) ||
            model.GiaVe1.GetValueOrDefault() != 0 ||
            model.SoLuongVe1.GetValueOrDefault() != 0)
        {
            ModelState.AddModelError(
                "TenLoaiVe1",
                "Workshop không sử dụng vé VIP.");
        }

        if (string.IsNullOrWhiteSpace(model.TenLoaiVe2) ||
            !model.GiaVe2.HasValue ||
            model.GiaVe2.Value <= 0 ||
            model.SoLuongVe2.GetValueOrDefault() != tongGhe)
        {
            ModelState.AddModelError(
                "TenLoaiVe2",
                "Workshop cần một loại vé Thường có giá hợp lệ và số lượng bằng tổng số ghế.");
        }
    }
}


            // ===== VALIDATE THEO LOẠI SỰ KIỆN =====
            if (laTrucTiep || laWorkshop)
            {
                if (string.IsNullOrWhiteSpace(model.TenDiaDiem))
                    ModelState.AddModelError("TenDiaDiem", "Vui lòng nhập tên địa điểm.");

                if (model.SoChoNgoi == null || model.SoChoNgoi < 1 || model.SoChoNgoi > 1200)
                    ModelState.AddModelError("SoChoNgoi", "Vui lòng nhập tổng số chỗ từ 1 đến 1200.");

                bool coVe1 = !string.IsNullOrWhiteSpace(model.TenLoaiVe1);
                bool coVe2 = !string.IsNullOrWhiteSpace(model.TenLoaiVe2);

                if (!coVe1 && !coVe2)
                    ModelState.AddModelError("TenLoaiVe1", "Vui lòng khai báo ít nhất 1 loại vé.");

                if (coVe1)
                {
                    if (model.GiaVe1 == null || model.GiaVe1 <= 0)
                        ModelState.AddModelError("GiaVe1", "Giá vé 1 phải > 0.");
                    if (model.SoLuongVe1 == null || model.SoLuongVe1 <= 0)
                        ModelState.AddModelError("SoLuongVe1", "Số lượng vé 1 phải > 0.");
                }

                if (coVe2)
                {
                    if (model.GiaVe2 == null || model.GiaVe2 <= 0)
                        ModelState.AddModelError("GiaVe2", "Giá vé 2 phải > 0.");
                    if (model.SoLuongVe2 == null || model.SoLuongVe2 <= 0)
                        ModelState.AddModelError("SoLuongVe2", "Số lượng vé 2 phải > 0.");
                }

                int tongVe =
                    (model.SoLuongVe1 ?? 0) +
                    (model.SoLuongVe2 ?? 0);

                if (model.SoChoNgoi.HasValue && tongVe != model.SoChoNgoi.Value)
                {
                    ModelState.AddModelError("SoChoNgoi", "Tổng số lượng vé VIP và Thường phải bằng tổng số chỗ của Workshop.");
                }

                if (coVe1 && (model.GiaVe1 == null || model.GiaVe1 <= 0 || model.SoLuongVe1 == null || model.SoLuongVe1 <= 0))
                    ModelState.AddModelError("TenLoaiVe1", "Nếu sử dụng vé VIP, vui lòng nhập tên, giá và số lượng VIP hợp lệ.");

                if (coVe2 && (model.GiaVe2 == null || model.GiaVe2 <= 0 || model.SoLuongVe2 == null || model.SoLuongVe2 <= 0))
                    ModelState.AddModelError("TenLoaiVe2", "Nếu sử dụng vé Thường, vui lòng nhập tên, giá và số lượng vé Thường hợp lệ.");

                if (!laWorkshop)
                {
                    if (model.SoHang == null || model.SoHang <= 0)
                        ModelState.AddModelError("SoHang", "Vui lòng nhập số hàng ghế > 0.");

                    if (model.SoCotMoiHang == null || model.SoCotMoiHang <= 0)
                        ModelState.AddModelError("SoCotMoiHang", "Vui lòng nhập số ghế mỗi hàng > 0.");

var soDoNhap = DocSoDo(model.SoDoJson);

if (soDoNhap == null)
{
    ModelState.AddModelError(
        "SoDoJson",
        "Sơ đồ chỗ ngồi không hợp lệ. Vui lòng kiểm tra lại.");
}
else
{
    int tongGhe = soDoNhap.Seats.Count;
    int soGheVip = soDoNhap.Seats.Count(x => x.Zone == "VIP");
    int soGheThuong = soDoNhap.Seats.Count(x => x.Zone == "Thuong");

    if (model.SoChoNgoi != tongGhe)
    {
        ModelState.AddModelError(
            "SoChoNgoi",
            "Tổng số ghế không khớp với sơ đồ.");
    }

    if (soGheVip > 0)
    {
        if (string.IsNullOrWhiteSpace(model.TenLoaiVe1) ||
            !model.GiaVe1.HasValue || model.GiaVe1 <= 0 ||
            model.SoLuongVe1 != soGheVip)
        {
            ModelState.AddModelError("TenLoaiVe1", "Sơ đồ có ghế VIP nên cần khai báo tên, giá và số lượng vé VIP đúng bằng số ghế VIP.");
        }
    }
    else if ((model.SoLuongVe1 ?? 0) > 0)
    {
        ModelState.AddModelError("SoLuongVe1", "Sơ đồ không có ghế VIP nên số lượng vé VIP phải bằng 0.");
    }

    if (soGheThuong > 0)
    {
        if (string.IsNullOrWhiteSpace(model.TenLoaiVe2) ||
            !model.GiaVe2.HasValue || model.GiaVe2 <= 0 ||
            model.SoLuongVe2 != soGheThuong)
        {
            ModelState.AddModelError("TenLoaiVe2", "Sơ đồ có ghế Thường nên cần khai báo tên, giá và số lượng vé Thường đúng bằng số ghế Thường.");
        }
    }
    else if ((model.SoLuongVe2 ?? 0) > 0)
    {
        ModelState.AddModelError("SoLuongVe2", "Sơ đồ không có ghế Thường nên số lượng vé Thường phải bằng 0.");
    }

    if (model.SoLuongToiDa.HasValue &&
        model.SoLuongToiDa.Value != tongGhe)
    {
        ModelState.AddModelError(
            "SoLuongToiDa",
            "Sức chứa phải bằng tổng số ghế thực tế.");
    }
}

                }
            }
            else if (tenLoaiLower.Contains("trực tuyến"))
            {
                // ⭐ VALIDATE SỰ KIỆN TRỰC TUYẾN
                if (string.IsNullOrWhiteSpace(model.NenTang) || string.IsNullOrWhiteSpace(model.LinkXem))
                    ModelState.AddModelError("NenTang", "Vui lòng nhập đầy đủ nền tảng và link xem cho sự kiện trực tuyến.");

                // BẮT BUỘC PHẢI CÓ ÍT NHẤT 1 LOẠI VÉ ONLINE (TenLoaiVe1)
                bool coVeOnline = !string.IsNullOrWhiteSpace(model.TenLoaiVe1);

                if (!coVeOnline)
                {
                    ModelState.AddModelError("TenLoaiVe1", "Vui lòng khai báo tên loại vé cho sự kiện trực tuyến.");
                }
                else
                {
                    if (!model.GiaVe1.HasValue || model.GiaVe1 <= 0)
                        ModelState.AddModelError("GiaVe1", "Giá vé phải > 0 cho loại vé trực tuyến.");

                    if (!model.SoLuongVe1.HasValue || model.SoLuongVe1 <= 0)
                        ModelState.AddModelError("SoLuongVe1", "Số lượng vé phải > 0 cho loại vé trực tuyến.");
                }
            }

            if (!ModelState.IsValid)
            {
                var loaiHienThi = await _db.LoaiSuKien
                    .Where(x => x.TenLoai != null &&
                                !x.TenLoai.Trim().ToLower().Contains("hybrid"))
                    .ToListAsync();
                ViewBag.LoaiId = new SelectList(loaiHienThi, "LoaiId", "TenLoai", model.LoaiId);
                await NapDanhSachChuDeAsync();
                return View(model);
            }

            await using var transaction = await _db.Database.BeginTransactionAsync();
            string? anhDaLuu = null;

            try
            {
            var sk = new SuKien
            {
                TenSuKien = model.TenSuKien,
                MoTaChiTiet = model.MoTaChiTiet,
                ThoiGianBatDau = model.ThoiGianBatDau,
                ThoiGianKetThuc = model.ThoiGianKetThuc,
                LoaiId = model.LoaiId,
                ToChucId = toChucId,
SoLuongToiDa = laTrucTiep
    ? DocSoDo(model.SoDoJson)?.Seats.Count
    : laWorkshop
        ? model.SoChoNgoi
        : model.SoLuongToiDa,

GiaVeMacDinh = laTrucTiep
    ? null
    : model.GiaVeMacDinh,
                TrangThaiDuyet = "ChoDuyet",
                NgayDang = DateTime.Now
            };

            // ⭐ XỬ LÝ ẢNH SỰ KIỆN (NẾU CÓ UPLOAD)
            if (AnhSuKienFile != null && AnhSuKienFile.Length > 0)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "sukien");
                Directory.CreateDirectory(uploadsFolder);

                var fileName = $"{Guid.NewGuid():N}{anhExtension}";
                var filePath = Path.Combine(uploadsFolder, fileName);
                await using (var stream = new FileStream(filePath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    await AnhSuKienFile.CopyToAsync(stream);
                }

                anhDaLuu = filePath;
                sk.AnhSuKien = $"/uploads/sukien/{fileName}";
            }

            _db.SuKien.Add(sk);
            await _db.SaveChangesAsync(); // SuKienId
if (model.ChuDeIds.Any())
{
    var danhSachChuDe = model.ChuDeIds.Select(chuDeId =>
        new SuKienChuDe
        {
            SuKienId = sk.SuKienId,
            ChuDeId = chuDeId
        }).ToList();

    _db.SuKienChuDe.AddRange(danhSachChuDe);
}
            if (laTrucTiep || laWorkshop)
            {
                var diaDiem = new DiaDiem
                {
                    SuKienId = sk.SuKienId,
                    TenDiaDiem = model.TenDiaDiem,
                    DiaChi = model.DiaChi,
                    SoChoNgoi = model.SoChoNgoi ?? 0,
                    BanDo = model.BanDo
                };
                _db.DiaDiem.Add(diaDiem);

                LoaiVe? loaiVeVip = null;
                LoaiVe? loaiVeThuong = null;

                if (!string.IsNullOrWhiteSpace(model.TenLoaiVe1) &&
                    model.GiaVe1.HasValue && model.SoLuongVe1.HasValue)
                {
                    loaiVeVip = new LoaiVe
                    {
                        SuKienId = sk.SuKienId,
                        TenLoai = model.TenLoaiVe1,
                        GiaVe = model.GiaVe1.Value,
                        SoLuong = model.SoLuongVe1.Value,
                        MoTa = "Khu vé VIP."
                    };
                    _db.LoaiVe.Add(loaiVeVip);
                }

                if (!string.IsNullOrWhiteSpace(model.TenLoaiVe2) &&
                    model.GiaVe2.HasValue && model.SoLuongVe2.HasValue)
                {
                    loaiVeThuong = new LoaiVe
                    {
                        SuKienId = sk.SuKienId,
                        TenLoai = model.TenLoaiVe2,
                        GiaVe = model.GiaVe2.Value,
                        SoLuong = model.SoLuongVe2.Value,
                        MoTa = "Khu vé Thường."
                    };
                    _db.LoaiVe.Add(loaiVeThuong);
                }

                await _db.SaveChangesAsync();

               if (laWorkshop)
{
var workshopData = soDoWorkshop
    ?? throw new InvalidOperationException(
        "Không đọc được sơ đồ Workshop.");

    if (loaiVeThuong == null)
    {
        throw new InvalidOperationException(
            "Không tìm thấy loại vé Thường của Workshop.");
    }

if (workshopData.Seats.Any(x => x.Zone != "Thuong"))
{
    throw new InvalidOperationException(
        "Workshop không được có ghế VIP.");
}

    var soDo = new SoDoSuKien
    {
        SuKienId = sk.SuKienId,
        ChieuRong = workshopData.Width,
        ChieuCao = workshopData.Height,
        TrangThai = "BanNhap",
        NgayCapNhat = DateTime.UtcNow
    };

    _db.SoDoSuKien.Add(soDo);
    await _db.SaveChangesAsync();

    foreach (var part in workshopData.Components)
    {
        _db.ThanhPhanSoDo.Add(new ThanhPhanSoDo
        {
            SoDoId = soDo.SoDoId,
            LoaiThanhPhan = part.Type,
            TenHienThi = part.Name,
            ViTriX = part.X,
            ViTriY = part.Y,
            ChieuRong = part.Width,
            ChieuCao = part.Height,
            GocXoay = 0
        });
    }

    foreach (var seat in workshopData.Seats)
    {
        _db.SoDoChoNgoi.Add(new SoDoChoNgoi
        {
            DiaDiemId = diaDiem.DiaDiemId,
            Hang = seat.Row,
            Cot = seat.Col,
            TrangThai = "Trong",
            LoaiVeId = loaiVeThuong.LoaiVeId,
            ViTriX = seat.X,
            ViTriY = seat.Y
        });
    }

    await _db.SaveChangesAsync();
}

else if (laTrucTiep)
{
    var soDoNhap = DocSoDo(model.SoDoJson);

    if (soDoNhap == null)
        throw new InvalidOperationException("Dữ liệu sơ đồ không hợp lệ.");

    bool coGheVip = soDoNhap.Seats.Any(x => x.Zone == "VIP");
    bool coGheThuong = soDoNhap.Seats.Any(x => x.Zone == "Thuong");

    if ((coGheVip && loaiVeVip == null) || (coGheThuong && loaiVeThuong == null))
        throw new InvalidOperationException("Loại vé chưa khớp với khu vực ghế trong sơ đồ.");

    var soDo = new SoDoSuKien
    {
        SuKienId = sk.SuKienId,
        ChieuRong = soDoNhap.Width,
        ChieuCao = soDoNhap.Height,
        TrangThai = "BanNhap",
        NgayCapNhat = DateTime.UtcNow
    };

    _db.SoDoSuKien.Add(soDo);
    await _db.SaveChangesAsync();

    KhuVucChoNgoi? khuVip = null;
    KhuVucChoNgoi? khuThuong = null;

    if (coGheVip)
    {
        khuVip = new KhuVucChoNgoi
        {
            SoDoId = soDo.SoDoId,
            TenKhu = "VIP",
            MauSac = "#D4AF37",
            LoaiVeId = loaiVeVip!.LoaiVeId,
            ThuTu = 1
        };
        _db.KhuVucChoNgoi.Add(khuVip);
    }

    if (coGheThuong)
    {
        khuThuong = new KhuVucChoNgoi
        {
            SoDoId = soDo.SoDoId,
            TenKhu = "Thường",
            MauSac = "#A9DCA9",
            LoaiVeId = loaiVeThuong!.LoaiVeId,
            ThuTu = 2
        };
        _db.KhuVucChoNgoi.Add(khuThuong);
    }

    await _db.SaveChangesAsync();

    foreach (var seat in soDoNhap.Seats)
    {
        bool vip = seat.Zone == "VIP";

        _db.SoDoChoNgoi.Add(new SoDoChoNgoi
        {
            DiaDiemId = diaDiem.DiaDiemId,
            Hang = seat.Row,
            Cot = seat.Col,
            TrangThai = "Trong",
            LoaiVeId = vip
                ? loaiVeVip!.LoaiVeId
                : loaiVeThuong!.LoaiVeId,
            KhuVucId = vip
                ? khuVip!.KhuVucId
                : khuThuong!.KhuVucId,
            ViTriX = seat.X,
            ViTriY = seat.Y
        });
    }

    foreach (var part in soDoNhap.Components)
    {
        _db.ThanhPhanSoDo.Add(new ThanhPhanSoDo
        {
            SoDoId = soDo.SoDoId,
            LoaiThanhPhan = part.Type,
            TenHienThi = part.Name,
            ViTriX = part.X,
            ViTriY = part.Y,
            ChieuRong = part.Width,
            ChieuCao = part.Height,
            GocXoay = 0
        });
    }

    await _db.SaveChangesAsync();
}

            }
            else if (tenLoaiLower.Contains("trực tuyến"))
            {
                var trucTuyen = new SuKienTrucTuyen
                {
                    SuKienId = sk.SuKienId,
                    NenTang = model.NenTang,
                    LinkXem = model.LinkXem,
                    MatKhauPhong = model.MatKhauPhong,
                    HuongDan = model.HuongDan
                };
                _db.SuKienTrucTuyen.Add(trucTuyen);

                if (!string.IsNullOrWhiteSpace(model.TenLoaiVe1) &&
                    model.GiaVe1.HasValue && model.SoLuongVe1.HasValue)
                {
                    var lvOnline = new LoaiVe
                    {
                        SuKienId = sk.SuKienId,
                        TenLoai = model.TenLoaiVe1,
                        GiaVe = model.GiaVe1.Value,
                        SoLuong = model.SoLuongVe1.Value,
                        MoTa = "Vé tham gia sự kiện trực tuyến."
                    };
                    _db.LoaiVe.Add(lvOnline);
                }

                await _db.SaveChangesAsync();
            }

            await _db.SaveChangesAsync();
await transaction.CommitAsync();

            TempData["ThongBao"] = "Đã gửi sự kiện cho Admin duyệt.";
            return RedirectToAction("Index");
            }
catch (Exception ex)
{
    await transaction.RollbackAsync();

    if (!string.IsNullOrWhiteSpace(anhDaLuu) &&
        System.IO.File.Exists(anhDaLuu))
    {
        System.IO.File.Delete(anhDaLuu);
    }

    TempData["ThongBao"] =
        "Lỗi tạo sự kiện: " + ex.GetBaseException().Message;

    return RedirectToAction("TaoSuKien");
}
        }

        // ========= CHI TIẾT SỰ KIỆN CHO BAN TỔ CHỨC + THỐNG KÊ ĐƠN GIẢN =========
        public async Task<IActionResult> ChiTiet(int id)
        {
            var toChucId = await GetOrCreateToChucIdAsync();

            var sk = await _db.SuKien
                .Include(s => s.LoaiSuKien)
                .Include(s => s.ToChucSuKien)
                .FirstOrDefaultAsync(s => s.SuKienId == id && s.ToChucId == toChucId);

            if (sk == null) return NotFound();

            var diaDiem = await _db.DiaDiem.FirstOrDefaultAsync(d => d.SuKienId == sk.SuKienId);
            var online = await _db.SuKienTrucTuyen.FirstOrDefaultAsync(o => o.SuKienId == sk.SuKienId);

            var loaiVes = await _db.LoaiVe
                .Where(l => l.SuKienId == sk.SuKienId)
                .ToListAsync();

            // ==== THỐNG KÊ THEO LOẠI VÉ ====
            var veTheoLoai = await _db.Ve
                .Where(v => v.SuKienId == sk.SuKienId &&
                            (v.TrangThai == "DaThanhToan" || v.TrangThai == "DaCheckIn"))
                .GroupBy(v => v.LoaiVeId)
                .Select(g => new { LoaiVeId = g.Key, SoLuongDaBan = g.Count() })
                .ToListAsync();

            var thongKeTheoLoai = loaiVes
                .Select(l => new ThongKeLoaiVeBtcVM
                {
                    LoaiVeId = l.LoaiVeId,
                    TenLoai = l.TenLoai ?? string.Empty,
                    GiaVe = l.GiaVe,
                    SoLuongConfig = l.SoLuong,
                    SoLuongDaBan = veTheoLoai
                        .FirstOrDefault(x => x.LoaiVeId == l.LoaiVeId)?.SoLuongDaBan ?? 0
                })
                .ToList();

            // ==== THỐNG KÊ CHUNG ====
            var veQuery = _db.Ve.Where(v => v.SuKienId == sk.SuKienId);
            var tongVeDaBan = await veQuery.CountAsync(v =>
                v.TrangThai == "DaThanhToan" || v.TrangThai == "DaCheckIn");

            var thanhToanQuery = from tt in _db.ThanhToan
                                 join v in _db.Ve on tt.VeId equals v.VeId
                                 where v.SuKienId == sk.SuKienId && tt.TrangThai == "ThanhCong"
                                 select tt.SoTien;

          
decimal tongThanhToanSuKien =
    await thanhToanQuery.SumAsync();

decimal tongHoanSuKien = await _db.DonHang
    .Where(d =>
        d.TrangThaiHoan == "DaHoan" &&
        _db.Ve.Any(v =>
            v.DonHangId == d.DonHangId &&
            v.SuKienId == sk.SuKienId))
    .SumAsync(d => d.SoTienHoan);

decimal tongDoanhThu =
    tongThanhToanSuKien - tongHoanSuKien;


            var thamGiaQuery = from tg in _db.ThamGiaSuKien
                               join v in _db.Ve on tg.VeId equals v.VeId
                               where v.SuKienId == sk.SuKienId
                               select tg;

            var tongCheckIn = await thamGiaQuery
                .Where(tg => tg.TrangThai == "DaCheckIn")
                .CountAsync();

            var ycQuery = from yc in _db.YeuCauHoanHuyVe
                          join v in _db.Ve on yc.VeId equals v.VeId
                          where v.SuKienId == sk.SuKienId && yc.HinhThuc == "HuyVe"
                          select yc;

            var tongYeuCauHoanHuy = await ycQuery.CountAsync();
            var tongYeuCauHoanHuyChoDuyet = await ycQuery
                .Where(y => y.TrangThai == "ChoDuyet")
                .CountAsync();


var soDo = await _db.SoDoSuKien
    .AsNoTracking()
    .FirstOrDefaultAsync(x => x.SuKienId == sk.SuKienId);

ViewBag.SoDoJson = "null";

if (soDo != null && diaDiem != null)
{
    var ghe = await _db.SoDoChoNgoi
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
                : x.LoaiVe.TenLoai
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
        ghe,
        thanhPhan
    });
}


            var vm = new SuKienChiTietBtcVM
            {
                SuKien = sk,
                LoaiSuKien = sk.LoaiSuKien,
                ToChuc = sk.ToChucSuKien,
                DiaDiem = diaDiem,
                SuKienTrucTuyen = online,
                ThongKeTheoLoaiVe = thongKeTheoLoai,

                TongVeDaBan = tongVeDaBan,
                TongDoanhThu = tongDoanhThu,
                TongCheckIn = tongCheckIn,
                TongYeuCauHoanHuy = tongYeuCauHoanHuy,
                TongYeuCauHoanHuyChoDuyet = tongYeuCauHoanHuyChoDuyet
            };

            return View(vm);
        }

        // ============================================================
        // ========== QUẢN LÝ LOẠI VÉ CHO TỪNG SỰ KIỆN =================
        // ============================================================

        // GET: /BanToChuc/QuanLyVe/5
        public async Task<IActionResult> QuanLyVe(int id)
        {
            var toChucId = await GetOrCreateToChucIdAsync();

            var sk = await _db.SuKien
                .FirstOrDefaultAsync(s => s.SuKienId == id && s.ToChucId == toChucId);

            if (sk == null) return NotFound();

            var loaiVes = await _db.LoaiVe
                .Where(l => l.SuKienId == id)
                .ToListAsync();

            var veTheoLoai = await _db.Ve
                .Where(v => v.SuKienId == id &&
                            (v.TrangThai == "DaThanhToan" || v.TrangThai == "DaCheckIn"))
                .GroupBy(v => v.LoaiVeId)
                .Select(g => new { LoaiVeId = g.Key, SoLuongDaBan = g.Count() })
                .ToListAsync();

            var vm = new QuanLyVeBtcVM
            {
                SuKienId = sk.SuKienId,
                TenSuKien = sk.TenSuKien,
                LoaiVes = loaiVes.Select(l => new QuanLyVeItemBtcVM
                {
                    LoaiVeId = l.LoaiVeId,
                    TenLoai = l.TenLoai ?? string.Empty,
                    GiaVe = l.GiaVe,
                    SoLuongConfig = l.SoLuong,
                    SoLuongDaBan = veTheoLoai.FirstOrDefault(x => x.LoaiVeId == l.LoaiVeId)?.SoLuongDaBan ?? 0,
                    MoTa = l.MoTa
                }).ToList()
            };

            ViewBag.LoiLoaiVe = TempData["LoiLoaiVe"]?.ToString();

            return View(vm);
        }

        // POST: Thêm loại vé mới
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ThemLoaiVe(ThemLoaiVeBtcVM model)
        {
            var toChucId = await GetOrCreateToChucIdAsync();

            var suKien = await _db.SuKien
                .FirstOrDefaultAsync(s => s.SuKienId == model.SuKienId && s.ToChucId == toChucId);

            if (suKien == null) return NotFound();

            if (string.IsNullOrWhiteSpace(model.TenLoai))
            {
                TempData["LoiLoaiVe"] = "Tên loại vé không được để trống.";
                return RedirectToAction("QuanLyVe", new { id = model.SuKienId });
            }

            if (model.GiaVe <= 0 || model.SoLuong <= 0)
            {
                TempData["LoiLoaiVe"] = "Giá vé và số lượng phải > 0.";
                return RedirectToAction("QuanLyVe", new { id = model.SuKienId });
            }

            var loaiVe = new LoaiVe
            {
                SuKienId = model.SuKienId,
                TenLoai = model.TenLoai,
                GiaVe = model.GiaVe,
                SoLuong = model.SoLuong,
                MoTa = model.MoTa
            };

            _db.LoaiVe.Add(loaiVe);
            await _db.SaveChangesAsync();

            TempData["ThongBao"] = "Đã thêm loại vé mới.";
            return RedirectToAction("QuanLyVe", new { id = model.SuKienId });
        }

        // POST: Cập nhật loại vé
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CapNhatLoaiVe(CapNhatLoaiVeBtcVM model)
        {
            var toChucId = await GetOrCreateToChucIdAsync();

            var loaiVe = await _db.LoaiVe
                .Include(l => l.SuKien)
                .FirstOrDefaultAsync(l => l.LoaiVeId == model.LoaiVeId && l.SuKienId == model.SuKienId && l.SuKien.ToChucId == toChucId);

            if (loaiVe == null) return NotFound();

            if (string.IsNullOrWhiteSpace(model.TenLoai) || model.GiaVe <= 0 || model.SoLuong <= 0)
            {
                TempData["LoiLoaiVe"] = "Dữ liệu loại vé không hợp lệ.";
                return RedirectToAction("QuanLyVe", new { id = model.SuKienId });
            }

            var soVeDaBan = await _db.Ve.CountAsync(v =>
                v.LoaiVeId == loaiVe.LoaiVeId &&
                (v.TrangThai == "DaThanhToan" || v.TrangThai == "DaCheckIn"));

            if (model.SoLuong < soVeDaBan)
            {
                TempData["LoiLoaiVe"] = $"Số lượng không được nhỏ hơn {soVeDaBan} vé đã bán.";
                return RedirectToAction("QuanLyVe", new { id = model.SuKienId });
            }

            loaiVe.TenLoai = model.TenLoai;
            loaiVe.GiaVe = model.GiaVe;
            loaiVe.SoLuong = model.SoLuong;
            loaiVe.MoTa = model.MoTa;

            await _db.SaveChangesAsync();

            TempData["ThongBao"] = "Đã cập nhật loại vé.";
            return RedirectToAction("QuanLyVe", new { id = model.SuKienId });
        }

        // POST: Xóa loại vé (nếu chưa có vé nào)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> XoaLoaiVe(int id, int suKienId)
        {
            var toChucId = await GetOrCreateToChucIdAsync();

            var loaiVe = await _db.LoaiVe
                .Include(l => l.SuKien)
                .FirstOrDefaultAsync(l => l.LoaiVeId == id && l.SuKienId == suKienId && l.SuKien.ToChucId == toChucId);

            if (loaiVe == null) return NotFound();

            bool daCoVe = await _db.Ve.AnyAsync(v => v.LoaiVeId == id);
            if (daCoVe)
            {
                TempData["LoiLoaiVe"] = "Không thể xóa loại vé đã có vé được bán.";
                return RedirectToAction("QuanLyVe", new { id = suKienId });
            }

            _db.LoaiVe.Remove(loaiVe);
            await _db.SaveChangesAsync();

            TempData["ThongBao"] = "Đã xóa loại vé.";
            return RedirectToAction("QuanLyVe", new { id = suKienId });
        }

        // ============================================================
        // ========== DANH SÁCH NGƯỜI MUA VÉ / THAM GIA ===============
        // ============================================================

public async Task<IActionResult> DanhSachVe(int id)
{
    var toChucId = await GetOrCreateToChucIdAsync();

    var sk = await _db.SuKien
        .AsNoTracking()
        .FirstOrDefaultAsync(s =>
            s.SuKienId == id &&
            s.ToChucId == toChucId);

    if (sk == null)
        return NotFound();

    var danhSach = await (
        from v in _db.Ve.AsNoTracking()
        join nd in _db.NguoiDung.AsNoTracking()
            on v.NguoiDungId equals nd.NguoiDungId
        join lv in _db.LoaiVe.AsNoTracking()
            on v.LoaiVeId equals lv.LoaiVeId
        join tg in _db.ThamGiaSuKien.AsNoTracking()
            on v.VeId equals tg.VeId into tgLeft
        from tg in tgLeft.DefaultIfEmpty()
        where v.SuKienId == id
        select new DanhSachVeThamGiaItemBtcVM
        {
            VeId = v.VeId,
            MaVe = v.MaVe,
            HoTenKhach = nd.HoTen,
            EmailKhach = nd.Email,
            TenLoaiVe = lv.TenLoai ?? string.Empty,
            TrangThaiVe = v.TrangThai,
            DaCheckIn = tg != null &&
                        tg.TrangThai == "DaCheckIn",
            ThoiGianCheckIn = tg != null
                ? tg.ThoiGianCheckIn
                : null
        }
    ).ToListAsync();

    // Lấy mã đơn hàng tương ứng với từng vé.
    // Chỉ truy vấn vé của sự kiện thuộc BTC hiện tại.
    var maDonHangTheoVe = await _db.Ve
        .AsNoTracking()
        .Where(v => v.SuKienId == id)
        .Select(v => new
        {
            v.VeId,
            v.DonHangId
        })
        .ToDictionaryAsync(
            x => x.VeId,
            x => x.DonHangId
        );

    ViewBag.MaDonHangTheoVe = maDonHangTheoVe;

    var vm = new DanhSachVeThamGiaBtcVM
    {
        SuKienId = sk.SuKienId,
        TenSuKien = sk.TenSuKien,
        DanhSach = danhSach
    };

    return View(vm);
}

// ============================================================
// ========== XỬ LÝ YÊU CẦU HỦY VÉ =====================
// ============================================================


[HttpGet]
public async Task<IActionResult> YeuCauHuyVe(string? trangThai)
{
    var toChucId = await GetOrCreateToChucIdAsync();

    var duLieu = await (
        from yc in _db.YeuCauHoanHuyVe
        join ve in _db.Ve on yc.VeId equals ve.VeId
        join sk in _db.SuKien on ve.SuKienId equals sk.SuKienId
        join nd in _db.NguoiDung on ve.NguoiDungId equals nd.NguoiDungId
        where sk.ToChucId == toChucId
              && yc.HinhThuc == "HuyVe"
        select new
        {
            yc.YeuCauId,
            yc.LyDo,
            yc.NgayYeuCau,
            yc.TrangThai,
            ve.VeId,
            ve.DonHangId,
            ve.MaVe,
            sk.TenSuKien,
            nd.HoTen
        }
    ).AsNoTracking().ToListAsync();

    // Gộp các yêu cầu theo đơn hàng.
    // Vé không có DonHangId được xử lý riêng.
    var danhSach = duLieu
        .GroupBy(x => x.DonHangId.HasValue
            ? $"DH_{x.DonHangId.Value}"
            : $"VE_{x.VeId}")
        .Select(g =>
        {
            // Ưu tiên yêu cầu đang chờ duyệt.
            // Nếu không có, hiển thị yêu cầu gần nhất.
            var daiDien = g
                .OrderByDescending(x =>
                    x.TrangThai == "ChoDuyet")
                .ThenByDescending(x => x.NgayYeuCau)
                .ThenByDescending(x => x.YeuCauId)
                .First();

            return new
            {
                DaiDien = daiDien,
                DonHangId = daiDien.DonHangId,
                VeId = daiDien.VeId
            };
        })
        .ToList();

    // Lọc sau khi gộp để mỗi đơn chỉ có một dòng.
    if (!string.IsNullOrWhiteSpace(trangThai))
    {
        danhSach = danhSach
            .Where(x => x.DaiDien.TrangThai == trangThai)
            .ToList();
    }

    var list = new List<YeuCauHoanHuyItemBtcVM>();

    foreach (var item in danhSach
        .OrderByDescending(x => x.DaiDien.NgayYeuCau))
    {
        int soLuongVe;

        if (item.DonHangId.HasValue)
        {
            soLuongVe = await _db.Ve
                .CountAsync(v =>
                    v.DonHangId == item.DonHangId.Value);
        }
        else
        {
            soLuongVe = 1;
        }

        // Lấy tất cả vé và ghế thuộc đơn hàng.
        var chiTietGhe = await _db.Ve
            .AsNoTracking()
            .Where(v => item.DonHangId.HasValue
                ? v.DonHangId == item.DonHangId.Value
                : v.VeId == item.VeId)
            .OrderBy(v => v.VeId)
            .Select(v => new
            {
                Hang = v.ChoNgoi != null
                    ? v.ChoNgoi.Hang
                    : null,

                Cot = v.ChoNgoi != null
                    ? (int?)v.ChoNgoi.Cot
                    : null,

                TenLoaiVe = v.LoaiVe.TenLoai
            })
            .ToListAsync();

        var danhSachGhe = chiTietGhe
            .Select(g =>
                !string.IsNullOrWhiteSpace(g.Hang) &&
                g.Cot.HasValue
                    ? $"{g.Hang}{g.Cot.Value} - {g.TenLoaiVe}"
                    : $"Không chọn ghế - {g.TenLoaiVe}")
            .ToList();

        list.Add(new YeuCauHoanHuyItemBtcVM
        {
            
            YeuCauId = item.DaiDien.YeuCauId,

            MaVe = item.DonHangId.HasValue
                ? $"Đơn #{item.DonHangId.Value} ({soLuongVe} vé)"
                : item.DaiDien.MaVe,

            TenSuKien = item.DaiDien.TenSuKien,
            HoTenKhach = item.DaiDien.HoTen,
            TrangThai = item.DaiDien.TrangThai,
            HinhThuc = "HuyVe",
            LyDo = item.DaiDien.LyDo,
            NgayYeuCau = item.DaiDien.NgayYeuCau,
            DanhSachGhe = danhSachGhe
            
        });
    }

    ViewBag.TrangThai = trangThai;
    return View(list);
}


[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> ChapNhanYeuCauHuyVe(int id)
{
    var toChucId = await GetOrCreateToChucIdAsync();

    await using var transaction =
        await _db.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable);

    try
    {
        // Kiểm tra yêu cầu thuộc sự kiện của BTC.
        var data = await (
            from yc in _db.YeuCauHoanHuyVe
            join ve in _db.Ve on yc.VeId equals ve.VeId
            join sk in _db.SuKien on ve.SuKienId equals sk.SuKienId
            where yc.YeuCauId == id
                  && yc.HinhThuc == "HuyVe"
                  && sk.ToChucId == toChucId
            select new
            {
                YeuCau = yc,
                Ve = ve,
                SuKien = sk
            }
        ).FirstOrDefaultAsync();

        if (data == null)
            return Forbid();

        if (data.YeuCau.TrangThai != "ChoDuyet")
        {
            TempData["ThongBao"] =
                "Yêu cầu này đã được xử lý trước đó.";

            return RedirectToAction("YeuCauHuyVe");
        }

        // Lấy toàn bộ vé thuộc cùng đơn.
        IQueryable<Ve> queryVe = _db.Ve;


if (data.Ve.DonHangId.HasValue)
{
    int maDonLayVe = data.Ve.DonHangId.Value;

    queryVe = queryVe.Where(v =>
        v.DonHangId == maDonLayVe);
}

        else
        {
            queryVe = queryVe.Where(v =>
                v.VeId == data.Ve.VeId);
        }

        var danhSachVe = await queryVe
            .OrderBy(v => v.VeId)
            .ToListAsync();

        if (!danhSachVe.Any())
        {
            TempData["ThongBao"] =
                "Không tìm thấy vé trong đơn.";

            return RedirectToAction("YeuCauHuyVe");
        }

        // Không được xử lý lẫn vé của khách/sự kiện khác.
        if (danhSachVe.Any(v =>
            v.NguoiDungId != data.Ve.NguoiDungId ||
            v.SuKienId != data.SuKien.SuKienId))
        {
            TempData["ThongBao"] =
                "Dữ liệu đơn hàng không hợp lệ.";

            return RedirectToAction("YeuCauHuyVe");
        }

        var now = DateTime.Now;

        // Chỉ hủy đơn chưa sử dụng, trước giờ bắt đầu.
        if (data.SuKien.ThoiGianBatDau <= now ||
            danhSachVe.Any(v =>
                v.TrangThai != "DaThanhToan"))
        {
            TempData["ThongBao"] =
                "Đơn hàng không còn đủ điều kiện hủy.";

            return RedirectToAction("YeuCauHuyVe");
        }

        var veIds = danhSachVe
            .Select(v => v.VeId)
            .ToList();

        // Không hủy đơn có bất kỳ vé nào đã check-in.
        bool daCheckIn = await _db.ThamGiaSuKien
            .AnyAsync(tg =>
                veIds.Contains(tg.VeId) &&
                tg.TrangThai == "DaCheckIn");

        if (daCheckIn)
        {
            TempData["ThongBao"] =
                "Đơn có vé đã check-in nên không thể hủy.";

            return RedirectToAction("YeuCauHuyVe");
        }

        // Lấy cả các yêu cầu cũ thuộc cùng đơn.
        var danhSachYeuCau = await _db.YeuCauHoanHuyVe
            .Where(y =>
                veIds.Contains(y.VeId) &&
                y.HinhThuc == "HuyVe")
            .ToListAsync();

        // Tránh xử lý khi đơn từng được chấp thuận.
        if (danhSachYeuCau.Any(y =>
            y.TrangThai == "DaHuy"))
        {
            TempData["ThongBao"] =
                "Đơn đã có yêu cầu hủy được chấp thuận.";

            return RedirectToAction("YeuCauHuyVe");
        }

        var yeuCauChoDuyet = danhSachYeuCau
            .Where(y => y.TrangThai == "ChoDuyet")
            .ToList();

        if (!yeuCauChoDuyet.Any())
        {
            TempData["ThongBao"] =
                "Không còn yêu cầu chờ duyệt.";

            return RedirectToAction("YeuCauHuyVe");
        }

        // Chấp thuận mọi yêu cầu đang chờ của đơn.



        // Cập nhật trạng thái đơn hàng sau khi hủy vé.

        // ==============================================
        // HOÀN TIỀN MÔ PHỎNG CHO TOÀN BỘ ĐƠN HÀNG
        // ==============================================

        if (!data.Ve.DonHangId.HasValue)
        {
            TempData["ThongBao"] =
                "Vé không có mã đơn hàng, không thể hoàn tiền tự động.";

            return RedirectToAction("YeuCauHuyVe");
        }

        int donHangId = data.Ve.DonHangId.Value;

        var donHang = await _db.DonHang
            .FirstOrDefaultAsync(d =>
                d.DonHangId == donHangId &&
                d.NguoiDungId == data.Ve.NguoiDungId);

        if (donHang == null)
        {
            TempData["ThongBao"] =
                "Không tìm thấy đơn hàng cần hoàn tiền.";

            return RedirectToAction("YeuCauHuyVe");
        }

        // Không ghi nhận hoàn tiền hai lần.
        if (donHang.TrangThaiHoan == "DaHoan" ||
            donHang.SoTienHoan > 0)
        {
            TempData["ThongBao"] =
                "Đơn hàng đã được hoàn tiền trước đó.";

            return RedirectToAction("YeuCauHuyVe");
        }

        // Kiểm tra giao dịch thanh toán thành công.
        decimal soTienDaThanhToan = await (
            from tt in _db.ThanhToan
            join v in _db.Ve on tt.VeId equals v.VeId
            where v.DonHangId == donHangId &&
                  tt.TrangThai == "ThanhCong"
            select tt.SoTien
        ).SumAsync();

        if (soTienDaThanhToan <= 0 ||
            soTienDaThanhToan > donHang.TongTien)
        {
            TempData["ThongBao"] =
                "Không xác định được số tiền thanh toán hợp lệ.";

            return RedirectToAction("YeuCauHuyVe");
        }

        // Hoàn 100% số tiền đã thanh toán (mô phỏng).
        donHang.SoTienHoan = soTienDaThanhToan;
        donHang.NgayHoan = now;
        donHang.TrangThaiHoan = "DaHoan";
        donHang.TrangThai = "Huy";
    

        // Chỉ xử lý hủy vé sau khi đã kiểm tra
        // đơn hàng và số tiền hoàn hợp lệ.

        foreach (var yc in yeuCauChoDuyet)
        {
            yc.TrangThai = "DaHuy";
        }

        foreach (var ve in danhSachVe)
        {
            ve.TrangThai = "Huy";
        }


        // Giải phóng toàn bộ ghế của đơn.
        var choNgoiIds = danhSachVe
            .Where(v => v.ChoNgoiId.HasValue)
            .Select(v => v.ChoNgoiId!.Value)
            .Distinct()
            .ToList();

        if (choNgoiIds.Any())
        {
            var danhSachGhe = await _db.SoDoChoNgoi
                .Where(g =>
                    choNgoiIds.Contains(g.ChoNgoiId))
                .ToListAsync();

            foreach (var ghe in danhSachGhe)
            {
                ghe.TrangThai = "Trong";
                ghe.DonHangGiuId = null;
                ghe.ThoiGianHetHanGiu = null;
            }
        }

        string maDon = data.Ve.DonHangId.HasValue
            ? $"#{data.Ve.DonHangId.Value}"
            : data.Ve.MaVe;

        _db.ThongBao.Add(new ThongBao
        {
            NguoiDungId = data.Ve.NguoiDungId,
            NoiDung =
                $"Yêu cầu hủy đơn {maDon} " +
                $"({danhSachVe.Count} vé) của sự kiện " +
                $"\"{data.SuKien.TenSuKien}\" đã được chấp thuận.",
            NgayGui = now,
            DaDoc = false
        });

        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

TempData["ThongBao"] =
    $"Đã hủy {danhSachVe.Count} vé và ghi nhận hoàn tiền mô phỏng " +
    $"{donHang.SoTienHoan:N0}đ cho đơn #{donHang.DonHangId}.";

        return RedirectToAction("YeuCauHuyVe");
    }
    catch (Exception)
    {
        await transaction.RollbackAsync();

        TempData["ThongBao"] =
            "Có lỗi khi xử lý hủy đơn. Vui lòng thử lại.";

        return RedirectToAction("YeuCauHuyVe");
    }
}


[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> TuChoiYeuCauHuyVe(int id)
{
    var toChucId = await GetOrCreateToChucIdAsync();

    await using var transaction =
        await _db.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable);

    try
    {
        var data = await (
            from yc in _db.YeuCauHoanHuyVe
            join ve in _db.Ve on yc.VeId equals ve.VeId
            join sk in _db.SuKien on ve.SuKienId equals sk.SuKienId
            where yc.YeuCauId == id
                  && yc.HinhThuc == "HuyVe"
                  && sk.ToChucId == toChucId
            select new
            {
                YeuCau = yc,
                Ve = ve,
                SuKien = sk
            }
        ).FirstOrDefaultAsync();

        if (data == null)
            return Forbid();

        if (data.YeuCau.TrangThai != "ChoDuyet")
        {
            TempData["ThongBao"] =
                "Yêu cầu đã được xử lý trước đó.";

            return RedirectToAction("YeuCauHuyVe");
        }

        IQueryable<Ve> queryVe = _db.Ve;

        if (data.Ve.DonHangId.HasValue)
        {
            int donHangId = data.Ve.DonHangId.Value;

            queryVe = queryVe.Where(v =>
                v.DonHangId == donHangId);
        }
        else
        {
            queryVe = queryVe.Where(v =>
                v.VeId == data.Ve.VeId);
        }

        var danhSachVe = await queryVe.ToListAsync();

        if (!danhSachVe.Any() ||
            danhSachVe.Any(v =>
                v.NguoiDungId != data.Ve.NguoiDungId ||
                v.SuKienId != data.SuKien.SuKienId))
        {
            TempData["ThongBao"] =
                "Dữ liệu đơn hàng không hợp lệ.";

            return RedirectToAction("YeuCauHuyVe");
        }

        var veIds = danhSachVe
            .Select(v => v.VeId)
            .ToList();

        var danhSachYeuCau = await _db.YeuCauHoanHuyVe
            .Where(y =>
                veIds.Contains(y.VeId) &&
                y.HinhThuc == "HuyVe")
            .ToListAsync();

        // Không từ chối nếu đơn đã được chấp thuận hủy.
        if (danhSachYeuCau.Any(y =>
            y.TrangThai == "DaHuy"))
        {
            TempData["ThongBao"] =
                "Đơn đã được chấp thuận hủy trước đó.";

            return RedirectToAction("YeuCauHuyVe");
        }

        var yeuCauChoDuyet = danhSachYeuCau
            .Where(y => y.TrangThai == "ChoDuyet")
            .ToList();

        if (!yeuCauChoDuyet.Any())
        {
            TempData["ThongBao"] =
                "Không còn yêu cầu chờ duyệt.";

            return RedirectToAction("YeuCauHuyVe");
        }

        foreach (var yc in yeuCauChoDuyet)
        {
            yc.TrangThai = "TuChoi";
        }

        // Không thay đổi trạng thái vé và ghế.
        // Vé đã thanh toán sẽ được sử dụng lại
        // nếu vẫn còn trong thời gian hợp lệ.

        string maDon = data.Ve.DonHangId.HasValue
            ? $"#{data.Ve.DonHangId.Value}"
            : data.Ve.MaVe;

        _db.ThongBao.Add(new ThongBao
        {
            NguoiDungId = data.Ve.NguoiDungId,
            NoiDung =
                $"Yêu cầu hủy đơn {maDon} " +
                $"({danhSachVe.Count} vé) của sự kiện " +
                $"\"{data.SuKien.TenSuKien}\" đã bị từ chối.",
            NgayGui = DateTime.Now,
            DaDoc = false
        });

        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        TempData["ThongBao"] =
            $"Đã từ chối yêu cầu hủy đơn gồm {danhSachVe.Count} vé.";

        return RedirectToAction("YeuCauHuyVe");
    }
    catch (Exception)
    {
        await transaction.RollbackAsync();

        TempData["ThongBao"] =
            "Có lỗi khi từ chối yêu cầu. Vui lòng thử lại.";

        return RedirectToAction("YeuCauHuyVe");
    }
}


        // ============================================================
        // ========== GỬI THÔNG BÁO CHO NGƯỜI ĐẶT VÉ ==================
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> GuiThongBao(int id)
        {
            var toChucId = await GetOrCreateToChucIdAsync();

            var sk = await _db.SuKien
                .FirstOrDefaultAsync(s => s.SuKienId == id && s.ToChucId == toChucId);

            if (sk == null) return NotFound();

            var vm = new GuiThongBaoBtcVM
            {
                SuKienId = sk.SuKienId
            };

            ViewBag.TenSuKien = sk.TenSuKien;
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GuiThongBao(GuiThongBaoBtcVM model)
        {
            if (string.IsNullOrWhiteSpace(model.NoiDung))
            {
                ModelState.AddModelError("NoiDung", "Nội dung thông báo không được để trống.");
            }

            var toChucId = await GetOrCreateToChucIdAsync();

            var sk = await _db.SuKien
                .FirstOrDefaultAsync(s => s.SuKienId == model.SuKienId && s.ToChucId == toChucId);

            if (sk == null) return NotFound();

            if (!ModelState.IsValid)
            {
                ViewBag.TenSuKien = sk.TenSuKien;
                return View(model);
            }

            var nguoiDungIds = await _db.Ve
                .Where(v => v.SuKienId == sk.SuKienId)
                .Select(v => v.NguoiDungId)
                .Distinct()
                .ToListAsync();

            foreach (var ndId in nguoiDungIds)
            {
                _db.ThongBao.Add(new ThongBao
                {
                    NguoiDungId = ndId,
                    NoiDung = $"[{sk.TenSuKien}] {model.NoiDung}"
                });
            }

            await _db.SaveChangesAsync();

            TempData["ThongBao"] = "Đã gửi thông báo tới những người đã đặt vé.";
            return RedirectToAction("ChiTiet", new { id = sk.SuKienId });
        }

 
[HttpGet]
public async Task<IActionResult> DoanhThuTheoSuKien()
{
    int toChucId = await GetOrCreateToChucIdAsync();

    var suKiens = await _db.SuKien
        .AsNoTracking()
        .Where(s => s.ToChucId == toChucId)
        .OrderByDescending(s => s.ThoiGianBatDau)
        .Select(s => new
        {
            s.SuKienId,
            s.TenSuKien,
            s.ThoiGianBatDau,
            s.ThoiGianKetThuc
        })
        .ToListAsync();

    var ids = suKiens.Select(s => s.SuKienId).ToList();

    if (ids.Count == 0)
    {
        return View(new List<DoanhThuSuKienItemVM>());
    }

    var ves = await _db.Ve
        .AsNoTracking()
        .Where(v => ids.Contains(v.SuKienId))
        .Select(v => new
        {
            v.VeId,
            v.SuKienId,
            v.LoaiVeId,
            v.DonHangId,
            v.TrangThai
        })
        .ToListAsync();

    var loaiVes = await _db.LoaiVe
        .AsNoTracking()
        .Where(l => ids.Contains(l.SuKienId))
        .Select(l => new
        {
            l.LoaiVeId,
            l.SuKienId,
            l.TenLoai,
            l.SoLuong
        })
        .ToListAsync();

    var thanhToans = await (
        from t in _db.ThanhToan.AsNoTracking()
        join v in _db.Ve.AsNoTracking()
            on t.VeId equals v.VeId
        where ids.Contains(v.SuKienId)
              && t.TrangThai == "ThanhCong"
        select new
        {
            t.VeId,
            v.SuKienId,
            v.LoaiVeId,
            t.SoTien,
            t.NgayThanhToan
        }
    ).ToListAsync();

    // Lấy khoản hoàn theo đơn, không nhân số tiền
    // hoàn lên khi một đơn có nhiều vé.
    var hoanRaw = await (
        from d in _db.DonHang.AsNoTracking()
        join v in _db.Ve.AsNoTracking()
            on d.DonHangId equals v.DonHangId
        where ids.Contains(v.SuKienId)
              && d.TrangThaiHoan == "DaHoan"
        select new
        {
            d.DonHangId,
            v.SuKienId,
            d.SoTienHoan,
            d.NgayHoan
        }
    ).Distinct().ToListAsync();

    var hoanTiens = hoanRaw
        .GroupBy(h => new { h.DonHangId, h.SuKienId })
        .Select(g => g.First())
        .ToList();

    var thangHienTai = new DateTime(
        DateTime.Today.Year,
        DateTime.Today.Month, 1);

    var thangDau = thangHienTai.AddMonths(-11);

    var ketQua = new List<DoanhThuSuKienItemVM>();

    foreach (var sk in suKiens)
    {
        var veSk = ves
            .Where(v => v.SuKienId == sk.SuKienId)
            .ToList();

        var ttSk = thanhToans
            .Where(t => t.SuKienId == sk.SuKienId)
            .ToList();

        var hoanSk = hoanTiens
            .Where(h => h.SuKienId == sk.SuKienId)
            .ToList();

        var loaiSk = loaiVes
            .Where(l => l.SuKienId == sk.SuKienId)
            .ToList();

        var item = new DoanhThuSuKienItemVM
        {
            SuKienId = sk.SuKienId,
            TenSuKien = sk.TenSuKien,
            ThoiGianBatDau = sk.ThoiGianBatDau,
            ThoiGianKetThuc = sk.ThoiGianKetThuc,

            SoVeDaBan = veSk.Count(v =>
                v.TrangThai == "DaThanhToan" ||
                v.TrangThai == "DaCheckIn"),

            SoVeDaCheckIn = veSk.Count(v =>
                v.TrangThai == "DaCheckIn"),

            SoVeDaHuy = veSk.Count(v =>
                v.TrangThai == "Huy"),

            TongSoVePhatHanh = loaiSk.Sum(l => l.SoLuong),

            TongThanhToan = ttSk.Sum(t => t.SoTien),
            TongHoan = hoanSk.Sum(h => h.SoTienHoan)
        };

        for (int i = 0; i < 12; i++)
        {
            var dau = thangDau.AddMonths(i);
            var cuoi = dau.AddMonths(1);

            item.TheoThang.Add(new DoanhThuThangSuKienVM
            {
                Thang = dau.ToString("MM/yyyy"),

                ThanhToan = ttSk
                    .Where(t =>
                        t.NgayThanhToan >= dau &&
                        t.NgayThanhToan < cuoi)
                    .Sum(t => t.SoTien),

                HoanTien = hoanSk
                    .Where(h =>
                        h.NgayHoan.HasValue &&
                        h.NgayHoan.Value >= dau &&
                        h.NgayHoan.Value < cuoi)
                    .Sum(h => h.SoTienHoan)
            });
        }

        // Doanh thu gộp theo đúng loại vé của giao dịch.
        // Không lấy giá niêm yết nhân số lượng.
        item.TheoLoaiVe = ttSk
            .GroupBy(t => t.LoaiVeId)
            .Select(g =>
            {
                var loai = loaiSk.FirstOrDefault(
                    l => l.LoaiVeId == g.Key);

                return new DoanhThuLoaiVeVM
                {
                    TenLoaiVe = loai?.TenLoai ??
                        "Loại vé không xác định",

                    SoVeThanhToan = g
                        .Select(t => t.VeId)
                        .Distinct()
                        .Count(),

                    DoanhThuGop = g.Sum(t => t.SoTien)
                };
            })
            .GroupBy(x => x.TenLoaiVe)
            .Select(g => new DoanhThuLoaiVeVM
            {
                TenLoaiVe = g.Key,
                SoVeThanhToan = g.Sum(x => x.SoVeThanhToan),
                DoanhThuGop = g.Sum(x => x.DoanhThuGop)
            })
            .OrderByDescending(x => x.DoanhThuGop)
            .ToList();

        ketQua.Add(item);
    }

    ketQua = ketQua
        .OrderByDescending(x => x.DoanhThuThucNhan)
        .ThenByDescending(x => x.SoVeDaBan)
        .ToList();

    return View(ketQua);
}

// ============================================================
// ========== THỐNG KÊ TỔNG QUAN (DOANH THU THEO SỰ KIỆN) ======
// ============================================================
// Action này để bạn gắn vào menu: asp-action="ThongKeTongQuan"
// - Hiển thị doanh thu theo từng sự kiện của tổ chức
// - Có thể lọc theo khoảng ngày thanh toán (tuNgay/denNgay)

[HttpGet]
public async Task<IActionResult> ThongKeTongQuan(int? suKienId)
{
    int toChucId = await GetOrCreateToChucIdAsync();
    DateTime now = DateTime.Now;

    // 1. Chỉ lấy sự kiện thuộc BTC hiện tại.
    var tatCaSuKien = await _db.SuKien
        .AsNoTracking()
        .Where(s => s.ToChucId == toChucId)
        .Select(s => new
        {
            s.SuKienId,
            s.TenSuKien,
            s.TrangThaiDuyet,
            s.ThoiGianBatDau,
            s.ThoiGianKetThuc
        })
        .ToListAsync();

    if (suKienId.HasValue &&
        !tatCaSuKien.Any(s =>
            s.SuKienId == suKienId.Value))
    {
        return NotFound();
    }

    var suKiens = tatCaSuKien
        .Where(s =>
            !suKienId.HasValue ||
            s.SuKienId == suKienId.Value)
        .ToList();

    var suKienIds = suKiens
        .Select(s => s.SuKienId)
        .ToList();

    // 2. Tất cả vé theo đúng danh sách sự kiện.
    var ves = await _db.Ve
        .AsNoTracking()
        .Where(v => suKienIds.Contains(v.SuKienId))
        .Select(v => new
        {
            v.VeId,
            v.SuKienId,
            v.DonHangId,
            v.TrangThai
        })
        .ToListAsync();

    // 3. Thanh toán thành công theo thời điểm giao dịch.
    // Mỗi vé chỉ tính một lần, kể cả khi có nhiều
    // bản ghi thanh toán thành công cùng VeId.
    var thanhToans = await (
        from tt in _db.ThanhToan.AsNoTracking()
        join ve in _db.Ve.AsNoTracking()
            on tt.VeId equals ve.VeId
        where suKienIds.Contains(ve.SuKienId) &&
              tt.TrangThai == "ThanhCong"
        select new
        {
            tt.VeId,
            tt.NgayThanhToan
        }
    ).ToListAsync();

    var veThanhToanTheoNgay = thanhToans
        .GroupBy(t => t.VeId)
        .Select(g => new
        {
            VeId = g.Key,
            NgayThanhToan = g.Min(t => t.NgayThanhToan)
        })
        .ToList();

    // 4. Lịch sử check-in: đếm theo thời điểm vào cổng.
    var checkIns = await (
        from tg in _db.ThamGiaSuKien.AsNoTracking()
        join ve in _db.Ve.AsNoTracking()
            on tg.VeId equals ve.VeId
        where suKienIds.Contains(ve.SuKienId) &&
              tg.TrangThai == "DaCheckIn" &&
              tg.ThoiGianCheckIn != null
        select new
        {
            tg.VeId,
            tg.ThoiGianCheckIn
        }
    ).ToListAsync();

    var checkInTheoNgay = checkIns
        .GroupBy(t => t.VeId)
        .Select(g => new
        {
            VeId = g.Key,
            NgayCheckIn = g.Min(t => t.ThoiGianCheckIn!.Value)
        })
        .ToList();

    // 5. Yêu cầu hủy của BTC.
    var yeuCaus = await (
        from yc in _db.YeuCauHoanHuyVe.AsNoTracking()
        join v in _db.Ve.AsNoTracking()
            on yc.VeId equals v.VeId
        join sk in _db.SuKien.AsNoTracking()
            on v.SuKienId equals sk.SuKienId
        where suKienIds.Contains(v.SuKienId) &&
              yc.HinhThuc == "HuyVe"
        select new
        {
            yc.YeuCauId,
            yc.NgayYeuCau,
            yc.TrangThai,
            yc.LyDo,
            v.VeId,
            v.DonHangId,
            v.MaVe,
            sk.TenSuKien
        }
    ).ToListAsync();

    // Đếm đơn đang chờ duyệt, không nhân theo số vé.
    int yeuCauChoDuyet = yeuCaus
        .Where(y => y.TrangThai == "ChoDuyet")
        .Select(y => y.DonHangId.HasValue
            ? "DH_" + y.DonHangId.Value
            : "VE_" + y.VeId)
        .Distinct()
        .Count();

    // 6. Xu hướng 12 tháng gần nhất.
    var thangHienTai = new DateTime(
        now.Year, now.Month, 1);

    var thangDau = thangHienTai.AddMonths(-11);

    var hoatDong = new List<HoatDongTheoThangVM>();

    for (int i = 0; i < 12; i++)
    {
        var dauThang = thangDau.AddMonths(i);
        var cuoiThang = dauThang.AddMonths(1);

        hoatDong.Add(new HoatDongTheoThangVM
        {
            Thang = dauThang.ToString("MM/yyyy"),

            VeThanhToan = veThanhToanTheoNgay.Count(t =>
                t.NgayThanhToan >= dauThang &&
                t.NgayThanhToan < cuoiThang),

            LuotCheckIn = checkInTheoNgay.Count(t =>
                t.NgayCheckIn >= dauThang &&
                t.NgayCheckIn < cuoiThang)
        });
    }

    // 7. Phân loại sự kiện theo trạng thái nghiệp vụ.
    int choDuyet = suKiens.Count(s =>
        s.TrangThaiDuyet == "ChoDuyet");

    int tuChoi = suKiens.Count(s =>
        s.TrangThaiDuyet == "TuChoi");

    int daAn = suKiens.Count(s =>
        s.TrangThaiDuyet == "An");

    var daDuyet = suKiens
        .Where(s => s.TrangThaiDuyet == "DaDuyet")
        .ToList();

    int sapDienRa = daDuyet.Count(s =>
        s.ThoiGianBatDau > now);

    int dangDienRa = daDuyet.Count(s =>
        s.ThoiGianBatDau <= now &&
        s.ThoiGianKetThuc > now);

    int daKetThuc = daDuyet.Count(s =>
        s.ThoiGianKetThuc <= now);

    var trangThaiSuKien =
        new List<TrangThaiSuKienTongQuanVM>
        {
            new()
            {
                TrangThai = "Sắp diễn ra",
                SoLuong = sapDienRa
            },
            new()
            {
                TrangThai = "Đang diễn ra",
                SoLuong = dangDienRa
            },
            new()
            {
                TrangThai = "Đã kết thúc",
                SoLuong = daKetThuc
            },
            new()
            {
                TrangThai = "Chờ duyệt",
                SoLuong = choDuyet
            },
            new()
            {
                TrangThai = "Từ chối",
                SoLuong = tuChoi
            },
            new()
            {
                TrangThai = "Đã ẩn",
                SoLuong = daAn
            }
        };

    // 8. Những sự kiện sắp diễn ra trong 30 ngày.
    var suKienSapToi = suKiens
        .Where(s =>
            s.TrangThaiDuyet == "DaDuyet" &&
            s.ThoiGianBatDau > now &&
            s.ThoiGianBatDau < now.AddDays(30))
        .OrderBy(s => s.ThoiGianBatDau)
        .Take(5)
        .Select(s => new SuKienSapDienRaVM
        {
            SuKienId = s.SuKienId,
            TenSuKien = s.TenSuKien,
            ThoiGianBatDau = s.ThoiGianBatDau,
            ThoiGianKetThuc = s.ThoiGianKetThuc,

            SoVeHieuLuc = ves.Count(v =>
                v.SuKienId == s.SuKienId &&
                (v.TrangThai == "DaThanhToan" ||
                 v.TrangThai == "DaCheckIn"))
        })
        .ToList();

    // 9. Lấy các yêu cầu gần đây, mỗi đơn một dòng.
    var yeuCauGanDay = yeuCaus
        .GroupBy(y => y.DonHangId.HasValue
            ? "DH_" + y.DonHangId.Value
            : "VE_" + y.VeId)
        .Select(g => g
            .OrderByDescending(x =>
                x.TrangThai == "ChoDuyet")
            .ThenByDescending(x => x.NgayYeuCau)
            .First())
        .OrderByDescending(x => x.NgayYeuCau)
        .Take(5)
        .Select(y => new YeuCauHoanHuyGanDayVM
        {
            MaVe = y.DonHangId.HasValue
                ? "Đơn #" + y.DonHangId.Value
                : y.MaVe,
            TenSuKien = y.TenSuKien,
            LyDo = y.LyDo ?? "",
            TrangThai = y.TrangThai,
            NgayYeuCau = y.NgayYeuCau
        })
        .ToList();

    var vm = new ThongKeTongQuanDashboardVM
    {
        SuKienDangChon = suKienId,

        DanhSachSuKien = tatCaSuKien
            .OrderBy(s => s.TenSuKien)
            .Select(s => new SuKienLocThongKeVM
            {
                SuKienId = s.SuKienId,
                TenSuKien = s.TenSuKien
            })
            .ToList(),

        TongSuKien = suKiens.Count,

        TongVeDaBan = ves.Count(v =>
            v.TrangThai == "DaThanhToan" ||
            v.TrangThai == "DaCheckIn"),

        TongVeDaHuy = ves.Count(v =>
            v.TrangThai == "Huy"),

        TongCheckIn = ves.Count(v =>
            v.TrangThai == "DaCheckIn"),

        YeuCauChoDuyet = yeuCauChoDuyet,

        HoatDongTheoThang = hoatDong,
        TrangThaiSuKien = trangThaiSuKien,

        TrangThaiVe = ves
            .GroupBy(v => v.TrangThai)
            .Select(g => new TrangThaiVeVM
            {
                TrangThai = g.Key,
                SoLuong = g.Count()
            })
            .OrderByDescending(x => x.SoLuong)
            .ToList(),

        TrangThaiHoanHuy = yeuCaus
            .GroupBy(y => y.TrangThai)
            .Select(g => new TrangThaiHoanHuyVM
            {
                TrangThai = g.Key,
                SoLuong = g.Count()
            })
            .ToList(),

        SuKienSapDienRa = suKienSapToi,
        YeuCauGanDay = yeuCauGanDay
    };

    return View(vm);
}


// ============================================================
// ========== DANH SÁCH VÉ - TỔNG QUAN THEO SỰ KIỆN ============
// ============================================================

[HttpGet]
public async Task<IActionResult> DanhSachVeTongQuan()
{
    var toChucId = await GetOrCreateToChucIdAsync();

    var list = await _db.SuKien
        .Where(s => s.ToChucId == toChucId)
        .OrderByDescending(s => s.ThoiGianBatDau)
        .Select(s => new DanhSachVeSuKienItemVM
        {
            SuKienId = s.SuKienId,
            TenSuKien = s.TenSuKien,
            ThoiGianBatDau = s.ThoiGianBatDau,
            ThoiGianKetThuc = s.ThoiGianKetThuc,
            TongVe = _db.Ve.Count(v => v.SuKienId == s.SuKienId),
            VeDaThanhToan = _db.Ve.Count(v => v.SuKienId == s.SuKienId && (v.TrangThai == "DaThanhToan" || v.TrangThai == "DaCheckIn")),
            VeDaHuy = _db.Ve.Count(v => v.SuKienId == s.SuKienId && v.TrangThai == "Huy")
        })
        .ToListAsync();

    return View(list); // Views/BanToChuc/DanhSachVeTongQuan.cshtml
}

        // ==============================================
        // QUÉT QR CHECK-IN BẰNG WEBCAM
        // ==============================================


[HttpGet]
public async Task<IActionResult> QuetMaQr()
{
    int toChucId = await GetOrCreateToChucIdAsync();

    var now = DateTime.Now;
    var dauNgay = now.Date;
    var cuoiNgay = dauNgay.AddDays(1);

    var suKienHomNay = await _db.SuKien
        .AsNoTracking()
        .Where(s =>
            s.ToChucId == toChucId &&
            s.TrangThaiDuyet == "DaDuyet" &&
            s.ThoiGianBatDau < cuoiNgay &&
            s.ThoiGianKetThuc > now)
        .OrderBy(s => s.ThoiGianBatDau)
        .Select(s => new
        {
            s.TenSuKien,
            s.ThoiGianBatDau,
            s.ThoiGianKetThuc
        })
        .ToListAsync();

    ViewBag.SuKienHomNay = suKienHomNay
        .Select(s =>
            $"{s.TenSuKien} | " +
            $"{s.ThoiGianBatDau:dd/MM/yyyy HH:mm} - " +
            $"{s.ThoiGianKetThuc:dd/MM/yyyy HH:mm}")
        .ToList();

    return View();
}


        // ==========================================================
        // CHECK-IN TOÀN BỘ ĐƠN BẰNG MỘT MÃ QR
        // ==========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
public async Task<IActionResult> XacNhanCheckInDonHang(
    string? maQr)
        {
            if (string.IsNullOrWhiteSpace(maQr) ||
                maQr.Length > 256)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Mã QR không hợp lệ."
                });
            }

            var parts = maQr.Trim().Split('|');

            if (parts.Length != 3 ||
                parts[0] != "CHECKIN" ||
                !int.TryParse(parts[1], out int veId) ||
                veId <= 0 ||
                string.IsNullOrWhiteSpace(parts[2]))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Mã QR không đúng định dạng của hệ thống."
                });
            }

            var toChucId = await GetOrCreateToChucIdAsync();

            await using var transaction =
                await _db.Database.BeginTransactionAsync(
                    System.Data.IsolationLevel.Serializable);

            try
            {
                // 1. Xác thực vé gốc của QR.
                var veGoc = await _db.Ve
                    .Include(v => v.SuKien)
                    .FirstOrDefaultAsync(v => v.VeId == veId);

                if (veGoc == null ||
                    veGoc.MaVe != parts[2])
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Mã QR không tồn tại hoặc đã bị thay đổi."
                    });
                }

                // 2. Chỉ BTC sở hữu sự kiện mới được check-in.
if (veGoc.SuKien.ToChucId != toChucId)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Vé không thuộc sự kiện đang kiểm soát."
                    });
                }


// 3. Cho phép check-in từ 00:00 ngày bắt đầu sự kiện
// đến trước thời điểm kết thúc sự kiện.

var now = DateTime.Now;
Console.WriteLine("===== KIEM TRA CHECK-IN =====");
Console.WriteLine($"Server Now: {now:dd/MM/yyyy HH:mm:ss}");
Console.WriteLine($"SuKien ID: {veGoc.SuKienId}");
Console.WriteLine($"Bat dau: {veGoc.SuKien.ThoiGianBatDau:dd/MM/yyyy HH:mm:ss}");
Console.WriteLine($"Ket thuc: {veGoc.SuKien.ThoiGianKetThuc:dd/MM/yyyy HH:mm:ss}");
Console.WriteLine("============================");
DateTime batDauCheckIn = veGoc.SuKien.ThoiGianBatDau.Date;
DateTime ketThucCheckIn = veGoc.SuKien.ThoiGianKetThuc;

if (now < batDauCheckIn || now >= ketThucCheckIn)
{
    return BadRequest(new
    {
        success = false,
        message = now < batDauCheckIn
            ? "Sự kiện chưa đến ngày tổ chức nên chưa thể check-in."
            : "Sự kiện đã kết thúc, không thể check-in."
    });
}


                // 4. Tìm tất cả vé cùng đơn.
                IQueryable<Ve> queryVe = _db.Ve;

                if (veGoc.DonHangId.HasValue)
                {
                    int donHangId = veGoc.DonHangId.Value;

                    queryVe = queryVe.Where(v =>
                        v.DonHangId == donHangId);
                }
                else
                {
                    queryVe = queryVe.Where(v =>
                        v.VeId == veGoc.VeId);
                }

                var danhSachVe = await queryVe
                    .Include(v => v.ChoNgoi)
                    .Include(v => v.LoaiVe)
                    .OrderBy(v => v.VeId)
                    .ToListAsync();

if (danhSachVe.Count == 0 ||
    danhSachVe.Any(v =>
        v.SuKienId != veGoc.SuKienId ||
        v.NguoiDungId != veGoc.NguoiDungId))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Các vé trong đơn không hợp lệ."
                    });
                }

                // 5. Kiểm tra đơn hàng chưa hủy, chưa hoàn.
                if (veGoc.DonHangId.HasValue)
                {
                    int donHangId = veGoc.DonHangId.Value;

                    var donHang = await _db.DonHang
                        .FirstOrDefaultAsync(d =>
                            d.DonHangId == donHangId &&
                            d.NguoiDungId == veGoc.NguoiDungId);

                    if (donHang == null ||
                        donHang.TrangThai == "Huy" ||
                        donHang.TrangThaiHoan == "DaHoan" ||
                        donHang.SoTienHoan > 0)
                    {
                        return BadRequest(new
                        {
                            success = false,
                            message = "Đơn hàng đã hủy, hoàn tiền hoặc không hợp lệ."
                        });
                    }
                }

                // 6. Tất cả vé phải ở trạng thái đã thanh toán.
                if (danhSachVe.Any(v =>
                    v.TrangThai != "DaThanhToan"))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Đơn có vé chưa thanh toán, đã hủy hoặc đã check-in."
                    });
                }

                var veIds = danhSachVe
                    .Select(v => v.VeId)
                    .ToList();

                // 7. Phải tồn tại giao dịch thanh toán thành công.
                bool daThanhToan = await _db.ThanhToan
                    .AnyAsync(tt =>
                        veIds.Contains(tt.VeId) &&
                        tt.TrangThai == "ThanhCong");

                if (!daThanhToan)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Không xác nhận được giao dịch thanh toán thành công."
                    });
                }

                // 8. Không check-in đơn đang chờ xử lý hủy.
                bool dangChoHuy = await _db.YeuCauHoanHuyVe
                    .AnyAsync(y =>
                        veIds.Contains(y.VeId) &&
                        y.TrangThai == "ChoDuyet");

                if (dangChoHuy)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Đơn đang chờ xử lý yêu cầu hủy vé."
                    });
                }

                // 9. Kiểm tra lịch sử check-in của tất cả vé.
                var lichSu = await _db.ThamGiaSuKien
                    .Where(t => veIds.Contains(t.VeId))
                    .ToListAsync();

                if (lichSu.Any(t => t.TrangThai == "DaCheckIn"))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Đơn đã có vé được check-in trước đó."
                    });
                }

                // Phòng trường hợp dữ liệu cũ có nhiều bản ghi
                // check-in trùng cho cùng một vé.
                if (lichSu.GroupBy(t => t.VeId)
                    .Any(g => g.Count() > 1))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Lịch sử check-in bị trùng. Cần kiểm tra dữ liệu."
                    });
                }

                // 10. Ghi nhận toàn bộ vé trong cùng transaction.
                foreach (var ve in danhSachVe)
                {
                    ve.TrangThai = "DaCheckIn";

                    var thamGia = lichSu
                        .FirstOrDefault(t => t.VeId == ve.VeId);

                    if (thamGia == null)
                    {
                        _db.ThamGiaSuKien.Add(
                            new ThamGiaSuKien
                            {
                                VeId = ve.VeId,
                                TrangThai = "DaCheckIn",
                                ThoiGianCheckIn = now
                            });
                    }
                    else
                    {
                        thamGia.TrangThai = "DaCheckIn";
                        thamGia.ThoiGianCheckIn = now;
                    }
                }

                await _db.SaveChangesAsync();
                await transaction.CommitAsync();

                // 11. Trả kết quả cho giao diện webcam.
                var ghe = danhSachVe.Select(v =>
                    v.ChoNgoi != null
                        ? $"{v.ChoNgoi.Hang}{v.ChoNgoi.Cot} - {v.LoaiVe.TenLoai}"
                        : $"Vé {v.MaVe} - {v.LoaiVe.TenLoai}"
                ).ToList();

                return Json(new
                {
                    success = true,
                    message = $"Check-in thành công {danhSachVe.Count} vé.",
                    donHangId = veGoc.DonHangId,
                    soVe = danhSachVe.Count,
                    danhSachGhe = ghe,
                    thoiGianCheckIn = now.ToString("dd/MM/yyyy HH:mm:ss")
                });
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();

                return StatusCode(500, new
                {
                    success = false,
                    message = "Có lỗi khi ghi nhận check-in. Không có vé nào được xác nhận."
                });
            }
        }


    }
}
