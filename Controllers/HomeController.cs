using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VeSuKienWeb.Data;

namespace VeSuKienWeb.Controllers
{
    public class HomeController : Controller
    {
        private readonly NguCanhSuKien _db;

        public HomeController(NguCanhSuKien db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            var now = DateTime.Now;

            // ⭐ Lấy tối đa 6 sự kiện đã được duyệt, còn/thời gian sắp diễn ra
            var suKienNoiBat = await _db.SuKien
                .Include(s => s.LoaiSuKien)
                .Include(s => s.ToChucSuKien)
                .Include(s => s.DiaDiem)
                .Where(s => s.TrangThaiDuyet == "DaDuyet"
                            && s.ThoiGianKetThuc > now)
                .OrderBy(s => s.ThoiGianBatDau)
                .Take(6)
                .ToListAsync();

            return View(suKienNoiBat); // ✅ Model là List<SuKien>
        }
    }
}
