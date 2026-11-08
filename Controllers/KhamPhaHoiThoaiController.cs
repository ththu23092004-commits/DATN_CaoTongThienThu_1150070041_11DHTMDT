
using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using VeSuKienWeb.Models.KhamPhaSuKien;
using VeSuKienWeb.Services;
using System.Linq;
using System.Security.Claims;
using System.Net.Http;
namespace VeSuKienWeb.Controllers
{
    [AllowAnonymous]
    [Route("kham-pha-hoi-thoai")]
    public class KhamPhaHoiThoaiController : Controller
    {
        private readonly AiIntentService _ai;
        private readonly KhamPhaSuKienService _timKiem;

        public KhamPhaHoiThoaiController(
            AiIntentService ai,
            KhamPhaSuKienService timKiem)
        {
            _ai = ai;
            _timKiem = timKiem;
        }

        [HttpPost("hoi")]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("kham-pha")]
        public async Task<IActionResult> Hoi(
            [FromForm] string? noiDung,
            [FromForm] string? nguCanhJson)
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

            NhuCauKhamPha? nguCanh = null;

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
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });
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

            try
            {
                // Gemini xử lý câu hỏi và ngữ cảnh trước đó.
                var nhuCau = await _ai.PhanTichAsync(
                    noiDung.Trim(), nguCanh,
                    HttpContext.RequestAborted);

                // Chỉ tìm sự kiện thật trong SQL Server.
                // Hiện chưa đưa dữ liệu cá nhân của khách
                // vào hội thoại khi chưa hoàn thiện kiểm thử.
                
 // Chỉ lấy ID từ tài khoản khách hàng đã đăng nhập.
 // Không nhận ID do JavaScript gửi lên.
 int? nguoiDungId = null;

 if (User?.Identity?.IsAuthenticated == true &&
     User.IsInRole("KhachHang"))
 {
     string? idDangNhap = User.FindFirstValue(
         ClaimTypes.NameIdentifier);

     if (int.TryParse(idDangNhap, out int id) && id > 0)
     {
         nguoiDungId = id;
     }
 }

 // Có đăng nhập: tìm kiếm + Cosine cá nhân hóa.
 // Khách chưa đăng nhập: tìm kiếm theo nhu cầu.
 var ketQua = await _timKiem.TimKiemAsync(
     nhuCau,
     nguoiDungId
 );


                return Ok(new
                {
                    thanhCong = true,
                    caNhanHoa = nguoiDungId.HasValue,
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
                    suKien = ketQua.DanhSach.Select(x => new
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
                    }).ToList()
                });
            }
            catch (HttpRequestException)
            {
                return StatusCode(503, new
                {
                    thanhCong = false,
                    thongBao = "Dịch vụ phân tích nhu cầu đang gián đoạn. Vui lòng thử lại."
                });
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
    }
}

