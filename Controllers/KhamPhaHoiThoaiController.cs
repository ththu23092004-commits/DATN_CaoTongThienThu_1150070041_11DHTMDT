using System;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using VeSuKienWeb.Data;
using VeSuKienWeb.Models.KhamPhaSuKien;
using VeSuKienWeb.Services;

namespace VeSuKienWeb.Controllers
{
    [Route("kham-pha-hoi-thoai")]
    public class KhamPhaHoiThoaiController : Controller
    {
        private readonly NguCanhSuKien _db;
        private readonly AiIntentService _ai;
        private readonly KhamPhaSuKienService _timKiem;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public KhamPhaHoiThoaiController(
            NguCanhSuKien db,
            AiIntentService ai,
            KhamPhaSuKienService timKiem)
        {
            _db = db;
            _ai = ai;
            _timKiem = timKiem;
        }

        [HttpPost("hoi")]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("kham-pha")]
        public async Task<IActionResult> Hoi(
            [FromForm] string? noiDung,
            [FromForm] string? nguCanhJson,
            [FromForm] string? hoiThoaiId,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(noiDung) ||
                noiDung.Trim().Length > 500)
            {
                return BadRequest(new
                {
                    thanhCong = false,
                    thongBao = "Vui lòng nhập câu hỏi từ 1 đến 500 ký tự."
                });
            }

            noiDung = noiDung.Trim();

            int? nguoiDungId = LayNguoiDungIdKhachHang();

            KhamPhaHoiThoai? hoiThoai = null;
            NhuCauKhamPha? nguCanh = null;

            if (nguoiDungId.HasValue)
            {
                if (!string.IsNullOrWhiteSpace(hoiThoaiId))
                {
                    if (!long.TryParse(hoiThoaiId, out long id) || id <= 0)
                    {
                        return BadRequest(new
                        {
                            thanhCong = false,
                            thongBao = "Mã cuộc trò chuyện không hợp lệ."
                        });
                    }

                    try
                    {
                        hoiThoai = await _db.KhamPhaHoiThoai
                            .FirstOrDefaultAsync(
                                x => x.HoiThoaiId == id &&
                                     x.NguoiDungId == nguoiDungId.Value,
                                cancellationToken);
                    }
                    catch (OperationCanceledException)
                        when (cancellationToken.IsCancellationRequested)
                    {
                        return new EmptyResult();
                    }
                    catch (Exception)
                    {
                        return StatusCode(500, new
                        {
                            thanhCong = false,
                            thongBao = "Chưa thể mở cuộc trò chuyện lúc này."
                        });
                    }

                    if (hoiThoai == null)
                    {
                        return NotFound(new
                        {
                            thanhCong = false,
                            thongBao = "Không tìm thấy cuộc trò chuyện hoặc bạn không có quyền truy cập."
                        });
                    }

                    if (!string.IsNullOrWhiteSpace(hoiThoai.NguCanhJson))
                    {
                        try
                        {
                            nguCanh = JsonSerializer.Deserialize<NhuCauKhamPha>(
                                hoiThoai.NguCanhJson,
                                JsonOptions);
                        }
                        catch (JsonException)
                        {
                            nguCanh = null;
                        }
                    }
                }
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(hoiThoaiId))
                {
                    return BadRequest(new
                    {
                        thanhCong = false,
                        thongBao = "Bạn cần đăng nhập để mở lịch sử cuộc trò chuyện."
                    });
                }

