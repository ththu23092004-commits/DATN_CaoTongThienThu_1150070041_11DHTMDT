
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using VeSuKienWeb.Data;
using VeSuKienWeb.Models.SuKienViewModels;
using VeSuKienWeb.Models.KhachHang;
using VeSuKienWeb.Services;

namespace VeSuKienWeb.Controllers
{
    public class SuKienController : Controller
    {
        private readonly NguCanhSuKien _db;
        private readonly GoiYSuKienService _goiYService;

        public SuKienController(
            NguCanhSuKien db,
            GoiYSuKienService goiYService)
        {
            _db = db;
            _goiYService = goiYService;
        }

        // ============================================================
        // DANH SÁCH SỰ KIỆN
        // ============================================================

        [AllowAnonymous]
        public async Task<IActionResult> DanhSach(
            string? search,
            string? keyword)
        {
            var now = DateTime.Now;

            // Hỗ trợ cả tên tham số mới và tham số cũ.
            if (string.IsNullOrWhiteSpace(search))
            {
                search = keyword;
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();
            }

            // Lấy các sự kiện đã được Admin duyệt.
            var query = _db.SuKien
                .AsNoTracking()
                .Where(s => s.TrangThaiDuyet == "DaDuyet");

            // Tìm kiếm theo tên và mô tả.
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(s =>
                    s.TenSuKien.Contains(search) ||
                    (s.MoTaChiTiet ?? "").Contains(search)
                );
            }

            // Chuyển dữ liệu sang ViewModel dùng chung
            // với giao diện KhachHang/Index.
            var list = await query
                .OrderBy(s => s.ThoiGianBatDau)
                .Select(s => new SuKienListItemKhVM
                {
                    SuKienId = s.SuKienId,

                    TenSuKien = s.TenSuKien,

                    TenLoai = s.LoaiSuKien != null
                        ? s.LoaiSuKien.TenLoai
                        : null,

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

            // ========================================================
            // GỢI Ý CÁ NHÂN HÓA TOP 6
            // ========================================================

            var goiYSuKien = new List<SuKienListItemKhVM>();

            if (User.Identity?.IsAuthenticated == true &&
                User.IsInRole("KhachHang") &&
                string.IsNullOrWhiteSpace(search))
            {
                var idStr = User.FindFirstValue(
                    ClaimTypes.NameIdentifier
                );

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

            // ========================================================
            // CẤU HÌNH VIEW DÙNG CHUNG
            // ========================================================

            ViewBag.GoiYSuKien = goiYSuKien;
            ViewBag.Search = search;
            ViewBag.Keyword = search;

            // Giữ URL thuộc SuKienController.
            ViewBag.RouteController = "SuKien";
            ViewBag.RouteAction = "DanhSach";

            // Dùng chung giao diện mới của khách hàng.
            return View(
                "~/Views/KhachHang/Index.cshtml",
                list
            );
        }

        // ============================================================
        // CHI TIẾT SỰ KIỆN CHO KHÁCH
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

            var diaDiem = await _db.DiaDiem
                .FirstOrDefaultAsync(d =>
                    d.SuKienId == sk.SuKienId);

            var online = await _db.SuKienTrucTuyen
                .FirstOrDefaultAsync(t =>
                    t.SuKienId == sk.SuKienId);

            var loaiVes = await _db.LoaiVe
                .Where(l => l.SuKienId == sk.SuKienId)
                .ToListAsync();

            // Kiểm tra người dùng đã mua vé chưa.
            bool daMuaVe = false;

            if (User.Identity != null &&
                User.Identity.IsAuthenticated)
            {
                var idStr = User.FindFirstValue(
                    ClaimTypes.NameIdentifier
                );

                if (int.TryParse(idStr, out int nguoiDungId))
                {
                    daMuaVe = await _db.Ve.AnyAsync(v =>
                        v.SuKienId == sk.SuKienId &&
                        v.NguoiDungId == nguoiDungId &&
                        v.TrangThai == "DaThanhToan"
                    );
                }
            }

            var vm = new SuKienChiTietKhachVM
            {
                SuKien = sk,
                LoaiSuKien = sk.LoaiSuKien,
                ToChuc = sk.ToChucSuKien,
                DiaDiem = diaDiem,
                SuKienTrucTuyen = online,
                LoaiVes = loaiVes,
                DaMuaVe = daMuaVe
            };

            return View(vm);
        }
    }
}
