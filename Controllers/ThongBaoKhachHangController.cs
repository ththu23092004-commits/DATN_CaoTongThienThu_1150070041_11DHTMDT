
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
    [Authorize(Roles = "KhachHang")]
    [Route("api/thong-bao-khach-hang")]
    public class ThongBaoKhachHangController : Controller
    {
        private readonly NguCanhSuKien _db;

        public ThongBaoKhachHangController(NguCanhSuKien db)
        {
            _db = db;
        }

        private int LayNguoiDungId()
        {
            var value = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (!int.TryParse(value, out var id))
                throw new UnauthorizedAccessException();

            return id;
        }

        // Tự tìm tên khóa chính từ EF Core.
        // Không cần đoán ThongBaoId hay Id.
        private string LayTenKhoaChinh()
        {
            var entity = _db.Model.FindEntityType(
                typeof(ThongBao));

            var key = entity?.FindPrimaryKey();

            if (key == null || key.Properties.Count != 1)
            {
                throw new InvalidOperationException(
                    "Bảng ThongBao cần một khóa chính."
                );
            }

            return key.Properties[0].Name;
        }

        // Tạo lời nhắc một lần cho mỗi sự kiện/mốc giờ.
        private async Task TaoLoiNhacAsync(int nguoiDungId)
        {
            var now = DateTime.Now;
            var trong24Gio = now.AddHours(24);

            // Chỉ xét vé đã thanh toán/chưa check-in,
            // sự kiện chưa kết thúc.
            var veHopLe = await _db.Ve
                .AsNoTracking()
                .Where(v =>
                    v.NguoiDungId == nguoiDungId &&
                    (v.TrangThai == "DaThanhToan" ||
                     v.TrangThai == "DaCheckIn") &&
                    v.SuKien.ThoiGianBatDau > now &&
                    v.SuKien.ThoiGianBatDau <= trong24Gio &&
                    v.SuKien.TrangThaiDuyet == "DaDuyet")
                .Select(v => new
                {
                    v.SuKienId,
                    v.SuKien.TenSuKien,
                    v.SuKien.ThoiGianBatDau
                })
                .Distinct()
                .ToListAsync();

            // Chỉ tạo lời nhắc cho vé chưa check-in.
            var suKienIds = await _db.Ve
                .AsNoTracking()
                .Where(v =>
                    v.NguoiDungId == nguoiDungId &&
                    v.TrangThai == "DaThanhToan" &&
                    v.SuKien.ThoiGianBatDau > now &&
                    v.SuKien.ThoiGianBatDau <= trong24Gio &&
                    v.SuKien.TrangThaiDuyet == "DaDuyet")
                .Select(v => v.SuKienId)
                .Distinct()
                .ToListAsync();

            // Đọc tất cả thông báo có tiền tố của chức năng này,
            // bao gồm thông báo đã đọc để tránh tạo lại.
            var noiDungDaTao = await _db.ThongBao
                .AsNoTracking()
                .Where(t =>
                    t.NguoiDungId == nguoiDungId &&
                    t.NoiDung.StartsWith("[NHAC-SK:"))
                .Select(t => t.NoiDung)
                .ToListAsync();

            var keys = new HashSet<string>(
                noiDungDaTao
                    .Select(x =>
                    {
                        var end = x.IndexOf(']');
                        return end >= 0
                            ? x.Substring(0, end + 1)
                            : x;
                    }),
                StringComparer.Ordinal
            );

            foreach (var sk in veHopLe)
            {
                if (!suKienIds.Contains(sk.SuKienId))
                    continue;

                var conLai =
                    sk.ThoiGianBatDau - now;

                // Nếu chỉ còn <=2 giờ, chỉ tạo nhắc 2 giờ.
                // Nếu trên 2 giờ, nhắc trong vòng 24 giờ.
                var moc = conLai.TotalHours <= 2
                    ? "2H"
                    : "24H";

                var key =
                    $"[NHAC-SK:{sk.SuKienId}:{moc}]";

                if (keys.Contains(key))
                    continue;

                string noiDung = moc == "2H"
                    ? $"Sự kiện \"{sk.TenSuKien}\" sắp bắt đầu. " +
                      $"Giờ diễn ra: {sk.ThoiGianBatDau:HH:mm dd/MM}."
                    : $"Sự kiện \"{sk.TenSuKien}\" diễn ra trong 24 giờ tới. " +
                      $"{sk.ThoiGianBatDau:HH:mm dd/MM}.";

                _db.ThongBao.Add(new ThongBao
                {
                    NguoiDungId = nguoiDungId,
                    NoiDung = key + " " + noiDung,
                    NgayGui = now,
                    DaDoc = false
                });

                keys.Add(key);
            }

            // Nhắc đơn chờ thanh toán trong 30 phút đầu.
            // Không tạo thông báo cho đơn Huy/ThanhCong.
            var donCho = await _db.DonHang
                .AsNoTracking()
                .Where(d =>
                    d.NguoiDungId == nguoiDungId &&
                    d.TrangThai == "DangXuLy" &&
                    d.NgayDat >= now.AddMinutes(-30) &&
                    d.NgayDat <= now)
                .Select(d => new
                {
                    d.DonHangId,
                    d.NgayDat
                })
                .ToListAsync();

            var daNhacThanhToan = await _db.ThongBao
                .AsNoTracking()
                .Where(t =>
                    t.NguoiDungId == nguoiDungId &&
                    t.NoiDung.StartsWith("[CHO-TT:"))
                .Select(t => t.NoiDung)
                .ToListAsync();

            var paymentKeys = daNhacThanhToan
                .Select(x =>
                {
                    var end = x.IndexOf(']');
                    return end >= 0
                        ? x.Substring(0, end + 1)
                        : x;
                })
                .ToHashSet(StringComparer.Ordinal);

            foreach (var don in donCho)
            {
                string key =
                    $"[CHO-TT:{don.DonHangId}]";

                if (paymentKeys.Contains(key))
                    continue;

                _db.ThongBao.Add(new ThongBao
                {
                    NguoiDungId = nguoiDungId,
                    NoiDung =
                        key + " Đơn #" + don.DonHangId +
                        " đang chờ thanh toán.",
                    NgayGui = now,
                    DaDoc = false
                });

                paymentKeys.Add(key);
            }

            if (_db.ChangeTracker.HasChanges())
                await _db.SaveChangesAsync();
        }

        [HttpGet("danh-sach")]
        public async Task<IActionResult> DanhSach()
        {
            int nguoiDungId = LayNguoiDungId();

            await TaoLoiNhacAsync(nguoiDungId);

            // Thông báo thực tế đã lưu trong SQL Server.
            var thongBao = await _db.ThongBao
                .AsNoTracking()
                .Where(x => x.NguoiDungId == nguoiDungId)
                .OrderByDescending(x => x.NgayGui)
                .Take(100)
                .ToListAsync();

            var tenKhoa = LayTenKhoaChinh();

            // Chỉ những đơn còn chờ thực sự mới cho
            // phép truy cập trang thanh toán.
            var donChoIds = await _db.DonHang
                .AsNoTracking()
                .Where(x =>
                    x.NguoiDungId == nguoiDungId &&
                    x.TrangThai == "DangXuLy")
                .Select(x => x.DonHangId)
                .ToListAsync();

            var donChoSet = donChoIds.ToHashSet();

            var result = new List<object>();

            foreach (var tb in thongBao)
            {
                string noiDung = tb.NoiDung ?? "";
                string loai = "chung";
                string url = Url.Action(
                    "VeCuaToi", "KhachHang")
                    ?? "/KhachHang/VeCuaToi";

                // Tiền tố là mã nội bộ, không hiển thị lên UI.
                if (noiDung.StartsWith("[NHAC-SK:"))
                {
                    loai = "suKien";

                    var end = noiDung.IndexOf(']');
                    if (end >= 0)
                    {
                        noiDung = noiDung.Substring(end + 1)
                            .Trim();
                    }
                }
                else if (noiDung.StartsWith("[CHO-TT:"))
                {
                    loai = "thanhToan";

                    var end = noiDung.IndexOf(']');

                    if (end > 8)
                    {
                        var idText = noiDung.Substring(
                            8, end - 8);

                        if (int.TryParse(idText, out int donId)
                            && donChoSet.Contains(donId))
                        {
                            url = Url.Action(
                                "ThanhToan",
                                "KhachHang",
                                new { id = donId }
                            ) ?? url;
                        }

                        noiDung = noiDung.Substring(end + 1)
                            .Trim();
                    }
                }
                else if (noiDung.Contains("hủy", StringComparison.OrdinalIgnoreCase))
                {
                    loai = "huyVe";
                }

                // Chỉ hiện thông báo thanh toán khi
                // đơn hàng vẫn có trạng thái DangXuLy.
                if (loai == "thanhToan" &&
                    url.Contains("VeCuaToi"))
                {
                    continue;
                }

                var keyValue = _db.Entry(tb)
                    .Property(tenKhoa)
                    .CurrentValue;

                int thongBaoId = Convert.ToInt32(keyValue);

                result.Add(new
                {
                    id = thongBaoId,
                    noiDung,
                    loai,
                    ngayGui = tb.NgayGui,
                    daDoc = tb.DaDoc,
                    url
                });
            }

            // Đếm toàn bộ chưa đọc đang hiển thị,
            // không chỉ 5 thông báo đầu.
            int chuaDoc = result.Count(x =>
            {
                var prop = x.GetType().GetProperty("daDoc");
                return prop?.GetValue(x) is bool b && !b;
            });

            return Ok(new
            {
                soChuaDoc = chuaDoc,
                danhSach = result.Take(30)
            });
        }

        [HttpPost("da-doc/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DanhDauDaDoc(int id)
        {
            int nguoiDungId = LayNguoiDungId();
            string keyName = LayTenKhoaChinh();

            var thongBao = await _db.ThongBao
                .FirstOrDefaultAsync(x =>
                    x.NguoiDungId == nguoiDungId &&
                    EF.Property<int>(x, keyName) == id);

            if (thongBao == null)
                return NotFound();

            if (!thongBao.DaDoc)
            {
                thongBao.DaDoc = true;
                await _db.SaveChangesAsync();
            }

            return Ok(new { success = true });
        }

        [HttpPost("da-doc-tat-ca")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DanhDauTatCa()
        {
            int nguoiDungId = LayNguoiDungId();

            var unread = await _db.ThongBao
                .Where(x =>
                    x.NguoiDungId == nguoiDungId &&
                    !x.DaDoc)
                .ToListAsync();

            foreach (var item in unread)
                item.DaDoc = true;

            if (unread.Count > 0)
                await _db.SaveChangesAsync();

            return Ok(new { success = true });
        }
    }
}