                if (!string.IsNullOrWhiteSpace(nguCanhJson))
                {
                    if (nguCanhJson.Length > 3000)
                    {
                        return BadRequest(new
                        {
                            thanhCong = false,
                            thongBao = "Ngữ cảnh hội thoại quá dài."
                        });
                    }

                    try
                    {
                        nguCanh = JsonSerializer.Deserialize<NhuCauKhamPha>(
                            nguCanhJson,
                            JsonOptions);
                    }
                    catch (JsonException)
                    {
                        return BadRequest(new
                        {
                            thanhCong = false,
                            thongBao = "Dữ liệu hội thoại không hợp lệ."
                        });
                    }
                }
            }

            try
            {
                var nhuCau = await _ai.PhanTichAsync(
                    noiDung,
                    nguCanh,
                    cancellationToken);

                var ketQua = await _timKiem.TimKiemAsync(
                    nhuCau,
                    nguoiDungId);

                var danhSachSuKien = ketQua.DanhSach.Select(x => new
                {
                    x.SuKienId,
                    x.TenSuKien,
                    x.AnhSuKien,
                    x.MoTaChiTiet,
                    x.ChuDe,
                    x.ThoiGianBatDau,
                    x.ThoiGianKetThuc,
                    x.TenDiaDiem,
                    x.DiaChi,
                    x.DiaDiem,
                    x.GiaVeThapNhat,
                    x.LyDoPhuHop,
                    x.LaGoiYMoRong,
                    x.DieuKienChuaKhop
                }).ToList();

                if (nguoiDungId.HasValue)
                {
                    await using var transaction =
                        await _db.Database.BeginTransactionAsync(
                            cancellationToken);

                    DateTime thoiGianHienTai = DateTime.Now;

                    if (hoiThoai == null)
                    {
                        hoiThoai = new KhamPhaHoiThoai
                        {
                            NguoiDungId = nguoiDungId.Value,
                            TieuDe = noiDung.Length > 200
                                ? noiDung.Substring(0, 200)
                                : noiDung,
                            TrangThai = "DangMo",
                            NgayTao = thoiGianHienTai,
                            NgayCapNhat = thoiGianHienTai
                        };

                        _db.KhamPhaHoiThoai.Add(hoiThoai);

                        await _db.SaveChangesAsync(cancellationToken);
                    }

                    var duLieuTraVe = new
                    {
                        thanhCong = true,
                        hoiThoaiId = hoiThoai.HoiThoaiId.ToString(),
                        caNhanHoa = true,
                        thongBao = ketQua.ThongBao,
                        nhuCau = new
                        {
                            nhuCau.ChuDe,
                            nhuCau.DiaDiem,
                            nhuCau.NganSachToiDa,
                            nhuCau.TuNgay,
                            nhuCau.DenNgay,
                            nhuCau.HinhThuc,
                            nhuCau.TuKhoa,
                            nhuCau.CauHoiBoSung
                        },
                        suKien = danhSachSuKien
                    };

                    string duLieuKetQuaJson = JsonSerializer.Serialize(
                        duLieuTraVe,
                        JsonOptions);

                    DateTime thoiGianTinNhan = DateTime.Now;

                    hoiThoai.NguCanhJson = JsonSerializer.Serialize(
                        nhuCau,
                        JsonOptions);

                    hoiThoai.NgayCapNhat = thoiGianTinNhan;

                    _db.KhamPhaTinNhan.Add(new KhamPhaTinNhan
                    {
                        HoiThoaiId = hoiThoai.HoiThoaiId,
                        VaiTro = "user",
                        NoiDung = noiDung,
                        NgayGui = thoiGianTinNhan
                    });

                    _db.KhamPhaTinNhan.Add(new KhamPhaTinNhan
                    {
                        HoiThoaiId = hoiThoai.HoiThoaiId,
                        VaiTro = "assistant",
                        NoiDung = string.IsNullOrWhiteSpace(ketQua.ThongBao)
                            ? "Mình đã hoàn tất tìm kiếm sự kiện."
                            : ketQua.ThongBao,
                        DuLieuKetQuaJson = duLieuKetQuaJson,
                        NgayGui = DateTime.Now
                    });

                    await _db.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);

                    return Content(
                        duLieuKetQuaJson,
                        "application/json; charset=utf-8");
                }

                var ketQuaKhachChuaDangNhap = new
                {
                    thanhCong = true,
                    hoiThoaiId = (string?)null,
                    caNhanHoa = false,
                    thongBao = ketQua.ThongBao,
                    nhuCau = new
                    {
                        nhuCau.ChuDe,
                        nhuCau.DiaDiem,
                        nhuCau.NganSachToiDa,
                        nhuCau.TuNgay,
                        nhuCau.DenNgay,
                        nhuCau.HinhThuc,
                        nhuCau.TuKhoa,
                        nhuCau.CauHoiBoSung
                    },
                    suKien = danhSachSuKien
                };

                return Content(
                    JsonSerializer.Serialize(
                        ketQuaKhachChuaDangNhap,
                        JsonOptions),
                    "application/json; charset=utf-8");
            }
            catch (HttpRequestException)
            {
                return StatusCode(503, new
                {
                    thanhCong = false,
                    thongBao = "Dịch vụ phân tích nhu cầu đang gián đoạn. Vui lòng thử lại."
                });
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                return new EmptyResult();
            }
            catch (Exception)
            {
                return StatusCode(500, new
                {
                    thanhCong = false,
                    thongBao = "Chưa thể tìm sự kiện lúc này. Vui lòng thử lại sau."
                });
            }
        }

        [HttpGet("lich-su")]
        [Authorize(Roles = "KhachHang")]
        [ResponseCache(
            NoStore = true,
            Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> LayLichSu(
            CancellationToken cancellationToken)
        {
            int? nguoiDungId = LayNguoiDungIdKhachHang();

            if (!nguoiDungId.HasValue)
            {
                return Unauthorized(new
                {
                    thanhCong = false,
                    thongBao = "Vui lòng đăng nhập bằng tài khoản khách hàng."
                });
            }

            var danhSach = await _db.KhamPhaHoiThoai
                .AsNoTracking()
                .Where(x => x.NguoiDungId == nguoiDungId.Value)
                .OrderByDescending(x => x.NgayCapNhat)
                .ThenByDescending(x => x.HoiThoaiId)
                .Select(x => new
                {
                    x.HoiThoaiId,
                    x.TieuDe,
                    x.NgayTao,
                    x.NgayCapNhat,
                    SoTinNhan = x.TinNhans.Count(),
                    TinNhanCuoi = x.TinNhans
                        .OrderByDescending(t => t.NgayGui)
                        .ThenByDescending(t => t.TinNhanId)
                        .Select(t => t.NoiDung)
                        .FirstOrDefault()
                })
                .ToListAsync(cancellationToken);

            return Ok(new
            {
                thanhCong = true,
                danhSach = danhSach.Select(x => new
                {
                    hoiThoaiId = x.HoiThoaiId.ToString(),
                    tieuDe = x.TieuDe,
                    ngayTao = x.NgayTao,
                    ngayCapNhat = x.NgayCapNhat,
                    soTinNhan = x.SoTinNhan,
                    tinNhanCuoi = x.TinNhanCuoi
                })
            });
        }

        [HttpGet("lich-su/{hoiThoaiId:long}")]
        [Authorize(Roles = "KhachHang")]
        [ResponseCache(
            NoStore = true,
            Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> ChiTietLichSu(
            long hoiThoaiId,
            CancellationToken cancellationToken)
        {
            int? nguoiDungId = LayNguoiDungIdKhachHang();

            if (!nguoiDungId.HasValue)
            {
                return Unauthorized(new
                {
                    thanhCong = false,
                    thongBao = "Vui lòng đăng nhập bằng tài khoản khách hàng."
                });
            }

            var hoiThoai = await _db.KhamPhaHoiThoai
                .AsNoTracking()
                .Include(x => x.TinNhans)
                .FirstOrDefaultAsync(
                    x => x.HoiThoaiId == hoiThoaiId &&
                         x.NguoiDungId == nguoiDungId.Value,
                    cancellationToken);

            if (hoiThoai == null)
            {
                return NotFound(new
                {
                    thanhCong = false,
                    thongBao = "Không tìm thấy cuộc trò chuyện hoặc bạn không có quyền truy cập."
                });
            }

            return Ok(new
            {
                thanhCong = true,
                hoiThoaiId = hoiThoai.HoiThoaiId.ToString(),
                tieuDe = hoiThoai.TieuDe,
                nguCanhJson = hoiThoai.NguCanhJson,
                ngayTao = hoiThoai.NgayTao,
                ngayCapNhat = hoiThoai.NgayCapNhat,
                tinNhans = hoiThoai.TinNhans
                    .OrderBy(x => x.NgayGui)
                    .ThenBy(x => x.TinNhanId)
                    .Select(x => new
                    {
                        x.TinNhanId,
                        x.VaiTro,
                        x.NoiDung,
                        x.DuLieuKetQuaJson,
                        x.NgayGui
                    })
            });
        }

        private int? LayNguoiDungIdKhachHang()
        {
            if (User?.Identity?.IsAuthenticated != true ||
                !User.IsInRole("KhachHang"))
            {
                return null;
            }

            string? idDangNhap = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (int.TryParse(idDangNhap, out int id) && id > 0)
            {
                return id;
            }

            return null;
        }
    }
}