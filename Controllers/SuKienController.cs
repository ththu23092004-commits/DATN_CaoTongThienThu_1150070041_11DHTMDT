using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using VeSuKienWeb.Data;
using VeSuKienWeb.Models.SuKienViewModels;

namespace VeSuKienWeb.Controllers
{
    public class SuKienController : Controller
    {
        private readonly NguCanhSuKien _db;

        public SuKienController(NguCanhSuKien db)
        {
            _db = db;
        }

        // DANH SÁCH SỰ KIỆN ĐÃ DUYỆT + TÌM KIẾM
        public async Task<IActionResult> DanhSach(string? keyword)
        {
            var query = _db.SuKien
                .Include(s => s.LoaiSuKien)
                
                .Include(s => s.DiaDiem)
                .Where(s => s.TrangThaiDuyet == "DaDuyet")
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.Trim();

                query = query.Where(s =>
                    s.TenSuKien.Contains(keyword) ||
                    (s.MoTaChiTiet != null && s.MoTaChiTiet.Contains(keyword))
                );
            }

            var list = await query
                .OrderBy(s => s.ThoiGianBatDau)
                .ToListAsync();

            // để nếu cần hiển thị lại keyword trên view
            ViewBag.Keyword = keyword;

            return View(list);
        }

        // CHI TIẾT SỰ KIỆN CHO KHÁCH
        public async Task<IActionResult> ChiTiet(int id)
        {
            var sk = await _db.SuKien
                .Include(s => s.LoaiSuKien)
                .Include(s => s.ToChucSuKien)
                .FirstOrDefaultAsync(s => s.SuKienId == id && s.TrangThaiDuyet == "DaDuyet");

            if (sk == null)
                return NotFound();

            var diaDiem = await _db.DiaDiem
                .FirstOrDefaultAsync(d => d.SuKienId == sk.SuKienId);

            var online = await _db.SuKienTrucTuyen
                .FirstOrDefaultAsync(t => t.SuKienId == sk.SuKienId);

            var loaiVes = await _db.LoaiVe
                .Where(l => l.SuKienId == sk.SuKienId)
                .ToListAsync();

            // ⭐ CHECK USER ĐÃ THANH TOÁN VÉ CHƯA
            bool daMuaVe = false;

            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);

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

                // ⭐ GỬI FLAG VỀ VIEW
                DaMuaVe = daMuaVe
            };

            return View(vm);
        }
    }
}
