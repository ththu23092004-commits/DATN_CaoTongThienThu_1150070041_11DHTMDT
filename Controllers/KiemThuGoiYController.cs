
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VeSuKienWeb.Services;

namespace VeSuKienWeb.Controllers
{
    [Authorize(Roles = "Admin")]
    public class KiemThuGoiYController : Controller
    {
        private readonly GoiYSuKienService _goiYService;

        public KiemThuGoiYController(
            GoiYSuKienService goiYService)
        {
            _goiYService = goiYService;
        }

        [HttpGet("/kiem-thu-goi-y")]
        public async Task<IActionResult> Index(
            int nguoiDungId = 5)
        {
            if (nguoiDungId <= 0)
                return BadRequest("Mã người dùng không hợp lệ.");

            var baoCao = await _goiYService
                .LayBaoCaoAsync(nguoiDungId);

            return View(baoCao);
        }
    }
}

