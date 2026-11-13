
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VeSuKienWeb.Data;

namespace VeSuKienWeb.Controllers
{
    [Authorize(Roles = "ToChuc")]
    [Route("api/btc-sidebar")]
    public class BtcSidebarThongBaoController : Controller
    {
        private readonly NguCanhSuKien _db;

        public BtcSidebarThongBaoController(NguCanhSuKien db)
        {
            _db = db;
        }

        [HttpGet("thong-bao")]
        public async Task<IActionResult> LayThongBao()
        {
            var userIdValue = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdValue, out int userId))
            {
                return Unauthorized();
            }

            var toChucId = await _db.ToChucSuKien
                .AsNoTracking()
                .Where(t => t.NguoiDungId == userId)
                .Select(t => (int?)t.ToChucId)
                .FirstOrDefaultAsync();

            if (!toChucId.HasValue)
            {
                return Json(new
                {
                    nguoiDungId = userId,
                    suKien = Array.Empty<object>(),
                    yeuCau = Array.Empty<int>()
                });
            }

            var suKien = await _db.SuKien
                .AsNoTracking()
                .Where(s =>
                    s.ToChucId == toChucId.Value &&
                    (
                        s.TrangThaiDuyet == "DaDuyet" ||
                        s.TrangThaiDuyet == "TuChoi"
                    ))
                .Select(s => new
                {
                    id = s.SuKienId,
                    trangThai = s.TrangThaiDuyet
                })
                .ToListAsync();

            var yeuCau = await (
                from yc in _db.YeuCauHoanHuyVe.AsNoTracking()
                join ve in _db.Ve.AsNoTracking()
                    on yc.VeId equals ve.VeId
                join sk in _db.SuKien.AsNoTracking()
                    on ve.SuKienId equals sk.SuKienId
                where sk.ToChucId == toChucId.Value
                      && yc.HinhThuc == "HuyVe"
                      && yc.TrangThai == "ChoDuyet"
                select yc.YeuCauId
            )
            .Distinct()
            .ToListAsync();

            Response.Headers.CacheControl = "no-store";

            return Json(new
            {
                nguoiDungId = userId,
                suKien,
                yeuCau
            });
        }
    }
}

