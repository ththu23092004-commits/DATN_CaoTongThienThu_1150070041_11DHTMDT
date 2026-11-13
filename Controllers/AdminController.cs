using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VeSuKienWeb.Data;
using VeSuKienWeb.Models;
using VeSuKienWeb.Models.Admin;
using VeSuKienWeb.Helpers;
using System.Text.Json;

namespace VeSuKienWeb.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly NguCanhSuKien _db;

        public AdminController(NguCanhSuKien db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            var list = await _db.SuKien
                .Include(s => s.LoaiSuKien)
                .Include(s => s.ToChucSuKien)
                .OrderBy(s => s.TrangThaiDuyet == "ChoDuyet" ? 0 : 1)
                .ThenByDescending(s => s.NgayDang)
                .ToListAsync();

            return View(list);
        }

        // ====== ADMIN XEM CHI TIẾT SỰ KIỆN ======
        [HttpGet]
        public async Task<IActionResult> ChiTiet(int id)
        {
            // Lấy sự kiện + loại + tổ chức
            var sk = await _db.SuKien
                .Include(s => s.LoaiSuKien)
                .Include(s => s.ToChucSuKien)
                .FirstOrDefaultAsync(s => s.SuKienId == id);

            if (sk == null) return NotFound();

            var loai = sk.LoaiSuKien;
            var toChuc = sk.ToChucSuKien;



            // Địa điểm (nếu là sự kiện trực tiếp)
            var diaDiem = await _db.DiaDiem
                .FirstOrDefaultAsync(d => d.SuKienId == sk.SuKienId);



            // Thông tin trực tuyến (nếu là sự kiện online)
            var trucTuyen = await _db.SuKienTrucTuyen
                .FirstOrDefaultAsync(t => t.SuKienId == sk.SuKienId);

            // Các loại vé
            var loaiVes = await _db.LoaiVe
                .Where(l => l.SuKienId == sk.SuKienId)
                .ToListAsync();




// Lấy sơ đồ theo cùng cách Ban tổ chức đang hiển thị
var soDo = await _db.SoDoSuKien
    .AsNoTracking()
    .FirstOrDefaultAsync(x => x.SuKienId == sk.SuKienId);

ViewBag.SoDoJson = "null";

if (soDo != null && diaDiem != null)
{
    var ghe = await _db.SoDoChoNgoi
        .AsNoTracking()
        .Where(x => x.DiaDiemId == diaDiem.DiaDiemId)
        .OrderBy(x => x.Hang)
        .ThenBy(x => x.Cot)
        .Select(x => new
        {
            id = x.ChoNgoiId,
            hang = x.Hang,
            cot = x.Cot,
            x = x.ViTriX,
            y = x.ViTriY,
            trangThai = x.TrangThai,
            khu = x.KhuVucChoNgoi != null
                ? x.KhuVucChoNgoi.TenKhu
                : x.LoaiVe.TenLoai
        })
        .ToListAsync();

    var thanhPhan = await _db.ThanhPhanSoDo
        .AsNoTracking()
        .Where(x => x.SoDoId == soDo.SoDoId)
        .Select(x => new
        {
            loai = x.LoaiThanhPhan,
            ten = x.TenHienThi,
            x = x.ViTriX,
            y = x.ViTriY,
            rong = x.ChieuRong,
            cao = x.ChieuCao
        })
        .ToListAsync();

    ViewBag.SoDoJson = JsonSerializer.Serialize(new
    {
        chieuRong = soDo.ChieuRong,
        chieuCao = soDo.ChieuCao,
        ghe,
        thanhPhan
    });
}



            var vm = new SuKienChiTietVM
            {
                SuKien = sk,
                LoaiSuKien = loai,
                ToChuc = toChuc,
                DiaDiem = diaDiem,
                SuKienTrucTuyen = trucTuyen,
                LoaiVes = loaiVes
            };

            return View(vm);
        }


