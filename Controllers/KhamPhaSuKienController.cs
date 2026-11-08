
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VeSuKienWeb.Services;

namespace VeSuKienWeb.Controllers
{
    [Authorize(Roles = "Admin")]
    public class KhamPhaSuKienController : Controller
    {
        private readonly AiIntentService _aiIntentService;
        private readonly KhamPhaSuKienService _khamPhaService;

        public KhamPhaSuKienController(
            AiIntentService aiIntentService,
            KhamPhaSuKienService khamPhaService)
        {
            _aiIntentService = aiIntentService;
            _khamPhaService = khamPhaService;
        }

        [HttpGet]
        public IActionResult KiemThu()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> KiemThu(string noiDung)
        {
            ViewBag.NoiDung = noiDung;

            if (string.IsNullOrWhiteSpace(noiDung))
            {
                ViewBag.Loi = "Vui lòng nhập nhu cầu tìm kiếm.";
                return View();
            }

            if (noiDung.Length > 500)
            {
                ViewBag.Loi = "Nội dung tối đa 500 ký tự.";
                return View();
            }

            try
            {
                // Bước 1: Gemini phân tích câu hỏi.
                var nhuCau = await _aiIntentService
                    .PhanTichAsync(noiDung);

                ViewBag.KetQua = nhuCau;

                // Bước 2: Tìm sự kiện thật từ SQL Server.
                // Giai đoạn này chưa cộng điểm cá nhân hóa.
                var top3 = await _khamPhaService
                    .TimKiemAsync(nhuCau);

                // Bước 3: Chuyển kết quả ra giao diện.
                ViewBag.Top3 = top3;
                ViewBag.ThanhCong = true;
            }
            catch (System.Net.Http.HttpRequestException ex)
            {
                ViewBag.Loi =
                    "Không gọi được Gemini API. " +
                    ex.Message;
            }
            catch (Exception)
            {
                // Không trả chi tiết lỗi máy chủ ra trình duyệt.
                ViewBag.Loi =
                    "Có lỗi khi phân tích hoặc tìm kiếm sự kiện. " +
                    "Vui lòng kiểm tra log máy chủ.";
            }

            return View();
        }
    }
}
