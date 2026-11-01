using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VeSuKienWeb.Data;
using VeSuKienWeb.Models;
using VeSuKienWeb.Models.Admin;
using VeSuKienWeb.Helpers;
using System.Text.Json;

namespace VeSuKienWeb.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly NguCanhSuKien _db;

        public AdminController(NguCanhSuKien db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            var list = await _db.SuKien
                .Include(s => s.LoaiSuKien)
                .Include(s => s.ToChucSuKien)
                .OrderBy(s => s.TrangThaiDuyet == "ChoDuyet" ? 0 : 1)
                .ThenByDescending(s => s.NgayDang)
                .ToListAsync();

            return View(list);
        }

        // ====== ADMIN XEM CHI TIẾT SỰ KIỆN ======
        [HttpGet]
        public async Task<IActionResult> ChiTiet(int id)
        {
            // Lấy sự kiện + loại + tổ chức
            var sk = await _db.SuKien
                .Include(s => s.LoaiSuKien)
                .Include(s => s.ToChucSuKien)
                .FirstOrDefaultAsync(s => s.SuKienId == id);

            if (sk == null) return NotFound();

            var loai = sk.LoaiSuKien;
            var toChuc = sk.ToChucSuKien;

            // Địa điểm (nếu là sự kiện trực tiếp)
            var diaDiem = await _db.DiaDiem
                .FirstOrDefaultAsync(d => d.SuKienId == sk.SuKienId);

            // Thông tin trực tuyến (nếu là sự kiện online)
            var trucTuyen = await _db.SuKienTrucTuyen
                .FirstOrDefaultAsync(t => t.SuKienId == sk.SuKienId);

            // Các loại vé
            var loaiVes = await _db.LoaiVe
                .Where(l => l.SuKienId == sk.SuKienId)
                .ToListAsync();


var soDo = await _db.SoDoSuKien
    .AsNoTracking()
    .FirstOrDefaultAsync(x => x.SuKienId == sk.SuKienId);

if (soDo != null && diaDiem != null)
{
    var ghe = await _db.SoDoChoNgoi
        .AsNoTracking()
        .Where(x => x.DiaDiemId == diaDiem.DiaDiemId)
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
        .OrderBy(x => x.hang)
        .ThenBy(x => x.cot)
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
else
{
    ViewBag.SoDoJson = "null";
}


            var vm = new SuKienChiTietVM
            {
                SuKien = sk,
                LoaiSuKien = loai,
                ToChuc = toChuc,
                DiaDiem = diaDiem,
                SuKienTrucTuyen = trucTuyen,
                LoaiVes = loaiVes
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Duyet(int id)
        {
            var sk = await _db.SuKien.FindAsync(id);
            if (sk == null) return NotFound();

            sk.TrangThaiDuyet = "DaDuyet";
            await _db.SaveChangesAsync();

            TempData["ThongBao"] = "Đã duyệt sự kiện.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TuChoi(int id)
        {
            var sk = await _db.SuKien.FindAsync(id);
            if (sk == null) return NotFound();

            sk.TrangThaiDuyet = "TuChoi";
            await _db.SaveChangesAsync();

            TempData["ThongBao"] = "Đã từ chối sự kiện.";
            return RedirectToAction("Index");
        }

        // ============================================================
        // ========== QUẢN LÝ YÊU CẦU HỦY / HOÀN VÉ ===================
        // ============================================================

        public async Task<IActionResult> YeuCauHoanHuy()
        {
            var list = await _db.YeuCauHoanHuyVe
                .OrderByDescending(y => y.NgayYeuCau)
                .ToListAsync();

            // Lấy danh sách Ve liên quan
            var veIds = list.Select(y => y.VeId).Distinct().ToList();

            var veMap = await _db.Ve
                .Where(v => veIds.Contains(v.VeId))
                .Include(v => v.SuKien)
                .Include(v => v.LoaiVe)
                .Include(v => v.NguoiDung)
                .ToDictionaryAsync(v => v.VeId, v => v);

            var vm = list.Select(y =>
            {
                veMap.TryGetValue(y.VeId, out var ve);

                return new YeuCauHoanHuyAdminVM
                {
                    YeuCauId = y.YeuCauId,
                    VeId = y.VeId,
                    MaVe = ve?.MaVe ?? string.Empty,
                    TenSuKien = ve?.SuKien?.TenSuKien ?? string.Empty,
                    TenNguoiDung = ve?.NguoiDung?.HoTen ?? string.Empty,
                    EmailNguoiDung = ve?.NguoiDung?.Email ?? string.Empty,
                    SoTien = ve?.LoaiVe?.GiaVe ?? 0,
                    NgayYeuCau = y.NgayYeuCau,
                    LyDo = y.LyDo,
                    HinhThuc = y.HinhThuc ?? string.Empty,
                    TrangThai = y.TrangThai ?? string.Empty
                };
            }).ToList();

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DuyetYeuCauHoanHuy(int id)
        {
            var yc = await _db.YeuCauHoanHuyVe
                .FirstOrDefaultAsync(y => y.YeuCauId == id);

            if (yc == null) return NotFound();

            if (yc.TrangThai != "ChoDuyet")
            {
                TempData["ThongBao"] = "Yêu cầu này đã được xử lý trước đó.";
                return RedirectToAction("YeuCauHoanHuy");
            }

            var ve = await _db.Ve
                .Include(v => v.ChoNgoi)
                .Include(v => v.SuKien)
                .FirstOrDefaultAsync(v => v.VeId == yc.VeId);

            if (ve == null)
            {
                TempData["ThongBao"] = "Không tìm thấy vé tương ứng.";
                return RedirectToAction("YeuCauHoanHuy");
            }

            // ✅ Cập nhật trạng thái vé
            ve.TrangThai = "Huy";

            // ✅ Nếu có ghế thì mở lại ghế
            if (ve.ChoNgoi != null)
            {
                ve.ChoNgoi.TrangThai = "Trong";
            }

            // ✅ Đánh dấu yêu cầu đã duyệt
            yc.TrangThai = "DaDuyet";

            // ✅ Ghi thông báo cho khách
            _db.ThongBao.Add(new ThongBao
            {
                NguoiDungId = ve.NguoiDungId,
                NoiDung = $"Yêu cầu hủy/hoàn vé {ve.MaVe} cho sự kiện \"{ve.SuKien.TenSuKien}\" đã được chấp nhận.",
                NgayGui = DateTime.Now,
                DaDoc = false
            });

            await _db.SaveChangesAsync();

            TempData["ThongBao"] = "Đã duyệt yêu cầu hủy/hoàn vé. Vé đã chuyển sang trạng thái Hủy và ghế được mở lại.";
            return RedirectToAction("YeuCauHoanHuy");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TuChoiYeuCauHoanHuy(int id)
        {
            var yc = await _db.YeuCauHoanHuyVe
                .FirstOrDefaultAsync(y => y.YeuCauId == id);

            if (yc == null) return NotFound();

            if (yc.TrangThai != "ChoDuyet")
            {
                TempData["ThongBao"] = "Yêu cầu này đã được xử lý trước đó.";
                return RedirectToAction("YeuCauHoanHuy");
            }

            yc.TrangThai = "TuChoi";

            var ve = await _db.Ve
                .Include(v => v.SuKien)
                .FirstOrDefaultAsync(v => v.VeId == yc.VeId);

            if (ve != null)
            {
                _db.ThongBao.Add(new ThongBao
                {
                    NguoiDungId = ve.NguoiDungId,
                    NoiDung = $"Yêu cầu hủy/hoàn vé {ve.MaVe} cho sự kiện \"{ve.SuKien.TenSuKien}\" đã bị từ chối.",
                    NgayGui = DateTime.Now,
                    DaDoc = false
                });
            }

            await _db.SaveChangesAsync();

            TempData["ThongBao"] = "Đã từ chối yêu cầu hủy/hoàn vé.";
            return RedirectToAction("YeuCauHoanHuy");
        }
    
    
        // ============================================================
        // ========== QUẢN LÝ NGƯỜI DÙNG ==============================
        // ============================================================

        [HttpGet("/Admin/NguoiDung")]
        public async Task<IActionResult> NguoiDung()
        {
            var list = await _db.NguoiDung
                .OrderByDescending(n => n.NgayTao)
                .ToListAsync();

            return View(list);
        }

        [HttpGet("/Admin/TaoBanToChuc")]
        public IActionResult TaoBanToChuc()
        {
            return View();
        }

        [HttpPost("/Admin/TaoBanToChuc")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TaoBanToChuc(NguoiDung model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var emailDaTonTai = await _db.NguoiDung
                .AnyAsync(n => n.Email == model.Email);

            if (emailDaTonTai)
            {
                ModelState.AddModelError("Email", "Email này đã được sử dụng.");
                return View(model);
            }

            model.VaiTro = "ToChuc";
            model.NgayTao = DateTime.Now;
            model.MatKhau = MatKhauHelper.BamMatKhau(model.MatKhau);

            _db.NguoiDung.Add(model);
            await _db.SaveChangesAsync();

            TempData["ThongBao"] = "Đã tạo tài khoản ban tổ chức thành công.";
            return RedirectToAction("NguoiDung");
        }
[HttpGet("/Admin/SuaNguoiDung/{id}")]
public async Task<IActionResult> SuaNguoiDung(int id)
{
    var user = await _db.NguoiDung.FindAsync(id);
    if (user == null) return NotFound();

    return View(user);
}

[HttpPost("/Admin/SuaNguoiDung/{id}")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> SuaNguoiDung(int id, NguoiDung model)
{
    var user = await _db.NguoiDung.FindAsync(id);
    if (user == null) return NotFound();

    user.HoTen = model.HoTen;
    user.Email = model.Email;
    user.DienThoai = model.DienThoai;
    user.VaiTro = model.VaiTro;

    await _db.SaveChangesAsync();

    TempData["ThongBao"] = "Đã cập nhật thông tin người dùng.";
    return RedirectToAction("NguoiDung");
}

[HttpPost("/Admin/KhoaMoKhoaNguoiDung")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> KhoaMoKhoaNguoiDung(int id)
{
    var user = await _db.NguoiDung.FindAsync(id);
    if (user == null) return NotFound();

    user.TrangThai = user.TrangThai == "BiKhoa" ? "HoatDong" : "BiKhoa";

    await _db.SaveChangesAsync();

    TempData["ThongBao"] = "Đã cập nhật trạng thái tài khoản.";
    return RedirectToAction("NguoiDung");
}
[HttpGet("/Admin/ThongKe")]
public async Task<IActionResult> ThongKe()
{
    var theoLoai = await _db.SuKien
        .Include(x => x.LoaiSuKien)
        .GroupBy(x => x.LoaiSuKien!.TenLoai)
        .Select(g => new
        {
            TenLoai = g.Key ?? "Khác",
            SoLuong = g.Count()
        })
        .ToListAsync();

    var suKienTheoThang = await _db.SuKien
        .GroupBy(x => new { x.NgayDang.Year, x.NgayDang.Month })
        .OrderBy(g => g.Key.Year)
        .ThenBy(g => g.Key.Month)
        .Select(g => new
        {
            Thang = $"{g.Key.Month:00}/{g.Key.Year}",
            SoLuong = g.Count()
        })
        .ToListAsync();

    var nguoiDungTheoThang = await _db.NguoiDung
        .GroupBy(x => new { x.NgayTao.Year, x.NgayTao.Month })
        .OrderBy(g => g.Key.Year)
        .ThenBy(g => g.Key.Month)
        .Select(g => new
        {
            Thang = $"{g.Key.Month:00}/{g.Key.Year}",
            SoLuong = g.Count()
        })
        .ToListAsync();

    var soTrucTiep = await _db.DiaDiem
        .Select(x => x.SuKienId)
        .Distinct()
        .CountAsync();

    var soTrucTuyen = await _db.SuKienTrucTuyen
        .Select(x => x.SuKienId)
        .Distinct()
        .CountAsync();

    var hybridIds = await _db.DiaDiem
        .Select(x => x.SuKienId)
        .Intersect(_db.SuKienTrucTuyen.Select(x => x.SuKienId))
        .CountAsync();

    var vm = new AdminDashboardVM
    {
        TongNguoiDung = await _db.NguoiDung.CountAsync(),
        TongBanToChuc = await _db.NguoiDung.CountAsync(x => x.VaiTro == "ToChuc"),
        TongSuKien = await _db.SuKien.CountAsync(),
        SuKienChoDuyet = await _db.SuKien.CountAsync(x => x.TrangThaiDuyet == "ChoDuyet"),
        SuKienDaDuyet = await _db.SuKien.CountAsync(x => x.TrangThaiDuyet == "DaDuyet"),
        SuKienTuChoi = await _db.SuKien.CountAsync(x => x.TrangThaiDuyet == "TuChoi"),

        TenLoaiSuKien = theoLoai.Select(x => x.TenLoai).ToList(),
        SoLuongTheoLoai = theoLoai.Select(x => x.SoLuong).ToList(),

        SuKienHybrid = hybridIds,
        SuKienTrucTiep = soTrucTiep - hybridIds,
        SuKienTrucTuyen = soTrucTuyen - hybridIds,

        ThangSuKien = suKienTheoThang.Select(x => x.Thang).ToList(),
        SoSuKienTheoThang = suKienTheoThang.Select(x => x.SoLuong).ToList(),

        ThangNguoiDung = nguoiDungTheoThang.Select(x => x.Thang).ToList(),
        SoNguoiDungTheoThang = nguoiDungTheoThang.Select(x => x.SoLuong).ToList()
    };

    return View(vm);
}
}
}