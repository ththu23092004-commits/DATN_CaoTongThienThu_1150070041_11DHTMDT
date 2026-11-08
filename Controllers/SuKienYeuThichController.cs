
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VeSuKienWeb.Data;
using VeSuKienWeb.Models;

namespace VeSuKienWeb.Controllers
{
    public class SuKienYeuThichController : Controller
    {
        private readonly NguCanhSuKien _db;

        public SuKienYeuThichController(NguCanhSuKien db)
        {
            _db = db;
        }

        private int? LayNguoiDungId()
        {
            var id = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            return int.TryParse(id, out var userId)
                ? userId
                : null;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> DanhSach()
        {
            var userId = LayNguoiDungId();

            if (User.Identity?.IsAuthenticated != true ||
                !User.IsInRole("KhachHang") ||
                !userId.HasValue)
            {
                return Json(new
                {
                    authenticated = false,
                    ids = Array.Empty<int>(),
                    items = Array.Empty<object>()
                });
            }

            var favorites = await _db.SuKienYeuThich
                .AsNoTracking()
                .Where(x => x.NguoiDungId == userId.Value)
                .OrderByDescending(x => x.NgayYeuThich)
                .Select(x => new
                {
                    x.SuKienId,
                    x.SuKien!.TenSuKien,
                    x.SuKien.AnhSuKien,
                    x.SuKien.ThoiGianBatDau,
                    x.SuKien.TrangThaiDuyet,
                    x.SuKien.ThoiGianKetThuc
                })
                .ToListAsync();

            var items = favorites.Select(x => new
            {
                id = x.SuKienId,
                name = x.TenSuKien,
                image = Url.Content(
                    string.IsNullOrWhiteSpace(x.AnhSuKien)
                        ? "~/images/default-event.jpg"
                        : x.AnhSuKien),
                date = x.ThoiGianBatDau.ToString("dd/MM/yyyy HH:mm"),
                url = Url.Action(
                    "ChiTiet",
                    "KhachHang",
                    new { id = x.SuKienId }),
                available = x.TrangThaiDuyet == "DaDuyet" &&
                    x.ThoiGianKetThuc > DateTime.Now
            }).ToList();

            return Json(new
            {
                authenticated = true,
                ids = favorites.Select(x => x.SuKienId).ToArray(),
                items
            });
        }

        [HttpPost]
        [Authorize(Roles = "KhachHang")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChuyenTrangThai(
            int suKienId)
        {
            var userId = LayNguoiDungId();

            if (!userId.HasValue)
                return Unauthorized();

            var existing = await _db.SuKienYeuThich
                .FirstOrDefaultAsync(x =>
                    x.NguoiDungId == userId.Value &&
                    x.SuKienId == suKienId);

            if (existing != null)
            {
                _db.SuKienYeuThich.Remove(existing);
                await _db.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    liked = false
                });
            }

            bool exists = await _db.SuKien
                .AnyAsync(x =>
                    x.SuKienId == suKienId &&
                    x.TrangThaiDuyet == "DaDuyet" &&
                    x.ThoiGianKetThuc > DateTime.Now);

            if (!exists)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Sự kiện không còn khả dụng."
                });
            }

            _db.SuKienYeuThich.Add(new SuKienYeuThich
            {
                NguoiDungId = userId.Value,
                SuKienId = suKienId,
                NgayYeuThich = DateTime.Now
            });

            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // Có thể người dùng bấm lưu đồng thời ở
                // hai tab. Khóa chính CSDL chống lưu trùng.
                bool daLuu = await _db.SuKienYeuThich
                    .AsNoTracking()
                    .AnyAsync(x =>
                        x.NguoiDungId == userId.Value &&
                        x.SuKienId == suKienId);

                if (!daLuu) throw;
            }

            return Json(new
            {
                success = true,
                liked = true
            });
        }
    }
}

