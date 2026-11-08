
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VeSuKienWeb.Data;
using VeSuKienWeb.Models.KhamPhaSuKien;

namespace VeSuKienWeb.Services
{
    public class KhamPhaSuKienService
    {
        private readonly NguCanhSuKien _db;
        private readonly GoiYSuKienService _goiY;

        public KhamPhaSuKienService(
            NguCanhSuKien db,
            GoiYSuKienService goiY)
        {
            _db = db;
            _goiY = goiY;
        }

public async Task<KetQuaKhamPha> TimKiemAsync(
    NhuCauKhamPha nhuCau,
    int? nguoiDungId = null,
    int? gioiHanKetQua = 3)
        {
            var ketQua = new KetQuaKhamPha
            {
                NhuCau = nhuCau
            };

            DateTime hienTai = DateTime.Now;

            string chuDeCanTim = ChuanHoa(nhuCau.ChuDe);
            string diaDiemCanTim =
                ChuanHoaDiaDiem(nhuCau.DiaDiem);
            string tuKhoa = ChuanHoa(nhuCau.TuKhoa);

            DateTime? tuNgay = DocNgay(nhuCau.TuNgay);
            DateTime? denNgay = DocNgay(nhuCau.DenNgay);

            // Tránh trả về sự kiện bất kỳ khi câu hỏi
            // không chứa một điều kiện tìm kiếm nào.
            bool coDieuKien =
                chuDeCanTim.Length > 0 ||
                diaDiemCanTim.Length > 0 ||
                tuKhoa.Length > 0 ||
                tuNgay.HasValue ||
                denNgay.HasValue ||
                nhuCau.NganSachToiDa.HasValue ||
                !string.IsNullOrWhiteSpace(nhuCau.HinhThuc);

            if (!coDieuKien)
            {
                ketQua.ThongBao =
                    "Bạn hãy cho biết chủ đề, địa điểm, " +
                    "thời gian hoặc ngân sách muốn tìm.";
                return ketQua;
            }

            // ======================================
            // 1. SỰ KIỆN ĐÃ DUYỆT, CHƯA KẾT THÚC
            // ======================================
            var suKiens = await _db.SuKien
                .AsNoTracking()
                .Where(x =>
                    x.TrangThaiDuyet == "DaDuyet" &&
                    x.ThoiGianKetThuc > hienTai)
                .Select(x => new
                {
                    x.SuKienId,
                    x.TenSuKien,
                    x.MoTaChiTiet,
                    x.AnhSuKien,
                    x.ThoiGianBatDau,
                    x.ThoiGianKetThuc,
                    x.GiaVeMacDinh
                })
                .ToListAsync();

            if (suKiens.Count == 0)
            {
                ketQua.ThongBao =
                    "Hiện chưa có sự kiện đã duyệt " +
                    "và còn hiệu lực.";
                return ketQua;
            }

            var ids = suKiens
                .Select(x => x.SuKienId)
                .ToList();

            // ======================================
            // 2. ĐỊA ĐIỂM VÀ ĐỊA CHỈ
            // ======================================
            var diaDiems = await _db.DiaDiem
                .AsNoTracking()
                .Where(x => ids.Contains(x.SuKienId))
                .Select(x => new
                {
                    x.SuKienId,
                    x.TenDiaDiem,
                    x.DiaChi
                })
                .ToListAsync();

            var diaDiemMap = diaDiems
                .GroupBy(x => x.SuKienId)
                .ToDictionary(
                    g => g.Key,
                    g => new
                    {
                        Ten = string.Join("; ",
                            g.Select(x => x.TenDiaDiem)
                             .Where(x =>
                                 !string.IsNullOrWhiteSpace(x))
                             .Distinct()),

                        DiaChi = string.Join("; ",
                            g.Select(x => x.DiaChi)
                             .Where(x =>
                                 !string.IsNullOrWhiteSpace(x))
                             .Distinct())
                    });

            // ======================================
            // 3. CHỦ ĐỀ SỰ KIỆN
            // ======================================
            var lienKetChuDe = await (
                from lk in _db.SuKienChuDe.AsNoTracking()
                join cd in _db.ChuDeSuKien.AsNoTracking()
                    on lk.ChuDeId equals cd.ChuDeId
                where ids.Contains(lk.SuKienId)
                select new
                {
                    lk.SuKienId,
                    cd.TenChuDe
                }
            ).ToListAsync();

            var chuDeMap = lienKetChuDe
                .GroupBy(x => x.SuKienId)
                .ToDictionary(
                    g => g.Key,
                    g => string.Join(", ",
                        g.Select(x => x.TenChuDe)
                         .Distinct())
                );

            // ======================================
            // 4. HÌNH THỨC TỔ CHỨC
            // ======================================
            var trucTuyenIds = (await _db.SuKienTrucTuyen
                .AsNoTracking()
                .Where(x => ids.Contains(x.SuKienId))
                .Select(x => x.SuKienId)
                .ToListAsync())
                .ToHashSet();

            var trucTiepIds = diaDiems
                .Select(x => x.SuKienId)
                .ToHashSet();

            // ======================================
            // 5. GIÁ VÉ
            // ======================================
            var loaiVes = await _db.LoaiVe
                .AsNoTracking()
                .Where(x => ids.Contains(x.SuKienId))
                .Select(x => new
                {
                    x.LoaiVeId,
                    x.SuKienId,
                    x.GiaVe,
                    x.SoLuong
                })
                .ToListAsync();

            var daBan = await _db.Ve
                .AsNoTracking()
                .Where(x =>
                    ids.Contains(x.SuKienId) &&
                    (x.TrangThai == "DaThanhToan" ||
                     x.TrangThai == "DaCheckIn"))
                .GroupBy(x => x.LoaiVeId)
                .Select(g => new
                {
                    LoaiVeId = g.Key,
                    SoLuongDaBan = g.Count()
                })
                .ToListAsync();

            // LoaiVeId trong Models/Ve.cs là int.
            var daBanMap = daBan.ToDictionary(
                x => x.LoaiVeId,
                x => x.SoLuongDaBan
            );

            var loaiVeMap = loaiVes
                .GroupBy(x => x.SuKienId)
                .ToDictionary(
                    g => g.Key,
                    g => g.ToList()
                );

            // ======================================
            // 6. ĐIỂM COSINE CÁ NHÂN HÓA
            // ======================================
            var diemCaNhan = new Dictionary<int, double>();

            if (nguoiDungId.HasValue &&
                nguoiDungId.Value > 0)
            {
                var baoCao = await _goiY.LayBaoCaoAsync(
                    nguoiDungId.Value);

                diemCaNhan = baoCao.DanhSach
                    .ToDictionary(
                        x => x.SuKienId,
                        x => Math.Clamp(x.Cosine, 0, 1)
                    );
            }

            // ======================================
            // 7. TÌM KIẾM VÀ ĐÁNH GIÁ
            // ======================================
            var danhSach = new List<SuKienPhuHop>();

            foreach (var sk in suKiens)
            {
                // Sự kiện giao với khoảng ngày yêu cầu.
                bool khopThoiGian =
                    (!tuNgay.HasValue ||
                        sk.ThoiGianKetThuc.Date >=
                        tuNgay.Value.Date) &&
                    (!denNgay.HasValue ||
                        sk.ThoiGianBatDau.Date <=
                        denNgay.Value.Date);

                bool coTrucTiep =
                    trucTiepIds.Contains(sk.SuKienId);

                bool coTrucTuyen =
                    trucTuyenIds.Contains(sk.SuKienId);

                // Hình thức được giữ là điều kiện bắt buộc.
                if (nhuCau.HinhThuc == "TrucTiep" &&
                    !coTrucTiep)
                    continue;

                if (nhuCau.HinhThuc == "TrucTuyen" &&
                    !coTrucTuyen)
                    continue;

                if (nhuCau.HinhThuc == "Hybrid" &&
                    !(coTrucTiep && coTrucTuyen))
                    continue;

                // ==================================
                // 8. THÔNG TIN ĐỊA ĐIỂM
                // ==================================
                string tenDiaDiem = "";
                string diaChi = "";

                if (diaDiemMap.TryGetValue(
                    sk.SuKienId, out var dd))
                {
                    tenDiaDiem = dd.Ten;
                    diaChi = dd.DiaChi;
                }

                string diaDiemDayDu =
                    $"{tenDiaDiem} {diaChi}".Trim();

                bool khopDiaDiem =
                    diaDiemCanTim.Length == 0 ||
                    ChuanHoaDiaDiem(diaDiemDayDu)
                        .Contains(diaDiemCanTim);

                // ==================================
                // 9. NỘI DUNG SỰ KIỆN
                // ==================================
                string chuDe = chuDeMap.GetValueOrDefault(
                    sk.SuKienId, "");

                string tenChuanHoa =
                    ChuanHoa(sk.TenSuKien);

                string chuDeChuanHoa =
                    ChuanHoa(chuDe);

                string moTaChuanHoa =
                    ChuanHoa(sk.MoTaChiTiet);

                // Tìm trong cả tên, chủ đề, mô tả.
                string noiDungTimKiem = ChuanHoa(
                    $"{sk.TenSuKien} {chuDe} " +
                    $"{sk.MoTaChiTiet}");

                bool coYeuCauChuDe =
                    chuDeCanTim.Length > 0;

                bool khopChuDeChinhXac =
                    !coYeuCauChuDe ||
                    ChuaCumTu(noiDungTimKiem, chuDeCanTim);

                double diemLienQuan = coYeuCauChuDe
                    ? TinhDiemLienQuan(
                        chuDeCanTim, noiDungTimKiem)
                    : 0;

                // Bắt buộc có liên quan về nội dung,
                // không lấy sự kiện bất kỳ cho đủ 3.
                if (coYeuCauChuDe &&
                    !khopChuDeChinhXac &&
                    diemLienQuan < 12.5)
                {
                    continue;
                }

                // Nếu chỉ có từ khóa bổ sung thì
                // từ khóa cũng phải thực sự liên quan.
                bool khopTuKhoa =
                    tuKhoa.Length == 0 ||
                    ChuaCumTu(noiDungTimKiem, tuKhoa) ||
                    TinhDiemLienQuan(
                        tuKhoa, noiDungTimKiem) >= 12.5;

                if (!coYeuCauChuDe &&
                    tuKhoa.Length > 0 &&
                    !khopTuKhoa)
                {
                    continue;
                }

                // ==================================
                // 10. GIÁ VÉ THẤP NHẤT
                // ==================================
                decimal? giaThapNhat = null;

                if (loaiVeMap.TryGetValue(
                    sk.SuKienId, out var cacLoaiVe))
                {
                    var veConSoLuong = cacLoaiVe
                        .Where(v =>
                            v.SoLuong >
                            daBanMap.GetValueOrDefault(
                                v.LoaiVeId))
                        .ToList();

                    if (veConSoLuong.Count == 0)
                        continue;

                    giaThapNhat = veConSoLuong
                        .Min(v => v.GiaVe);
                }
                else
                {
                    giaThapNhat = sk.GiaVeMacDinh;
                }

                // Ngân sách là giới hạn cứng.
                if (nhuCau.NganSachToiDa.HasValue)
                {
                    if (!giaThapNhat.HasValue ||
                        giaThapNhat.Value >
                        nhuCau.NganSachToiDa.Value)
                    {
                        continue;
                    }
                }

                // ==================================
                // 11. PHÂN LOẠI KHỚP / MỞ RỘNG
                // ==================================
                bool khopChuDe =
                    khopChuDeChinhXac && khopTuKhoa;

                bool laGoiYMoRong =
                    !khopChuDe ||
                    !khopDiaDiem ||
                    !khopThoiGian;

                var dieuKienChuaKhop =
                    new List<string>();

                if (!khopChuDe)
                {
                    dieuKienChuaKhop.Add(
                        "Nội dung chỉ liên quan một phần");
                }

                if (!khopDiaDiem)
                {
                    dieuKienChuaKhop.Add(
                        "Khác địa điểm yêu cầu");
                }

                if (!khopThoiGian)
                {
                    dieuKienChuaKhop.Add(
                        "Ngoài khoảng thời gian yêu cầu");
                }

                // Không có chủ đề/từ khóa liên quan và
                // sai địa điểm: không lấy làm gợi ý mở rộng.
                if (!coYeuCauChuDe &&
                    tuKhoa.Length == 0 &&
                    !khopDiaDiem)
                {
                    continue;
                }

                // ==================================
                // 12. CHẤM ĐIỂM
                // ==================================
                double diem = 0;
                var lyDo = new List<string>();

                if (coYeuCauChuDe)
                {
                    if (khopChuDeChinhXac)
                    {
                        diem += 50;
                        lyDo.Add("Khớp chủ đề/nội dung");

                        if (ChuaCumTu(
                            tenChuanHoa, chuDeCanTim))
                        {
                            diem += 20;
                        }
                        else if (ChuaCumTu(
                            chuDeChuanHoa, chuDeCanTim))
                        {
                            diem += 15;
                        }
                        else if (ChuaCumTu(
                            moTaChuanHoa, chuDeCanTim))
                        {
                            diem += 10;
                        }
                    }
                    else
                    {
                        diem += Math.Min(diemLienQuan, 25);
                        lyDo.Add(
                            "Có nội dung liên quan");
                    }
                }

                if (tuKhoa.Length > 0 && khopTuKhoa)
                {
                    diem += ChuaCumTu(
                        noiDungTimKiem, tuKhoa) ? 8 : 4;

                    lyDo.Add("Có từ khóa phù hợp");
                }

                if (diaDiemCanTim.Length > 0 &&
                    khopDiaDiem)
                {
                    diem += 15;
                    lyDo.Add("Đúng địa điểm");
                }

                if ((tuNgay.HasValue ||
                     denNgay.HasValue) &&
                    khopThoiGian)
                {
                    diem += 10;
                    lyDo.Add("Đúng thời gian");
                }

                if (nhuCau.NganSachToiDa.HasValue)
                {
                    diem += 10;
                    lyDo.Add("Trong ngân sách");
                }

                if (!string.IsNullOrWhiteSpace(
                    nhuCau.HinhThuc))
                {
                    diem += 5;
                    lyDo.Add("Đúng hình thức");
                }

                // Cosine chỉ được cộng tối đa 10 điểm.

 // Điểm nội dung đã bao gồm:
 // chủ đề, tên, mô tả, địa điểm,
 // thời gian, hình thức và ngân sách.
 double diemNoiDung = diem;

 // Cosine nằm trong khoảng 0 đến 1.
 double cosine = Math.Clamp(
     diemCaNhan.GetValueOrDefault(sk.SuKienId, 0),
     0,
     1
 );

 // Giữ tổng điểm để theo dõi nội bộ.
 // Cosine đóng góp tối đa 10 điểm.
 diem = diemNoiDung + cosine * 10;


                danhSach.Add(new SuKienPhuHop
                {
                    SuKienId = sk.SuKienId,
                    TenSuKien = sk.TenSuKien,
                    AnhSuKien = sk.AnhSuKien,
                    MoTaChiTiet = sk.MoTaChiTiet,

                    ThoiGianBatDau =
                        sk.ThoiGianBatDau,

                    ThoiGianKetThuc =
                        sk.ThoiGianKetThuc,

                    TenDiaDiem = tenDiaDiem,
                    DiaChi = diaChi,

                    DiaDiem =
                        diaDiemDayDu.Length > 0
                            ? diaDiemDayDu
                            : (coTrucTuyen
                                ? "Trực tuyến"
                                : "Chưa cập nhật"),

                    ChuDe = chuDe,
                    GiaVeThapNhat = giaThapNhat,
                    DiemNoiDung = diemNoiDung,
                    DiemCosine = cosine,
                    DiemPhuHop = diem,

                    LaGoiYMoRong = laGoiYMoRong,

                    DieuKienChuaKhop =
                        string.Join("; ",
                            dieuKienChuaKhop),

                    LyDoPhuHop =
                        lyDo.Count > 0
                            ? string.Join("; ", lyDo)
                            : "Sự kiện còn hiệu lực"
                });
            }

            // ======================================
            // 13. XẾP HẠNG VÀ CHỌN TOP-3
            // ======================================
            // Khớp đầy đủ luôn đứng trước gợi ý mở rộng.

 // Thứ tự ưu tiên:
 // 1. Đáp ứng đầy đủ điều kiện tìm kiếm.
 // 2. Điểm phù hợp với nội dung câu hỏi.
 // 3. Điểm Cosine theo sở thích người dùng.
 // 4. Sự kiện diễn ra sớm hơn.
 ketQua.DanhSach = danhSach
     .OrderBy(x => x.LaGoiYMoRong)
     .ThenByDescending(x => x.DiemNoiDung)
     .ThenByDescending(x => x.DiemCosine)
     .ThenBy(x => x.ThoiGianBatDau)
.ThenBy(x => x.SuKienId)
.Take(gioiHanKetQua ?? int.MaxValue)
.ToList();


#if DEBUG
if (nguoiDungId.HasValue)
{
    Console.WriteLine();
    Console.WriteLine("===== KIEM TRA COSINE TOP-3 =====");

    foreach (var sk in ketQua.DanhSach)
    {
        Console.WriteLine(
            $"SuKienId: {sk.SuKienId} | " +
            $"Ten: {sk.TenSuKien} | " +
            $"NoiDung: {sk.DiemNoiDung:F2} | " +
            $"Cosine: {sk.DiemCosine:F4} | " +
            $"Tong: {sk.DiemPhuHop:F2}"
        );
    }

    Console.WriteLine("===============================");
}
#endif


            if (ketQua.DanhSach.Count == 0)
            {
                ketQua.ThongBao =
                    "Chưa tìm thấy sự kiện có nội dung " +
                    "liên quan và đáp ứng các giới hạn " +
                    "bắt buộc. Bạn có thể thử chủ đề " +
                    "rộng hơn hoặc điều chỉnh ngân sách.";
            }
            else if (ketQua.DanhSach.Any(
                x => x.LaGoiYMoRong))
            {
                ketQua.ThongBao =
                    "Đây là các sự kiện phù hợp hoặc " +
                    "có nội dung liên quan nhất. " +
                    "Một số kết quả khác địa điểm " +
                    "hoặc thời gian bạn yêu cầu.";
            }
            else
            {
                ketQua.ThongBao =
                    $"Tìm thấy {ketQua.DanhSach.Count} " +
                    "sự kiện phù hợp nhất.";
            }

            return ketQua;
        }

