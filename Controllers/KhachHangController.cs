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

namespace VeSuKienWeb.Controllers
{
    public class KhachHangController : Controller
    {
        private readonly NguCanhSuKien _db;
        private readonly ILogger<KhachHangController> _logger;

        public KhachHangController(NguCanhSuKien db, ILogger<KhachHangController> logger)
        {
            _db = db;
            _logger = logger;
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
                    TenLoai = s.LoaiSuKien != null ? s.LoaiSuKien.TenLoai : null,
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
                .FirstOrDefaultAsync(s => s.SuKienId == id && s.TrangThaiDuyet == "DaDuyet");

            if (sk == null) return NotFound();

            var now = DateTime.Now;
            bool coTheDatVe = sk.ThoiGianKetThuc > now;

            var diaDiem = await _db.DiaDiem
                .FirstOrDefaultAsync(d => d.SuKienId == sk.SuKienId);

            var online = await _db.SuKienTrucTuyen
                .FirstOrDefaultAsync(o => o.SuKienId == sk.SuKienId);

            var loaiVes = await _db.LoaiVe
                .Where(l => l.SuKienId == sk.SuKienId)
                .ToListAsync();

            var vm = new SuKienChiTietKhVM
            {
                SuKien = sk,
                LoaiSuKien = sk.LoaiSuKien,
                ToChuc = sk.ToChucSuKien,
                DiaDiem = diaDiem,
                SuKienTrucTuyen = online,
                LoaiVes = loaiVes,
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
            var sk = await _db.SuKien
                .Include(s => s.LoaiSuKien)
                .FirstOrDefaultAsync(s => s.SuKienId == id && s.TrangThaiDuyet == "DaDuyet");

            if (sk == null) return NotFound();

            if (sk.ThoiGianKetThuc <= DateTime.Now)
            {
                TempData["ThongBao"] = "Sự kiện đã kết thúc, không thể đặt vé.";
                return RedirectToAction("ChiTiet", new { id });
            }

            var loaiTenLower = (sk.LoaiSuKien?.TenLoai ?? "").ToLower();
            bool laTrucTiep = loaiTenLower.Contains("trực tiếp") || loaiTenLower.Contains("truc tiep");
            bool laWorkshop = loaiTenLower.Contains("workshop");
            bool laSuKienCoSoDoGhe = laTrucTiep || laWorkshop;

            var vm = new DatVeKhVM
            {
                SuKienId = sk.SuKienId,
                TenSuKien = sk.TenSuKien,
                LaSuKienTrucTiep = laSuKienCoSoDoGhe,
                LaWorkshop = laWorkshop
            };

            if (laSuKienCoSoDoGhe)
            {
                var diaDiem = await _db.DiaDiem
                    .FirstOrDefaultAsync(d => d.SuKienId == sk.SuKienId);

                if (diaDiem == null)
                {
                    TempData["ThongBao"] = "Sự kiện chưa cấu hình địa điểm.";
                    return RedirectToAction("ChiTiet", new { id });
                }

                var gheList = await _db.SoDoChoNgoi
                    .Include(g => g.LoaiVe)
                    .Where(g => g.DiaDiemId == diaDiem.DiaDiemId)
                    .OrderBy(g => g.Hang)
                    .ThenBy(g => g.Cot)
                    .ToListAsync();

                vm.GheList = gheList.Select(g => new DatVeGheItemVM
                {
                    ChoNgoiId = g.ChoNgoiId,
                    Hang = g.Hang ?? string.Empty,
                    Cot = g.Cot,
                    TrangThai = g.TrangThai ?? "Trong",
                    TenLoaiVe = g.LoaiVe.TenLoai ?? string.Empty,
                    GiaVe = g.LoaiVe.GiaVe
                }).ToList();

                var thongKeTheoHang = vm.GheList
                    .GroupBy(g => g.Hang)
                    .OrderBy(g => g.Key)
                    .Select(g => new
                    {
                        Hang = g.Key,
                        SoGhe = g.Count(),
                        CotMin = g.Min(x => x.Cot),
                        CotMax = g.Max(x => x.Cot),
                        SoVip = g.Count(x => (x.TenLoaiVe ?? string.Empty).ToLower().Contains("vip"))
                    })
                    .ToList();

                _logger.LogWarning(
                    "[DatVeDebug] SuKienId={SuKienId}, LaWorkshop={LaWorkshop}, TongGhe={TongGhe}, TheoHang={@TheoHang}",
                    sk.SuKienId,
                    laWorkshop,
                    vm.GheList.Count,
                    thongKeTheoHang);
            }
            else
            {
                vm.LaSuKienTrucTiep = false;

                // ✅ SỰ KIỆN TRỰC TUYẾN: tự chọn loại vé mặc định (ví dụ: rẻ nhất)
                var loaiVeMacDinh = await _db.LoaiVe
                    .Where(l => l.SuKienId == sk.SuKienId)
                    .OrderBy(l => l.GiaVe)
                    .FirstOrDefaultAsync();

                if (loaiVeMacDinh == null)
                {
                    TempData["ThongBao"] = "Sự kiện trực tuyến chưa cấu hình loại vé, không thể đặt vé.";
                    return RedirectToAction("ChiTiet", new { id = sk.SuKienId });
                }

                vm.LoaiVeId = loaiVeMacDinh.LoaiVeId;
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
                .FirstOrDefaultAsync(s => s.SuKienId == model.SuKienId && s.TrangThaiDuyet == "DaDuyet");

            if (sk == null) return NotFound();

            if (sk.ThoiGianKetThuc <= DateTime.Now)
            {
                TempData["ThongBao"] = "Sự kiện đã kết thúc, không thể đặt vé.";
                return RedirectToAction("ChiTiet", new { id = model.SuKienId });
            }

            var loaiTenLower = (sk.LoaiSuKien?.TenLoai ?? "").ToLower();
            bool laTrucTiep = loaiTenLower.Contains("trực tiếp") || loaiTenLower.Contains("truc tiep");
            bool laWorkshop = loaiTenLower.Contains("workshop");
            bool laSuKienCoSoDoGhe = laTrucTiep || laWorkshop;
            int userId = GetCurrentUserId();

            // ========================================================
            // ✅ GIỚI HẠN TỐI ĐA 10 VÉ / 1 LẦN ĐẶT
            // ========================================================
            if (laSuKienCoSoDoGhe)
            {
                if (model.GheChonIds == null || !model.GheChonIds.Any())
                {
                    TempData["ThongBao"] = "Vui lòng chọn ít nhất 1 ghế.";
                    return RedirectToAction("DatVe", new { id = model.SuKienId });
                }

                if (model.GheChonIds.Count > 10)
                {
                    TempData["ThongBao"] = "Bạn chỉ được đặt tối đa 10 vé trong một lần.";
                    return RedirectToAction("DatVe", new { id = model.SuKienId });
                }
            }
            else
            {
                // ✅ TRỰC TUYẾN: chỉ kiểm tra số lượng + đảm bảo LoaiVeId đã có sẵn
                if (model.SoLuong <= 0)
                {
                    TempData["ThongBao"] = "Vui lòng nhập số lượng vé hợp lệ.";
                    return RedirectToAction("DatVe", new { id = model.SuKienId });
                }

                if (model.SoLuong > 10)
                {
                    TempData["ThongBao"] = "Bạn chỉ được đặt tối đa 10 vé trong một lần.";
                    return RedirectToAction("DatVe", new { id = model.SuKienId });
                }

                if (!model.LoaiVeId.HasValue)
                {
                    TempData["ThongBao"] = "Sự kiện chưa được cấu hình loại vé, không thể đặt vé trực tuyến.";
                    return RedirectToAction("ChiTiet", new { id = model.SuKienId });
                }
            }

            // ========================================================
            // ✅ CẢNH BÁO TRÙNG THỜI GIAN VỚI SỰ KIỆN KHÁC (CHỈ CẢNH BÁO)
            // ========================================================
            var suKienTrung = await _db.Ve
                .Include(v => v.SuKien)
                .Where(v => v.NguoiDungId == userId
                            && v.TrangThai == "DaThanhToan"
                            && v.SuKienId != sk.SuKienId
                            && v.SuKien.ThoiGianBatDau < sk.ThoiGianKetThuc
                            && v.SuKien.ThoiGianKetThuc > sk.ThoiGianBatDau)
                .Select(v => v.SuKien)
                .Distinct()
                .ToListAsync();

            if (suKienTrung.Any())
            {
                TempData["CanhBaoTrungLich"] = "Lưu ý: Bạn đã có sự kiện khác trong khoảng thời gian này. " +
                                                "Nếu tiếp tục, lịch sẽ bị trùng.";
            }

            // ========================================================
            // Tạo đơn hàng
            // ========================================================
            var donHang = new DonHang
            {
                NguoiDungId = userId,
                NgayDat = DateTime.Now,
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
                    TempData["ThongBao"] = "Sự kiện chưa cấu hình địa điểm.";
                    return RedirectToAction("ChiTiet", new { id = model.SuKienId });
                }

                var gheChon = await _db.SoDoChoNgoi
                    .Include(g => g.LoaiVe)
                    .Where(g => model.GheChonIds.Contains(g.ChoNgoiId)
                             && g.DiaDiemId == diaDiem.DiaDiemId)
                    .ToListAsync();

                if (!gheChon.Any())
                {
                    TempData["ThongBao"] = "Không tìm thấy ghế đã chọn.";
                    return RedirectToAction("DatVe", new { id = model.SuKienId });
                }

                foreach (var ghe in gheChon)
                {
                    if (!string.Equals(ghe.TrangThai, "Trong", StringComparison.OrdinalIgnoreCase))
                        continue;

                    string maVe = $"V{DateTime.Now:yyyyMMddHHmmss}{Guid.NewGuid().ToString("N")[..4]}";

                    var ve = new Ve
                    {
                        NguoiDungId = userId,
                        LoaiVeId = ghe.LoaiVeId,
                        SuKienId = sk.SuKienId,
                        ChoNgoiId = ghe.ChoNgoiId,
                        MaVe = maVe,
                        NgayDat = DateTime.Now,
                        TrangThai = "ChuaThanhToan",
                        DonHangId = donHang.DonHangId
                    };

                    _db.Ve.Add(ve);
                    tongTien += ghe.LoaiVe.GiaVe;
                }
            }
            else
            {
                // ===== ĐẶT VÉ TRỰC TUYẾN =====
                var loaiVe = await _db.LoaiVe
                    .FirstOrDefaultAsync(l => l.LoaiVeId == model.LoaiVeId!.Value && l.SuKienId == sk.SuKienId);

                if (loaiVe == null)
                {
                    TempData["ThongBao"] = "Loại vé không hợp lệ.";
                    return RedirectToAction("DatVe", new { id = model.SuKienId });
                }

                for (int i = 0; i < model.SoLuong; i++)
                {
                    string maVe = $"V{DateTime.Now:yyyyMMddHHmmss}{Guid.NewGuid().ToString("N")[..4]}";

                    var ve = new Ve
                    {
                        NguoiDungId = userId,
                        LoaiVeId = loaiVe.LoaiVeId,
                        SuKienId = sk.SuKienId,
                        ChoNgoiId = null,
                        MaVe = maVe,
                        NgayDat = DateTime.Now,
                        TrangThai = "ChuaThanhToan",
                        DonHangId = donHang.DonHangId
                    };

                    _db.Ve.Add(ve);
                    tongTien += loaiVe.GiaVe;
                }
            }

            if (tongTien <= 0)
            {
                TempData["ThongBao"] = "Không tạo được vé hợp lệ.";
                return RedirectToAction("ChiTiet", new { id = model.SuKienId });
            }

            donHang.TongTien = tongTien;

            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                TempData["ThongBao"] = "Có lỗi khi lưu dữ liệu. Vui lòng thử lại.";
                return RedirectToAction("ChiTiet", new { id = model.SuKienId });
            }

            return RedirectToAction("ThanhToan", new { id = donHang.DonHangId });
        }

