
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VeSuKienWeb.Data;
using VeSuKienWeb.Models;
using VeSuKienWeb.Services;

namespace VeSuKienWeb.Controllers
{
    public class HomeController : Controller
    {
        private readonly NguCanhSuKien _db;
        private readonly GoiYSuKienService _goiYService;

        public HomeController(
            NguCanhSuKien db,
            GoiYSuKienService goiYService)
        {
            _db = db;
            _goiYService = goiYService;
        }

        public async Task<IActionResult> Index()
        {
            var now = DateTime.Now;

            // Giữ nguyên dữ liệu cho Carousel và dải cuộn phim.
            var suKienNoiBat = await _db.SuKien
                .AsNoTracking()
                .Include(s => s.LoaiSuKien)
                .Include(s => s.ToChucSuKien)
                .Include(s => s.DiaDiem)
                .Where(s =>
                    s.TrangThaiDuyet == "DaDuyet" &&
                    s.ThoiGianKetThuc > now)
                .OrderBy(s => s.ThoiGianBatDau)
                .Take(6)
                .ToListAsync();

            // Danh sách tối đa 10 sự kiện sắp diễn ra.
            var suKienSapDienRa = await _db.SuKien
                .AsNoTracking()
                .Include(s => s.LoaiSuKien)
                .Include(s => s.ToChucSuKien)
                .Include(s => s.DiaDiem)
                .Where(s =>
                    s.TrangThaiDuyet == "DaDuyet" &&
                    s.ThoiGianBatDau > now &&
                    s.ThoiGianKetThuc > now)
                .OrderBy(s => s.ThoiGianBatDau)
                .Take(10)
                .ToListAsync();

            var goiYSuKien = new List<SuKien>();

            // Giữ nguyên thuật toán gợi ý cá nhân hóa Top-6.
            if (User.Identity?.IsAuthenticated == true &&
                User.IsInRole("KhachHang"))
            {
                var idStr = User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

                if (int.TryParse(idStr, out int userId))
                {
                    var ids = await _goiYService.LayGoiYAsync(
                        userId,
                        topN: 5);

                    if (ids.Count > 0)
                    {
                        var suKien = await _db.SuKien
                            .AsNoTracking()
                            .Include(s => s.LoaiSuKien)
                            .Include(s => s.DiaDiem)
                            .Where(s =>
                                ids.Contains(s.SuKienId) &&
                                s.TrangThaiDuyet == "DaDuyet" &&
                                s.ThoiGianKetThuc > now)
                            .ToListAsync();

                        var thuTu = ids
                            .Select((id, index) => new
                            {
                                Id = id,
                                Position = index
                            })
                            .ToDictionary(
                                x => x.Id,
                                x => x.Position);

                        goiYSuKien = suKien
                            .OrderBy(s => thuTu[s.SuKienId])
                            .ToList();
                    }
                }
            }

            // Tổng hợp ID để lấy giá vé cho tất cả khu vực.
            var danhSachId = suKienNoiBat
                .Select(x => x.SuKienId)
                .Concat(suKienSapDienRa.Select(x => x.SuKienId))
                .Concat(goiYSuKien.Select(x => x.SuKienId))
                .Distinct()
                .ToList();

            var giaVeTheoSuKien =
                new Dictionary<int, decimal>();

            if (danhSachId.Count > 0)
            {
                var bangGia = await _db.LoaiVe
                    .AsNoTracking()
                    .Where(v =>
                        danhSachId.Contains(v.SuKienId))
                    .GroupBy(v => v.SuKienId)
                    .Select(g => new
                    {
                        SuKienId = g.Key,
                        GiaThapNhat = g.Min(v => v.GiaVe)
                    })
                    .ToListAsync();

                giaVeTheoSuKien = bangGia.ToDictionary(
                    x => x.SuKienId,
                    x => x.GiaThapNhat);
            }

            // Truyền dữ liệu cho View.
            ViewBag.GiaVeTheoSuKien = giaVeTheoSuKien;
            ViewBag.GoiYSuKien = goiYSuKien;
            ViewBag.SuKienSapDienRa = suKienSapDienRa;

            // Giữ Model cũ để không thay đổi Carousel và dải cuộn phim.
            return View(suKienNoiBat);
        }
    }
}