        // ==========================================
        // CÁC HÀM HỖ TRỢ
        // ==========================================

        private static DateTime? DocNgay(string? value)
        {
            if (DateTime.TryParseExact(
                value,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var ngay))
            {
                return ngay.Date;
            }

            return null;
        }

        // So khớp cả cụm từ, tránh khớp một phần chữ.
        private static bool ChuaCumTu(
            string noiDung,
            string cumTu)
        {
            if (string.IsNullOrWhiteSpace(cumTu))
                return true;

            string a = " " + ChuanHoa(noiDung) + " ";
            string b = " " + ChuanHoa(cumTu) + " ";

            return a.Contains(
                b,
                StringComparison.Ordinal);
        }

        // Điểm liên quan theo tỷ lệ từ khóa trùng.
        // Đây là so khớp từ khóa, chưa phải embedding.
  
private static double TinhDiemLienQuan(
    string chuDeCanTim,
    string noiDungSuKien)
{
    // Bỏ các từ chung chung, không đại diện
    // cho chủ đề thực sự của khách hàng.
    var tuChung = new HashSet<string>
    {
        "su", "kien", "tim", "kiem",
        "cho", "toi", "muon", "tham",
        "gia", "cac", "tai", "trong",
        "ve", "co", "mot", "workshop",
        "lam", "duoc", "nhung", "nay",
        "nao", "giup", "hoc"
    };

    // Ví dụ "làm bánh" -> chỉ giữ "banh".
    var tuQuanTrong = ChuanHoa(chuDeCanTim)
        .Split(' ',
            StringSplitOptions.RemoveEmptyEntries)
        .Where(x =>
            x.Length >= 2 &&
            !tuChung.Contains(x))
        .Distinct()
        .ToList();

    if (tuQuanTrong.Count == 0)
        return 0;

    // Đọc toàn bộ tên, chủ đề và mô tả sự kiện.
    var tuTrongSuKien = ChuanHoa(noiDungSuKien)
        .Split(' ',
            StringSplitOptions.RemoveEmptyEntries)
        .ToHashSet();

    int soTuKhop = tuQuanTrong.Count(
        x => tuTrongSuKien.Contains(x));

    if (soTuKhop == 0)
        return 0;

    double tyLe =
        (double)soTuKhop / tuQuanTrong.Count;

    return Math.Round(25 * tyLe, 2);
}


