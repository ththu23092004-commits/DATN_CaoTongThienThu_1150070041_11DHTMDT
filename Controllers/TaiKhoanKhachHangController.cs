
using System;
using System.Collections.Generic;
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
    public class ChuDeTaiKhoanVM
    {
        public int ChuDeId { get; set; }
        public string TenChuDe { get; set; } = "";
        public bool DaChon { get; set; }
    }

    public class TaiKhoanCaNhanVM
    {
        public int NguoiDungId { get; set; }
        public string HoTen { get; set; } = "";
        public string Email { get; set; } = "";
        public string DienThoai { get; set; } = "";
        public DateTime NgayTao { get; set; }

        public List<ChuDeTaiKhoanVM> ChuDes { get; set; } = new();

        public int SoVe { get; set; }
        public int SoSuKienYeuThich { get; set; }
    }

    [Authorize(Roles = "KhachHang")]
    [Route("tai-khoan-ca-nhan")]
    public class TaiKhoanKhachHangController : Controller
    {
        private readonly NguCanhSuKien _db;

        public TaiKhoanKhachHangController(NguCanhSuKien db)
        {
            _db = db;
        }

        private int LayNguoiDungId()
        {
            string? value = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (!int.TryParse(value, out int id))
            {
                throw new UnauthorizedAccessException(
                    "Không xác định được tài khoản.");
            }

            return id;
        }

        [HttpGet("")]
        [HttpGet("ho-so")]
        public async Task<IActionResult> HoSo()
        {
            int id = LayNguoiDungId();

            var user = await _db.NguoiDung
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.NguoiDungId == id);

            if (user == null)
                return NotFound();

            var chuDeIds = await _db.NguoiDungSoThich
                .AsNoTracking()
                .Where(x => x.NguoiDungId == id)
                .Select(x => x.ChuDeId)
                .ToListAsync();

            var selected = chuDeIds.ToHashSet();

            var chuDes = await _db.ChuDeSuKien
                .AsNoTracking()
                .Where(x => x.TrangThai)
                .OrderBy(x => x.TenChuDe)
                .Select(x => new
                {
                    x.ChuDeId,
                    x.TenChuDe
                })
                .ToListAsync();

            var vm = new TaiKhoanCaNhanVM
            {
                NguoiDungId = user.NguoiDungId,
                HoTen = user.HoTen,
                Email = user.Email,
                DienThoai = user.DienThoai ?? "",
                NgayTao = user.NgayTao,

                ChuDes = chuDes.Select(x => new ChuDeTaiKhoanVM
                {
                    ChuDeId = x.ChuDeId,
                    TenChuDe = x.TenChuDe,
                    DaChon = selected.Contains(x.ChuDeId)
                }).ToList(),

                SoVe = await _db.Ve.CountAsync(x =>
                    x.NguoiDungId == id &&
                    x.TrangThai != "Huy"),

                SoSuKienYeuThich =
                    await _db.SuKienYeuThich.CountAsync(x =>
                        x.NguoiDungId == id)
            };

            return View("~/Views/KhachHang/HoSo.cshtml", vm);
        }

        [HttpPost("luu-ho-so")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LuuHoSo(
            string? hoTen,
            string? dienThoai)
        {
            int id = LayNguoiDungId();

            var user = await _db.NguoiDung
                .FirstOrDefaultAsync(x => x.NguoiDungId == id);

            if (user == null)
                return NotFound();

            hoTen = (hoTen ?? "").Trim();
            dienThoai = (dienThoai ?? "").Trim();

            if (hoTen.Length < 2 || hoTen.Length > 100)
            {
                TempData["HoSoLoi"] =
                    "Họ tên phải có từ 2 đến 100 ký tự.";

                return RedirectToAction(
                    nameof(HoSo), new { tab = "profile" });
            }

            if (!string.IsNullOrEmpty(dienThoai) &&
                (!System.Text.RegularExpressions.Regex.IsMatch(
                    dienThoai, @"^0\d{9}$")))
            {
                TempData["HoSoLoi"] =
                    "Số điện thoại phải gồm 10 chữ số và bắt đầu bằng 0.";

                return RedirectToAction(
                    nameof(HoSo), new { tab = "profile" });
            }

            user.HoTen = hoTen;
            user.DienThoai = string.IsNullOrEmpty(dienThoai)
                ? null
                : dienThoai;

            await _db.SaveChangesAsync();

            TempData["HoSoThanhCong"] =
                "Cập nhật thông tin thành công.";

            return RedirectToAction(
                nameof(HoSo), new { tab = "profile" });
        }

        [HttpPost("luu-so-thich")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LuuSoThich(
            List<int>? chuDeIds)
        {
            int userId = LayNguoiDungId();

            bool userExists = await _db.NguoiDung
                .AnyAsync(x => x.NguoiDungId == userId);

            if (!userExists)
                return NotFound();

            var inputIds = (chuDeIds ?? new List<int>())
                .Where(x => x > 0)
                .Distinct()
                .ToList();

            var validIds = await _db.ChuDeSuKien
                .AsNoTracking()
                .Where(x =>
                    x.TrangThai &&
                    inputIds.Contains(x.ChuDeId))
                .Select(x => x.ChuDeId)
                .ToListAsync();

            if (inputIds.Count != validIds.Count)
            {
                TempData["SoThichLoi"] =
                    "Có chủ đề không còn hợp lệ. Vui lòng chọn lại.";

                return RedirectToAction(
                    nameof(HoSo), new { tab = "preferences" });
            }

            await using var transaction =
                await _db.Database.BeginTransactionAsync();

            var soThichCu = await _db.NguoiDungSoThich
                .Where(x => x.NguoiDungId == userId)
                .ToListAsync();

            var oldIds = soThichCu
                .Select(x => x.ChuDeId)
                .ToHashSet();

            var newIds = validIds.ToHashSet();

            var canDelete = soThichCu
                .Where(x => !newIds.Contains(x.ChuDeId))
                .ToList();

            _db.NguoiDungSoThich.RemoveRange(canDelete);

            foreach (int chuDeId in validIds)
            {
                if (oldIds.Contains(chuDeId))
                    continue;

                _db.NguoiDungSoThich.Add(
                    new NguoiDungSoThich
                    {
                        NguoiDungId = userId,
                        ChuDeId = chuDeId,
                        NgayChon = DateTime.Now
                    });
            }

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            TempData["SoThichThanhCong"] =
                "Đã lưu sở thích sự kiện.";

            return RedirectToAction(
                nameof(HoSo), new { tab = "preferences" });
        }
    }
}