        // ============================================================
        // ========== THANH TOÁN (GIẢ LẬP QR) =========================
        // ============================================================

        [Authorize(Roles = "KhachHang")]
        [HttpGet]
        public async Task<IActionResult> ThanhToan(int id)
        {
            int userId = GetCurrentUserId();

            var donHang = await _db.DonHang
                .Include(d => d.Ves)
                    .ThenInclude(v => v.SuKien)
                .Include(d => d.Ves)
                    .ThenInclude(v => v.LoaiVe)
                .Include(d => d.Ves)
                    .ThenInclude(v => v.ChoNgoi)
                .FirstOrDefaultAsync(d => d.DonHangId == id && d.NguoiDungId == userId);

            if (donHang == null) return NotFound();

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

        [Authorize(Roles = "KhachHang")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ThanhToanXacNhan(int id)
        {
            int userId = GetCurrentUserId();

            var donHang = await _db.DonHang
                .Include(d => d.Ves)
                    .ThenInclude(v => v.ChoNgoi)
                .Include(d => d.Ves)
                    .ThenInclude(v => v.LoaiVe)
                .FirstOrDefaultAsync(d => d.DonHangId == id && d.NguoiDungId == userId);

            if (donHang == null) return NotFound();

            if (donHang.TrangThai == "ThanhCong")
            {
                TempData["ThongBao"] = "Đơn hàng này đã được thanh toán trước đó.";
                return RedirectToAction("ThanhToan", new { id });
            }

            foreach (var ve in donHang.Ves)
            {
                var tt = new ThanhToan
                {
                    VeId = ve.VeId,
                    PhuongThuc = "QR Demo",
                    MaGiaoDich = "DEMO-" + Guid.NewGuid().ToString("N")[..8],
                    TrangThai = "ThanhCong",
                    NgayThanhToan = DateTime.Now,
                    SoTien = ve.LoaiVe.GiaVe
                };
                _db.ThanhToan.Add(tt);

                ve.TrangThai = "DaThanhToan";

                if (ve.ChoNgoiId.HasValue && ve.ChoNgoi != null)
                {
                    ve.ChoNgoi.TrangThai = "DaDat";
                }
            }

            donHang.TrangThai = "ThanhCong";
            await _db.SaveChangesAsync();

            TempData["ThongBao"] = "Thanh toán thành công (giả lập). Vé của bạn đã được kích hoạt.";
            return RedirectToAction("ThanhToanThanhCong", new { id = donHang.DonHangId });
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
                VeId = first.VeId,
                SuKienId = first.SuKienId,
                MaVe = first.MaVe,

                TenSuKien = first.SuKien.TenSuKien,
                TenLoaiSuKien = tenLoai,
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

                CoTheHuy = g.All(x => x.TrangThai == "DaThanhToan")
                           && first.SuKien.ThoiGianBatDau > now
                           && !g.Any(x => _db.YeuCauHoanHuyVe
                               .Any(yc => yc.VeId == x.VeId && yc.TrangThai == "ChoDuyet"))
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
        VeId = first.VeId,
        SuKienId = first.SuKienId,
        MaVe = first.MaVe,

        TenSuKien = first.SuKien.TenSuKien,
        TenLoaiSuKien = tenLoai,
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

        CoTheHuy = veRaw.All(x => x.TrangThai == "DaThanhToan")
                   && first.SuKien.ThoiGianBatDau > now
                   && !veRaw.Any(x => _db.YeuCauHoanHuyVe
                       .Any(yc => yc.VeId == x.VeId && yc.TrangThai == "ChoDuyet"))
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
        .Include(v => v.SuKien)
        .FirstOrDefaultAsync(v => v.VeId == veId && v.NguoiDungId == userId);

    if (ve == null)
    {
        return NotFound();
    }

if (ve.SuKien.ThoiGianKetThuc < now)
{
    TempData["QrError"] = "Sự kiện đã kết thúc nên không thể xem mã QR.";
    TempData["QrMaVe"] = ve.MaVe;
    return RedirectToAction("VeCuaToi");
}

    string qrText = $"CHECKIN|{ve.VeId}|{ve.MaVe}";

    using var qrGenerator = new QRCodeGenerator();
    QRCodeData qrCodeData = qrGenerator.CreateQrCode(qrText, QRCodeGenerator.ECCLevel.Q);

    var pngQr = new PngByteQRCode(qrCodeData);
    byte[] qrBytes = pngQr.GetGraphic(20);

    string base64 = Convert.ToBase64String(qrBytes);

    var vm = new MaQrVM
    {
        VeId = ve.VeId,
        MaVe = ve.MaVe,
        QrCodeBase64 = base64,
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

    if (nhomVe.Any(v => v.TrangThai != "DaThanhToan") || veGoc.SuKien.ThoiGianBatDau <= DateTime.Now)
    {
        TempData["ThongBao"] = "Vé này không đủ điều kiện để yêu cầu hủy/hoàn.";
        return RedirectToAction("VeCuaToi");
    }

    bool daCoYeuCauChoDuyet = await _db.YeuCauHoanHuyVe
        .AnyAsync(yc => nhomVe.Select(v => v.VeId).Contains(yc.VeId)
                        && yc.TrangThai == "ChoDuyet");

    if (daCoYeuCauChoDuyet)
    {
        TempData["ThongBao"] = "Bạn đã gửi yêu cầu hủy/hoàn cho vé này và đang chờ xử lý.";
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

    if (nhomVe.Any(v => v.TrangThai != "DaThanhToan") || veGoc.SuKien.ThoiGianBatDau <= DateTime.Now)
    {
        TempData["ThongBao"] = "Vé này không đủ điều kiện để yêu cầu hủy/hoàn.";
        return RedirectToAction("VeCuaToi");
    }

    bool daCoYeuCauChoDuyet = await _db.YeuCauHoanHuyVe
        .AnyAsync(yc => nhomVe.Select(v => v.VeId).Contains(yc.VeId)
                        && yc.TrangThai == "ChoDuyet");

    if (daCoYeuCauChoDuyet)
    {
        TempData["ThongBao"] = "Bạn đã có yêu cầu hủy/hoàn đang chờ xử lý cho vé này.";
        return RedirectToAction("VeCuaToi");
    }

    foreach (var ve in nhomVe)
    {
        var yeuCau = new YeuCauHoanHuyVe
        {
            VeId = ve.VeId,
            LyDo = model.LyDo,
            NgayYeuCau = DateTime.Now,
            TrangThai = "ChoDuyet",
            HinhThuc = "HoanTien"
        };

        _db.YeuCauHoanHuyVe.Add(yeuCau);
    }

    await _db.SaveChangesAsync();

    TempData["ThongBao"] = "Gửi yêu cầu hủy/hoàn vé thành công. Vui lòng chờ hệ thống xử lý.";
    return RedirectToAction("VeCuaToi");
}

        // ============================================================
        // ========== SAU KHI THANH TOÁN THÀNH CÔNG ===================
        // ============================================================

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