        private static string ChuanHoaDiaDiem(
            string? value)
        {
            string s = ChuanHoa(value);

            // Đồng nhất các cách gọi TP.HCM.
            s = s.Replace(
                    "thanh pho ho chi minh",
                    "ho chi minh")
                 .Replace("tp ho chi minh",
                    "ho chi minh")
                 .Replace("tp hcm",
                    "ho chi minh")
                 .Replace("tphcm",
                    "ho chi minh")
                 .Replace("sai gon",
                    "ho chi minh");

            // Chỉ thay từ HCM độc lập.
            s = (" " + s + " ")
                .Replace(" hcm ", " ho chi minh ")
                .Trim();

            return s;
        }

        // Chuẩn hóa tiếng Việt và dấu câu để tìm kiếm.
        private static string ChuanHoa(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "";

            string text = value
                .Trim()
                .ToLowerInvariant()
                .Replace('đ', 'd');

            string decomposed = text.Normalize(
                NormalizationForm.FormD);

            var sb = new StringBuilder();

            foreach (char c in decomposed)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c)
                    == UnicodeCategory.NonSpacingMark)
                {
                    continue;
                }

                sb.Append(
                    char.IsLetterOrDigit(c)
                        ? c
                        : ' ');
            }

            return string.Join(
                " ",
                sb.ToString().Split(
                    ' ',
                    StringSplitOptions.RemoveEmptyEntries));
        }
    }
}