[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Duyet(int id)
{
    var hienTai = DateTime.Now;

    var soDongCapNhat = await _db.SuKien
        .Where(s =>
            s.SuKienId == id &&
            s.TrangThaiDuyet == "ChoDuyet" &&
            s.ThoiGianKetThuc > hienTai)
        .ExecuteUpdateAsync(setters => setters
            .SetProperty(
                s => s.TrangThaiDuyet,
                "DaDuyet"));

    if (soDongCapNhat == 0)
    {
        var suKien = await _db.SuKien
            .AsNoTracking()
            .Where(s => s.SuKienId == id)
            .Select(s => new
            {
                s.TrangThaiDuyet,
                s.ThoiGianKetThuc
            })
            .FirstOrDefaultAsync();

        if (suKien == null)
            return NotFound();

        if (suKien.TrangThaiDuyet == "An")
        {
            TempData["ThongBao"] =
                "Sự kiện đã được Ban tổ chức ẩn, không thể duyệt.";
        }
        else if (suKien.ThoiGianKetThuc <= DateTime.Now)
        {
            TempData["ThongBao"] =
                "Sự kiện đã kết thúc, không thể phê duyệt.";
        }
        else
        {
            TempData["ThongBao"] =
                "Sự kiện không còn ở trạng thái chờ duyệt.";
        }
    }
    else
    {
        TempData["ThongBao"] =
            "Đã phê duyệt sự kiện thành công.";
    }

    return RedirectToAction(nameof(Index));
}

[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> TuChoi(int id)
{
    var hienTai = DateTime.Now;

    var soDongCapNhat = await _db.SuKien
        .Where(s =>
            s.SuKienId == id &&
            s.TrangThaiDuyet == "ChoDuyet" &&
            s.ThoiGianKetThuc > hienTai)
        .ExecuteUpdateAsync(setters => setters
            .SetProperty(
                s => s.TrangThaiDuyet,
                "TuChoi"));

    if (soDongCapNhat == 0)
    {
        var suKien = await _db.SuKien
            .AsNoTracking()
            .Where(s => s.SuKienId == id)
            .Select(s => new
            {
                s.TrangThaiDuyet,
                s.ThoiGianKetThuc
            })
            .FirstOrDefaultAsync();

        if (suKien == null)
            return NotFound();

        if (suKien.TrangThaiDuyet == "An")
        {
            TempData["ThongBao"] =
                "Sự kiện đã được Ban tổ chức ẩn, không thể từ chối.";
        }
        else if (suKien.ThoiGianKetThuc <= DateTime.Now)
        {
            TempData["ThongBao"] =
                "Sự kiện đã kết thúc, không thể từ chối.";
        }
        else
        {
            TempData["ThongBao"] =
                "Sự kiện không còn ở trạng thái chờ duyệt.";
        }
    }
    else
    {
        TempData["ThongBao"] =
            "Đã từ chối sự kiện thành công.";
    }

    return RedirectToAction(nameof(Index));
}



        // ============================================================
        // ========== QUẢN LÝ YÊU CẦU HỦY / HOÀN VÉ ===================
        // ============================================================


public async Task<IActionResult> YeuCauHoanHuy()
{
    // Admin chỉ theo dõi yêu cầu hủy vé.
    var yeuCau = await _db.YeuCauHoanHuyVe
        .AsNoTracking()
        .Where(y => y.HinhThuc == "HuyVe")
        .OrderByDescending(y => y.NgayYeuCau)
        .ToListAsync();

    var veIds = yeuCau
        .Select(y => y.VeId)
        .Distinct()
        .ToList();

    // Tải vé cùng sự kiện, khách hàng, loại vé và BTC.
    var veMap = await _db.Ve
        .AsNoTracking()
        .Where(v => veIds.Contains(v.VeId))
        .Include(v => v.NguoiDung)
        .Include(v => v.LoaiVe)
        .Include(v => v.SuKien)
            .ThenInclude(s => s.ToChucSuKien)
        .ToDictionaryAsync(v => v.VeId);

    // Lấy tên BTC từ navigation ToChucSuKien.
    // Hỗ trợ các tên thuộc tính thường gặp.
    string LayTenBTC(object? toChuc)
    {
        if (toChuc == null)
            return "Chưa xác định";

        if (toChuc is NguoiDung nguoiDung)
            return string.IsNullOrWhiteSpace(nguoiDung.HoTen)
                ? "Chưa xác định"
                : nguoiDung.HoTen;

        foreach (var tenThuocTinh in new[]
        {
            "TenToChuc",
            "TenBanToChuc",
            "TenDonVi",
            "HoTen",
            "Ten"
        })
        {
            var property = toChuc.GetType()
                .GetProperty(tenThuocTinh);

            if (property?.GetValue(toChuc) is string ten &&
                !string.IsNullOrWhiteSpace(ten))
            {
                return ten;
            }
        }

        return "Chưa xác định";
    }

    // Mã khóa dùng để lọc đúng từng BTC.
    string LayMaBTC(object? toChuc)
    {
        if (toChuc == null)
            return string.Empty;

        if (toChuc is NguoiDung nguoiDung)
            return nguoiDung.NguoiDungId.ToString();

        var entry = _db.Entry(toChuc);
        var primaryKey = entry.Metadata.FindPrimaryKey();

        if (primaryKey == null)
            return string.Empty;

        return string.Join(
            "|",
            primaryKey.Properties.Select(p =>
                entry.Property(p.Name).CurrentValue?.ToString()
                ?? string.Empty
            )
        );
    }

    var danhSach = yeuCau.Select(y =>
    {
        veMap.TryGetValue(y.VeId, out var ve);

        var btc = ve?.SuKien?.ToChucSuKien;

        return new YeuCauHoanHuyAdminVM
        {
            YeuCauId = y.YeuCauId,
            VeId = y.VeId,

            MaVe = ve?.MaVe ?? string.Empty,
            TenSuKien = ve?.SuKien?.TenSuKien
                ?? "Không tìm thấy sự kiện",

            BanToChucId = LayMaBTC(btc),
            TenBanToChuc = LayTenBTC(btc),

            TenNguoiDung = ve?.NguoiDung?.HoTen
                ?? "Không tìm thấy khách hàng",

            EmailNguoiDung = ve?.NguoiDung?.Email
                ?? string.Empty,

            SoTien = ve?.LoaiVe?.GiaVe ?? 0,

            NgayYeuCau = y.NgayYeuCau,
            LyDo = y.LyDo ?? string.Empty,

            HinhThuc = y.HinhThuc ?? string.Empty,
            TrangThai = y.TrangThai ?? string.Empty
        };
    }).ToList();

    return View(danhSach);
}

    
    
        // ============================================================
        // ========== QUẢN LÝ NGƯỜI DÙNG ==============================
        // ============================================================

        [HttpGet("/Admin/NguoiDung")]
        public async Task<IActionResult> NguoiDung()
        {
            var list = await _db.NguoiDung
                .OrderByDescending(n => n.NgayTao)
                .ToListAsync();

            return View(list);
        }

        [HttpGet("/Admin/TaoBanToChuc")]
        public IActionResult TaoBanToChuc()
        {
            return View();
        }


[HttpPost("/Admin/TaoBanToChuc")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> TaoBanToChuc(NguoiDung model)
{
    var hoTen = (model.HoTen ?? "").Trim();
    var email = (model.Email ?? "").Trim();
    var matKhau = model.MatKhau ?? "";
    var dienThoai = (model.DienThoai ?? "").Trim();

    // Chỉ nhận những trường được phép tạo.
    ModelState.Clear();

    if (string.IsNullOrWhiteSpace(hoTen))
    {
        ModelState.AddModelError(
            nameof(model.HoTen),
            "Vui lòng nhập họ và tên.");
    }

    if (hoTen.Length > 150)
    {
        ModelState.AddModelError(
            nameof(model.HoTen),
            "Họ và tên không được vượt quá 150 ký tự.");
    }

    if (string.IsNullOrWhiteSpace(email) ||
        !new System.ComponentModel.DataAnnotations
            .EmailAddressAttribute().IsValid(email))
    {
        ModelState.AddModelError(
            nameof(model.Email),
            "Vui lòng nhập email hợp lệ.");
    }

    if (email.Length > 255)
    {
        ModelState.AddModelError(
            nameof(model.Email),
            "Email không được vượt quá 255 ký tự.");
    }

    if (string.IsNullOrWhiteSpace(matKhau) ||
        matKhau.Length < 6)
    {
        ModelState.AddModelError(
            nameof(model.MatKhau),
            "Mật khẩu phải có ít nhất 6 ký tự.");
    }

    if (!string.IsNullOrWhiteSpace(dienThoai) &&
        !System.Text.RegularExpressions.Regex.IsMatch(
            dienThoai,
            @"^0\d{9}$"))
    {
        ModelState.AddModelError(
            nameof(model.DienThoai),
            "Số điện thoại phải gồm 10 chữ số và bắt đầu bằng 0.");
    }

    if (!ModelState.IsValid)
        return View(model);

    var emailChuanHoa = email.ToLower();

    bool emailDaTonTai = await _db.NguoiDung
        .AnyAsync(x =>
            x.Email.ToLower() == emailChuanHoa);

    if (emailDaTonTai)
    {
        ModelState.AddModelError(
            nameof(model.Email),
            "Email này đã được sử dụng.");

        return View(model);
    }

    var taiKhoanMoi = new NguoiDung
    {
        HoTen = hoTen,
        Email = email,
        DienThoai = string.IsNullOrWhiteSpace(dienThoai)
            ? null
            : dienThoai,
        MatKhau = MatKhauHelper.BamMatKhau(matKhau),
        VaiTro = "ToChuc",
        TrangThai = "HoatDong",
        NgayTao = DateTime.Now
    };

    _db.NguoiDung.Add(taiKhoanMoi);

    await _db.SaveChangesAsync();

    TempData["ThongBao"] =
        "Đã tạo tài khoản Ban tổ chức thành công.";

    return RedirectToAction(nameof(NguoiDung));
}

[HttpGet("/Admin/SuaNguoiDung/{id}")]
public async Task<IActionResult> SuaNguoiDung(int id)
{
    var user = await _db.NguoiDung.FindAsync(id);
    if (user == null) return NotFound();

    return View(user);
}


[HttpPost("/Admin/SuaNguoiDung/{id}")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> SuaNguoiDung(
    int id,
    NguoiDung model)
{
    var user = await _db.NguoiDung
        .FirstOrDefaultAsync(x => x.NguoiDungId == id);

    if (user == null)
        return NotFound();

    var hoTen = (model.HoTen ?? "").Trim();
    var email = (model.Email ?? "").Trim();
    var dienThoai = (model.DienThoai ?? "").Trim();
    var vaiTro = (model.VaiTro ?? "").Trim();

    ModelState.Clear();

    if (string.IsNullOrWhiteSpace(hoTen))
    {
        ModelState.AddModelError(
            nameof(model.HoTen),
            "Vui lòng nhập họ và tên.");
    }

    if (hoTen.Length > 150)
    {
        ModelState.AddModelError(
            nameof(model.HoTen),
            "Họ và tên không được vượt quá 150 ký tự.");
    }

    if (string.IsNullOrWhiteSpace(email) ||
        !new System.ComponentModel.DataAnnotations
            .EmailAddressAttribute().IsValid(email))
    {
        ModelState.AddModelError(
            nameof(model.Email),
            "Email không hợp lệ.");
    }

    if (email.Length > 255)
    {
        ModelState.AddModelError(
            nameof(model.Email),
            "Email không được vượt quá 255 ký tự.");
    }

    if (!string.IsNullOrWhiteSpace(dienThoai) &&
        !System.Text.RegularExpressions.Regex.IsMatch(
            dienThoai,
            @"^0\d{9}$"))
    {
        ModelState.AddModelError(
            nameof(model.DienThoai),
            "Số điện thoại phải gồm 10 chữ số và bắt đầu bằng 0.");
    }

    var vaiTroHopLe = new[]
    {
        "KhachHang",
        "ToChuc",
        "Admin"
    };

    if (!vaiTroHopLe.Contains(vaiTro))
    {
        ModelState.AddModelError(
            nameof(model.VaiTro),
            "Vai trò không hợp lệ.");
    }

    // Không cho phép Admin tự thay đổi vai trò.
    var adminIdText = User.FindFirst(
        System.Security.Claims.ClaimTypes.NameIdentifier
    )?.Value;

    if (!int.TryParse(adminIdText, out int adminId))
        return Forbid();

    if (adminId == id && user.VaiTro != vaiTro)
    {
        ModelState.AddModelError(
            nameof(model.VaiTro),
            "Bạn không thể tự thay đổi vai trò của mình.");
    }

    // Bảo vệ tài khoản Admin cuối cùng.
    if (user.VaiTro == "Admin" && vaiTro != "Admin")
    {
        int soAdmin = await _db.NguoiDung
            .CountAsync(x => x.VaiTro == "Admin");

        if (soAdmin <= 1)
        {
            ModelState.AddModelError(
                nameof(model.VaiTro),
                "Không thể thay đổi vai trò của Admin cuối cùng.");
        }
    }

    if (!ModelState.IsValid)
    {
        model.NguoiDungId = id;
        return View(model);
    }

    var emailChuanHoa = email.ToLower();

    bool emailDaTonTai = await _db.NguoiDung
        .AnyAsync(x =>
            x.NguoiDungId != id &&
            x.Email.ToLower() == emailChuanHoa);

    if (emailDaTonTai)
    {
        ModelState.AddModelError(
            nameof(model.Email),
            "Email đã được tài khoản khác sử dụng.");

        model.NguoiDungId = id;
        return View(model);
    }

    user.HoTen = hoTen;
    user.Email = email;
    user.DienThoai = string.IsNullOrWhiteSpace(dienThoai)
        ? null
        : dienThoai;
    user.VaiTro = vaiTro;

    // Không sửa mật khẩu, trạng thái hay ngày tạo.
    await _db.SaveChangesAsync();

    TempData["ThongBao"] =
        "Đã cập nhật thông tin người dùng thành công.";

    return RedirectToAction(nameof(NguoiDung));
}


    // =====================================================
    // ADMIN CẤP LẠI MẬT KHẨU CHO BAN TỔ CHỨC
    // =====================================================

    [HttpPost("/Admin/CapLaiMatKhauBtc")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CapLaiMatKhauBtc(
        int id,
        string matKhauMoi,
        string xacNhanMatKhau)
    {
        var user = await _db.NguoiDung
            .FirstOrDefaultAsync(x => x.NguoiDungId == id);

        if (user == null)
            return NotFound();

        // Chỉ cho phép cấp lại mật khẩu cho Ban tổ chức.
        if (user.VaiTro != "ToChuc")
        {
            TempData["ThongBao"] =
                "Chỉ được cấp lại mật khẩu cho tài khoản Ban tổ chức.";

            return RedirectToAction(nameof(NguoiDung));
        }

        if (string.IsNullOrWhiteSpace(matKhauMoi) ||
            matKhauMoi.Length < 12 ||
            matKhauMoi.Length > 128)
        {
            TempData["ThongBao"] =
                "Mật khẩu tạm phải có từ 12 đến 128 ký tự.";

            return RedirectToAction(nameof(NguoiDung));
        }

        bool coChuHoa = matKhauMoi.Any(char.IsUpper);
        bool coChuThuong = matKhauMoi.Any(char.IsLower);
        bool coChuSo = matKhauMoi.Any(char.IsDigit);
        bool coKyTuDacBiet = matKhauMoi.Any(
            c => !char.IsLetterOrDigit(c));

        if (!coChuHoa || !coChuThuong ||
            !coChuSo || !coKyTuDacBiet)
        {
            TempData["ThongBao"] =
                "Mật khẩu phải có chữ hoa, chữ thường, " +
                "chữ số và ký tự đặc biệt.";

            return RedirectToAction(nameof(NguoiDung));
        }

        if (matKhauMoi != xacNhanMatKhau)
        {
            TempData["ThongBao"] =
                "Mật khẩu xác nhận không khớp.";

            return RedirectToAction(nameof(NguoiDung));
        }

        // Băm mật khẩu trước khi lưu.
        user.MatKhau = MatKhauHelper.BamMatKhau(matKhauMoi);

        await _db.SaveChangesAsync();

        TempData["ThongBao"] =
            $"Đã cấp lại mật khẩu cho Ban tổ chức {user.HoTen}.";

        return RedirectToAction(nameof(NguoiDung));
    }


[HttpPost("/Admin/KhoaMoKhoaNguoiDung")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> KhoaMoKhoaNguoiDung(int id)
{
    var adminIdText = User.FindFirst(
        System.Security.Claims.ClaimTypes.NameIdentifier
    )?.Value;

    if (!int.TryParse(adminIdText, out int adminId))
        return Forbid();

    if (adminId == id)
    {
        TempData["ThongBao"] =
            "Bạn không thể khóa hoặc mở khóa tài khoản của chính mình.";

        return RedirectToAction(nameof(NguoiDung));
    }

    var user = await _db.NguoiDung
        .FirstOrDefaultAsync(x => x.NguoiDungId == id);

    if (user == null)
        return NotFound();

    if (user.TrangThai == "BiKhoa")
    {
        user.TrangThai = "HoatDong";

        await _db.SaveChangesAsync();

        TempData["ThongBao"] =
            "Đã mở khóa tài khoản thành công.";

        return RedirectToAction(nameof(NguoiDung));
    }

    if (user.TrangThai != "HoatDong")
    {
        TempData["ThongBao"] =
            "Không thể khóa tài khoản có trạng thái không hợp lệ.";

        return RedirectToAction(nameof(NguoiDung));
    }

    // Không cho khóa Admin cuối cùng còn hoạt động.
    if (user.VaiTro == "Admin")
    {
        int soAdminHoatDong = await _db.NguoiDung
            .CountAsync(x =>
                x.VaiTro == "Admin" &&
                x.TrangThai == "HoatDong");

        if (soAdminHoatDong <= 1)
        {
            TempData["ThongBao"] =
                "Không thể khóa Admin cuối cùng đang hoạt động.";

            return RedirectToAction(nameof(NguoiDung));
        }
    }

    user.TrangThai = "BiKhoa";

    await _db.SaveChangesAsync();

    TempData["ThongBao"] =
        "Đã khóa tài khoản thành công.";

    return RedirectToAction(nameof(NguoiDung));
}


[HttpGet("/Admin/ThongKe")]
public async Task<IActionResult> ThongKe(string ky = "180")
{
    var homNay = DateTime.Today;
    var denNgayExclusive = homNay.AddDays(1);

    var soNgay = ky switch
    {
        "30" => 30,
        "90" => 90,
        "180" => 180,
        "365" => 365,
        _ => 180
    };

    if (ky != "30" && ky != "90" &&
        ky != "180" && ky != "365")
    {
        ky = "180";
    }

    var tuNgay = homNay.AddDays(-(soNgay - 1));

    var nguoiDung = _db.NguoiDung.AsNoTracking();
    var suKien = _db.SuKien.AsNoTracking();
    var donHang = _db.DonHang.AsNoTracking();

    // Đơn hàng có trạng thái thanh toán thành công.
    var donThanhCong = donHang
        .Where(x => x.TrangThai == "ThanhCong");

    // Vé nằm trong những đơn hàng thành công.
    // Tính cả vé đã hủy sau khi từng thanh toán,
    // vì đây là chỉ số vé đã được bán trong lịch sử.
    var veThuocDonThanhCong =
        from ve in _db.Ve.AsNoTracking()
        join don in donThanhCong
            on ve.DonHangId equals (int?)don.DonHangId
        select new
        {
            ve.VeId,
            ve.SuKienId,
            ve.TrangThai,
            don.NgayDat,
            don.DonHangId
        };

    var donTrongKy = donThanhCong
        .Where(x => x.NgayDat >= tuNgay &&
                    x.NgayDat < denNgayExclusive);

    var veTrongKy = veThuocDonThanhCong
        .Where(x => x.NgayDat >= tuNgay &&
                    x.NgayDat < denNgayExclusive);

    var suKienTrongKy = suKien
        .Where(x => x.NgayDang >= tuNgay &&
                    x.NgayDang < denNgayExclusive);

    var nguoiDungTrongKy = nguoiDung
        .Where(x => x.NgayTao >= tuNgay &&
                    x.NgayTao < denNgayExclusive);

    var vm = new AdminDashboardVM
    {
        KyThongKe = ky,
        TuNgay = tuNgay,
        DenNgay = homNay,
        CapNhatLuc = DateTime.Now,

        TongNguoiDung = await nguoiDung.CountAsync(),

        TongBanToChuc = await nguoiDung.CountAsync(
            x => x.VaiTro == "ToChuc"),

        TongSuKien = await suKien.CountAsync(),

        SuKienChoDuyet = await suKien.CountAsync(
            x => x.TrangThaiDuyet == "ChoDuyet"),

        SuKienDaDuyet = await suKien.CountAsync(
            x => x.TrangThaiDuyet == "DaDuyet"),

        SuKienTuChoi = await suKien.CountAsync(
            x => x.TrangThaiDuyet == "TuChoi"),

        SuKienDaAn = await suKien.CountAsync(
            x => x.TrangThaiDuyet == "An"),

        TaiKhoanBiKhoa = await nguoiDung.CountAsync(
            x => x.TrangThai == "BiKhoa"),

        YeuCauHuyChoXuLy =
            await _db.YeuCauHoanHuyVe.CountAsync(x =>
                x.HinhThuc == "HuyVe" &&
                x.TrangThai == "ChoDuyet"),

        TongDonThanhCong = await donThanhCong.CountAsync(),

        TongVeBan = await veThuocDonThanhCong.CountAsync(),

        TongDoanhThu = await donThanhCong
            .SumAsync(x => (decimal?)x.TongTien) ?? 0,

        TongTienHoanGhiNhan = await donHang
            .SumAsync(x => (decimal?)x.SoTienHoan) ?? 0,

        NguoiDungMoiTrongKy =
            await nguoiDungTrongKy.CountAsync(),

        BanToChucMoiTrongKy =
            await nguoiDungTrongKy.CountAsync(
                x => x.VaiTro == "ToChuc"),

        SuKienMoiTrongKy =
            await suKienTrongKy.CountAsync(),

        DonThanhCongTrongKy =
            await donTrongKy.CountAsync(),

        VeBanTrongKy =
            await veTrongKy.CountAsync(),

        DoanhThuTrongKy = await donTrongKy
            .SumAsync(x => (decimal?)x.TongTien) ?? 0
    };

    // =====================================================
    // PHAN BO THEO LOAI SU KIEN
    // =====================================================

    var loaiSuKien = await suKienTrongKy
        .Include(x => x.LoaiSuKien)
        .GroupBy(x => x.LoaiSuKien!.TenLoai)
        .Select(g => new
        {
            Ten = g.Key,
            SoLuong = g.Count()
        })
        .OrderByDescending(x => x.SoLuong)
        .ToListAsync();

    vm.TenLoaiSuKien = loaiSuKien
        .Select(x => x.Ten ?? "Khác")
        .ToList();

    vm.SoLuongTheoLoai = loaiSuKien
        .Select(x => x.SoLuong)
        .ToList();

    // =====================================================
    // HINH THUC TO CHUC
    // =====================================================

    var suKienIds = await suKienTrongKy
        .Select(x => x.SuKienId)
        .ToListAsync();

    var trucTiepIds = await _db.DiaDiem
        .AsNoTracking()
        .Where(x => suKienIds.Contains(x.SuKienId))
        .Select(x => x.SuKienId)
        .Distinct()
        .ToListAsync();

    var trucTuyenIds = await _db.SuKienTrucTuyen
        .AsNoTracking()
        .Where(x => suKienIds.Contains(x.SuKienId))
        .Select(x => x.SuKienId)
        .Distinct()
        .ToListAsync();

    var trucTiepSet = trucTiepIds.ToHashSet();
    var trucTuyenSet = trucTuyenIds.ToHashSet();

    vm.SuKienHybrid = trucTiepSet
        .Intersect(trucTuyenSet)
        .Count();

    vm.SuKienTrucTiep = trucTiepSet.Count -
        vm.SuKienHybrid;

    vm.SuKienTrucTuyen = trucTuyenSet.Count -
        vm.SuKienHybrid;

    // =====================================================
    // XU HUONG THEO THANG
    // =====================================================

    var lichThang = new List<DateTime>();

    var thangBatDau =
        new DateTime(tuNgay.Year, tuNgay.Month, 1);

    var thangKetThuc =
        new DateTime(homNay.Year, homNay.Month, 1);

    for (var ngay = thangBatDau;
         ngay <= thangKetThuc;
         ngay = ngay.AddMonths(1))
    {
        lichThang.Add(ngay);
    }

    // Chỉ lấy các trường cần cho thống kê,
    // sau đó gom theo tháng ở bộ nhớ.
    var ngayTaoSuKien = await suKienTrongKy
        .Select(x => x.NgayDang)
        .ToListAsync();

    var ngayTaoNguoiDung = await nguoiDungTrongKy
        .Select(x => x.NgayTao)
        .ToListAsync();

    var donThanhCongTrongKy = await donTrongKy
        .Select(x => new
        {
            x.DonHangId,
            x.NgayDat,
            x.TongTien
        })
        .ToListAsync();

    var ngayBanVe = await veTrongKy
        .Select(x => x.NgayDat)
        .ToListAsync();

    foreach (var thang in lichThang)
    {
        var dauThang = thang;
        var cuoiThang = thang.AddMonths(1);

        var nhan = thang.ToString("yyyy-MM");

        vm.ThangSuKien.Add(nhan);
        vm.ThangNguoiDung.Add(nhan);
        vm.ThangDoanhThu.Add(nhan);

        vm.SoSuKienTheoThang.Add(
            ngayTaoSuKien.Count(x =>
                x >= dauThang && x < cuoiThang)
        );

        vm.SoNguoiDungTheoThang.Add(
            ngayTaoNguoiDung.Count(x =>
                x >= dauThang && x < cuoiThang)
        );

        vm.DoanhThuTheoThang.Add(
            donThanhCongTrongKy
                .Where(x =>
                    x.NgayDat >= dauThang &&
                    x.NgayDat < cuoiThang)
                .Sum(x => x.TongTien)
        );

        vm.VeBanTheoThang.Add(
            ngayBanVe.Count(x =>
                x >= dauThang && x < cuoiThang)
        );
    }

    // =====================================================
    // TOP SU KIEN THEO SO VE BAN TRONG KY
    // =====================================================

    var topSuKienIds = await veTrongKy
        .GroupBy(x => x.SuKienId)
        .Select(g => new
        {
            SuKienId = g.Key,
            SoVe = g.Count()
        })
        .OrderByDescending(x => x.SoVe)
        .Take(8)
        .ToListAsync();

    var topIds = topSuKienIds
        .Select(x => x.SuKienId)
        .ToList();

    var tenSuKien = await suKien
        .Where(x => topIds.Contains(x.SuKienId))
        .Select(x => new
        {
            x.SuKienId,
            x.TenSuKien
        })
        .ToDictionaryAsync(x => x.SuKienId, x => x.TenSuKien);

    vm.TopSuKien = topSuKienIds
        .Select(x => new AdminTopSuKienVM
        {
            SuKienId = x.SuKienId,
            TenSuKien = tenSuKien.TryGetValue(
                x.SuKienId, out var ten)
                    ? ten
                    : "Sự kiện không còn tồn tại",

            SoVeBan = x.SoVe
        })
        .ToList();

    return View(vm);
}

// =====================================================
// DRILL-THROUGH: LAY CHI TIET KHI BAM BIEU DO
// =====================================================

[HttpGet("/Admin/ThongKeChiTiet")]
public async Task<IActionResult> ThongKeChiTiet(
    string loai,
    string giaTri,
    string ky = "180")
{
    var homNay = DateTime.Today;

    int soNgay = ky switch
    {
        "30" => 30,
        "90" => 90,
        "365" => 365,
        _ => 180
    };

    var tuNgay = homNay.AddDays(-(soNgay - 1));
    var denNgay = homNay.AddDays(1);

    // Các loại truy vấn chỉ đọc.
    const int gioiHanBanGhi = 100;

    if (loai == "suKienThang" ||
        loai == "nguoiDungThang" ||
        loai == "donHangThang")
    {
        if (!DateTime.TryParseExact(
            giaTri + "-01",
            "yyyy-MM-dd",
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None,
            out var dauThang))
        {
            return BadRequest(new
            {
                message = "Tháng thống kê không hợp lệ."
            });
        }

        var cuoiThang = dauThang.AddMonths(1);

        // Chỉ xem dữ liệu thuộc kỳ đang được chọn.
        var batDau = dauThang > tuNgay
            ? dauThang : tuNgay;

        var ketThuc = cuoiThang < denNgay
            ? cuoiThang : denNgay;

        if (batDau >= ketThuc)
        {
            return Ok(new
            {
                title = "Không có dữ liệu",
                items = Array.Empty<object>()
            });
        }

        if (loai == "suKienThang")
        {
            var items = await _db.SuKien
                .AsNoTracking()
                .Where(x =>
                    x.NgayDang >= batDau &&
                    x.NgayDang < ketThuc)
                .OrderByDescending(x => x.NgayDang)
                .Take(gioiHanBanGhi)
                .Select(x => new
                {
                    title = x.TenSuKien,
                    subtitle = "Sự kiện #" + x.SuKienId,
                    detail = x.TrangThaiDuyet,
                    date = x.NgayDang.ToString("dd/MM/yyyy")
                })
                .ToListAsync();

            return Ok(new
            {
                title = "Sự kiện tạo trong " + giaTri,
                items
            });
        }

        if (loai == "nguoiDungThang")
        {
            var items = await _db.NguoiDung
                .AsNoTracking()
                .Where(x =>
                    x.NgayTao >= batDau &&
                    x.NgayTao < ketThuc)
                .OrderByDescending(x => x.NgayTao)
                .Take(gioiHanBanGhi)
                .Select(x => new
                {
                    title = x.HoTen,
                    subtitle = x.Email,
                    detail = x.VaiTro,
                    date = x.NgayTao.ToString("dd/MM/yyyy")
                })
                .ToListAsync();

            return Ok(new
            {
                title = "Người dùng mới trong " + giaTri,
                items
            });
        }

        var donHangItems = await _db.DonHang
            .AsNoTracking()
            .Where(x =>
                x.TrangThai == "ThanhCong" &&
                x.NgayDat >= batDau &&
                x.NgayDat < ketThuc)
            .OrderByDescending(x => x.NgayDat)
            .Take(gioiHanBanGhi)
            .Select(x => new
            {
                title = "Đơn hàng #" + x.DonHangId,
                subtitle = x.NguoiDung.HoTen,
                detail = x.TongTien.ToString("N0") + " đ",
                date = x.NgayDat.ToString("dd/MM/yyyy")
            })
            .ToListAsync();

        return Ok(new
        {
            title = "Đơn hàng thành công trong " + giaTri,
            items = donHangItems
        });
    }

    if (loai == "trangThai")
    {
        var trangThaiHopLe = new[]
        {
            "ChoDuyet", "DaDuyet", "TuChoi", "An"
        };

        if (!trangThaiHopLe.Contains(giaTri))
            return BadRequest();

        var items = await _db.SuKien
            .AsNoTracking()
            .Where(x => x.TrangThaiDuyet == giaTri)
            .OrderByDescending(x => x.NgayDang)
            .Take(gioiHanBanGhi)
            .Select(x => new
            {
                title = x.TenSuKien,
                subtitle = "Sự kiện #" + x.SuKienId,
                detail = x.TrangThaiDuyet,
                date = x.NgayDang.ToString("dd/MM/yyyy")
            })
            .ToListAsync();

        return Ok(new
        {
            title = "Sự kiện theo trạng thái",
            items
        });
    }

    if (loai == "suKien")
    {
        if (!int.TryParse(giaTri, out int suKienId))
            return BadRequest();

        var suKien = await _db.SuKien
            .AsNoTracking()
            .Where(x => x.SuKienId == suKienId)
            .Select(x => new
            {
                x.SuKienId,
                x.TenSuKien
            })
            .FirstOrDefaultAsync();

        if (suKien == null)
            return NotFound();

        var items = await (
            from ve in _db.Ve.AsNoTracking()
            join don in _db.DonHang.AsNoTracking()
                on ve.DonHangId equals (int?)don.DonHangId
            where ve.SuKienId == suKienId &&
                  don.TrangThai == "ThanhCong" &&
                  don.NgayDat >= tuNgay &&
                  don.NgayDat < denNgay
            orderby don.NgayDat descending
            select new
            {
                title = ve.MaVe,
                subtitle = ve.NguoiDung.HoTen,
                detail = ve.TrangThai,
                date = don.NgayDat.ToString("dd/MM/yyyy")
            }
        )
        .Take(gioiHanBanGhi)
        .ToListAsync();

        return Ok(new
        {
            title = "Vé bán: " + suKien.TenSuKien,
            items
        });
    }

    return BadRequest(new
    {
        message = "Loại phân tích không hợp lệ."
    });
}

[HttpGet("/Admin/ThongKeBanToChucDuLieu")]
public async Task<IActionResult> ThongKeBanToChucDuLieu(
    string ky = "180")
{
    int soNgay = ky switch
    {
        "30" => 30,
        "90" => 90,
        "365" => 365,
        _ => 180
    };

    DateTime homNay = DateTime.Today;
    DateTime tuNgay = homNay.AddDays(1 - soNgay);
    DateTime denNgay = homNay.AddDays(1);

    // Danh sách BTC thật, dùng ToChucId làm khóa.
    var btcList = await _db.ToChucSuKien
        .AsNoTracking()
        .Select(x => new
        {
            x.ToChucId,
            x.NguoiDungId,
            x.TenToChuc,
            x.EmailLienHe
        })
        .ToListAsync();

    // Sự kiện được TẠO trong kỳ.
    var suKienTheoBtc = await _db.SuKien
        .AsNoTracking()
        .Where(x => x.NgayDang >= tuNgay &&
                    x.NgayDang < denNgay)
        .GroupBy(x => x.ToChucId)
        .Select(g => new
        {
            ToChucId = g.Key,
            SoSuKien = g.Count()
        })
        .ToListAsync();

    // Vé thuộc đơn hàng thành công,
    // tính theo ngày đặt của đơn hàng.
    var veTheoBtc = await (
        from ve in _db.Ve.AsNoTracking()
        join don in _db.DonHang.AsNoTracking()
            on ve.DonHangId equals (int?)don.DonHangId
        join sk in _db.SuKien.AsNoTracking()
            on ve.SuKienId equals sk.SuKienId
        where don.TrangThai == "ThanhCong"
            && don.NgayDat >= tuNgay
            && don.NgayDat < denNgay
        group ve by sk.ToChucId into g
        select new
        {
            ToChucId = g.Key,
            SoVeBan = g.Count()
        }
    ).ToListAsync();

    // Số yêu cầu hủy được gửi trong kỳ.
    // Không đồng nghĩa với số vé đã hủy.
    var huyTheoBtc = await (
        from yc in _db.YeuCauHoanHuyVe.AsNoTracking()
        join ve in _db.Ve.AsNoTracking()
            on yc.VeId equals ve.VeId
        join sk in _db.SuKien.AsNoTracking()
            on ve.SuKienId equals sk.SuKienId
        where yc.HinhThuc == "HuyVe"
            && yc.NgayYeuCau >= tuNgay
            && yc.NgayYeuCau < denNgay
        group yc by sk.ToChucId into g
        select new
        {
            ToChucId = g.Key,
            SoYeuCauHuy = g.Count()
        }
    ).ToListAsync();

    var suKienMap = suKienTheoBtc
        .ToDictionary(x => x.ToChucId, x => x.SoSuKien);

    var veMap = veTheoBtc
        .ToDictionary(x => x.ToChucId, x => x.SoVeBan);

    var huyMap = huyTheoBtc
        .ToDictionary(x => x.ToChucId, x => x.SoYeuCauHuy);

    var rows = btcList.Select(x => new
    {
        toChucId = x.ToChucId,
        nguoiDungId = x.NguoiDungId,

        tenToChuc = string.IsNullOrWhiteSpace(x.TenToChuc)
            ? "BTC #" + x.ToChucId
            : x.TenToChuc,

        email = x.EmailLienHe ?? "",

        soSuKien = suKienMap.GetValueOrDefault(x.ToChucId),
        soVeBan = veMap.GetValueOrDefault(x.ToChucId),
        soYeuCauHuy = huyMap.GetValueOrDefault(x.ToChucId)
    })
    .OrderByDescending(x => x.soVeBan)
    .ThenByDescending(x => x.soSuKien)
    .ThenBy(x => x.tenToChuc)
    .ToList();

    return Ok(new
    {
        tuNgay = tuNgay.ToString("dd/MM/yyyy"),
        denNgay = homNay.ToString("dd/MM/yyyy"),
        capNhatLuc = DateTime.Now.ToString("HH:mm:ss"),
        tongBtc = rows.Count,
        tongSuKien = rows.Sum(x => x.soSuKien),
        tongVeBan = rows.Sum(x => x.soVeBan),
        tongYeuCauHuy = rows.Sum(x => x.soYeuCauHuy),
        rows
    });
}

[HttpGet("/Admin/ThongKeBanToChucChiTiet")]
public async Task<IActionResult> ThongKeBanToChucChiTiet(
    int toChucId,
    string ky = "180")
{
    int soNgay = ky switch
    {
        "30" => 30,
        "90" => 90,
        "365" => 365,
        _ => 180
    };

    DateTime tuNgay = DateTime.Today.AddDays(1 - soNgay);
    DateTime denNgay = DateTime.Today.AddDays(1);

    var btc = await _db.ToChucSuKien
        .AsNoTracking()
        .Where(x => x.ToChucId == toChucId)
        .Select(x => new
        {
            x.ToChucId,
            x.TenToChuc
        })
        .FirstOrDefaultAsync();

    if (btc == null)
    {
        return NotFound(new
        {
            message = "Không tìm thấy Ban tổ chức."
        });
    }

    // Liệt kê TẤT CẢ sự kiện của BTC.
    // Số vé, yêu cầu hủy bên dưới vẫn lọc theo kỳ.
    var suKien = await _db.SuKien
        .AsNoTracking()
        .Where(x => x.ToChucId == toChucId)
        .OrderByDescending(x => x.NgayDang)
        .Select(x => new
        {
            x.SuKienId,
            x.TenSuKien,
            x.NgayDang,
            x.TrangThaiDuyet
        })
        .ToListAsync();

    var veTheoSuKien = await (
        from ve in _db.Ve.AsNoTracking()
        join don in _db.DonHang.AsNoTracking()
            on ve.DonHangId equals (int?)don.DonHangId
        join sk in _db.SuKien.AsNoTracking()
            on ve.SuKienId equals sk.SuKienId
        where sk.ToChucId == toChucId
            && don.TrangThai == "ThanhCong"
            && don.NgayDat >= tuNgay
            && don.NgayDat < denNgay
        group ve by ve.SuKienId into g
        select new
        {
            SuKienId = g.Key,
            SoVeBan = g.Count()
        }
    ).ToListAsync();

    var huyTheoSuKien = await (
        from yc in _db.YeuCauHoanHuyVe.AsNoTracking()
        join ve in _db.Ve.AsNoTracking()
            on yc.VeId equals ve.VeId
        join sk in _db.SuKien.AsNoTracking()
            on ve.SuKienId equals sk.SuKienId
        where sk.ToChucId == toChucId
            && yc.HinhThuc == "HuyVe"
            && yc.NgayYeuCau >= tuNgay
            && yc.NgayYeuCau < denNgay
        group yc by ve.SuKienId into g
        select new
        {
            SuKienId = g.Key,
            SoYeuCauHuy = g.Count()
        }
    ).ToListAsync();

    var veMap = veTheoSuKien
        .ToDictionary(x => x.SuKienId, x => x.SoVeBan);

    var huyMap = huyTheoSuKien
        .ToDictionary(x => x.SuKienId, x => x.SoYeuCauHuy);

    var rows = suKien.Select(x => new
    {
        suKienId = x.SuKienId,
        tenSuKien = x.TenSuKien,
        ngayTao = x.NgayDang.ToString("dd/MM/yyyy"),
        trangThai = x.TrangThaiDuyet,
        soVeBan = veMap.GetValueOrDefault(x.SuKienId),
        soYeuCauHuy = huyMap.GetValueOrDefault(x.SuKienId)
    })
    .ToList();

    return Ok(new
    {
        toChucId = btc.ToChucId,
        tenToChuc = btc.TenToChuc,
        tongSuKien = rows.Count,
        suKienMoiTrongKy = suKien.Count(x =>
            x.NgayDang >= tuNgay &&
            x.NgayDang < denNgay),
        tongVeBanTrongKy = rows.Sum(x => x.soVeBan),
        tongYeuCauHuyTrongKy = rows.Sum(x => x.soYeuCauHuy),
        rows
    });
}

}

}