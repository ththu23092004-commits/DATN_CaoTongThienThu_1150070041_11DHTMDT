
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VeSuKienWeb.Data;
using VeSuKienWeb.Models;
using VeSuKienWeb.Models.KhaoSatSoThich;

namespace VeSuKienWeb.Controllers
{
    [Authorize(Roles = "KhachHang")]
    public class KhaoSatSoThichController : Controller
    {
        private readonly NguCanhSuKien _db;

        public KhaoSatSoThichController(NguCanhSuKien db)
        {
            _db = db;
        }

        private int GetCurrentUserId()
        {
            return int.Parse(
                User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            int userId = GetCurrentUserId();

            var chuDes = await _db.ChuDeSuKien
                .Where(x => x.TrangThai)
                .OrderBy(x => x.TenChuDe)
                .ToListAsync();

            var hoSo = await _db.HoSoSoThich
                .FirstOrDefaultAsync(x => x.NguoiDungId == userId);

            var chuDeDaChon = await _db.NguoiDungSoThich
                .Where(x => x.NguoiDungId == userId)
                .Select(x => x.ChuDeId)
                .ToListAsync();

            var model = new KhaoSatSoThichVM
            {
                ChuDeIds = chuDeDaChon,
                DiaDiemUuTien = hoSo?.DiaDiemUuTien,
                HinhThucUuTien =
                    hoSo?.HinhThucUuTien ?? "KhongGioiHan"
            };

            ViewBag.ChuDes = chuDes;

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(KhaoSatSoThichVM model)
        {
            int userId = GetCurrentUserId();

            model.ChuDeIds = (model.ChuDeIds ?? new List<int>())
                .Distinct()
                .ToList();

            var chuDesHopLe = await _db.ChuDeSuKien
                .Where(x =>
                    model.ChuDeIds.Contains(x.ChuDeId) &&
                    x.TrangThai)
                .Select(x => x.ChuDeId)
                .ToListAsync();

            if (chuDesHopLe.Count != model.ChuDeIds.Count ||
                model.ChuDeIds.Count == 0)
            {
                ModelState.AddModelError(
                    nameof(model.ChuDeIds),
                    "Vui lòng chọn ít nhất một chủ đề hợp lệ.");
            }

            var hinhThucHopLe = new[]
            {
                "KhongGioiHan",
                "TrucTiep",
                "TrucTuyen",
                "Hybrid"
            };

            if (!hinhThucHopLe.Contains(model.HinhThucUuTien))
            {
                ModelState.AddModelError(
                    nameof(model.HinhThucUuTien),
                    "Hình thức tổ chức không hợp lệ.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.ChuDes = await _db.ChuDeSuKien
                    .Where(x => x.TrangThai)
                    .OrderBy(x => x.TenChuDe)
                    .ToListAsync();

                return View(model);
            }

            var hoSo = await _db.HoSoSoThich
                .FirstOrDefaultAsync(x => x.NguoiDungId == userId);

            if (hoSo == null)
            {
                hoSo = new HoSoSoThich
                {
                    NguoiDungId = userId
                };

                _db.HoSoSoThich.Add(hoSo);
            }

            hoSo.DiaDiemUuTien =
                string.IsNullOrWhiteSpace(model.DiaDiemUuTien)
                    ? null
                    : model.DiaDiemUuTien.Trim();

            hoSo.HinhThucUuTien = model.HinhThucUuTien;
            hoSo.DaHoanThanhKhaoSat = true;
            hoSo.NgayCapNhat = DateTime.Now;

            var soThichCu = await _db.NguoiDungSoThich
                .Where(x => x.NguoiDungId == userId)
                .ToListAsync();

            _db.NguoiDungSoThich.RemoveRange(soThichCu);

            foreach (var chuDeId in model.ChuDeIds)
            {
                _db.NguoiDungSoThich.Add(
                    new NguoiDungSoThich
                    {
                        NguoiDungId = userId,
                        ChuDeId = chuDeId,
                        NgayChon = DateTime.Now
                    });
            }

            await _db.SaveChangesAsync();

            TempData["ThongBao"] =
                "Đã lưu sở thích sự kiện thành công!";

            return RedirectToAction("Index", "KhachHang");
        }
    }
}

