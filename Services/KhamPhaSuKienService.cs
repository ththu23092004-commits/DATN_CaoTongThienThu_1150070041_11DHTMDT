
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

            int gioiHan = Math.Clamp(gioiHanKetQua ?? 3, 1, 3);
            DateTime hienTai = DateTime.Now;

            string chuDeCanTim = ChuanHoa(nhuCau.ChuDe);
            string diaDiemCanTim = ChuanHoaDiaDiem(nhuCau.DiaDiem);
            string tuKhoa = ChuanHoa(nhuCau.TuKhoa);

            bool timTenRieng = LaTruyVanTenRieng(nhuCau.TuKhoa);

            if (!timTenRieng && LaTruyVanTenRieng(nhuCau.ChuDe))
            {
                timTenRieng = true;

                if (tuKhoa.Length == 0)
                {
                    tuKhoa = chuDeCanTim;
                }

                if (chuDeCanTim == tuKhoa)
                {
                    chuDeCanTim = "";
                }
            }

            if (timTenRieng && chuDeCanTim == tuKhoa)
            {
                chuDeCanTim = "";
            }

            DateTime? tuNgay = DocNgay(nhuCau.TuNgay);
            DateTime? denNgay = DocNgay(nhuCau.DenNgay);

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
                    "Hiện chưa có sự kiện phù hợp để khám phá. " +
                    "Bạn có thể thử lại khi hệ thống có sự kiện mới.";

                return ketQua;
            }

            var ids = suKiens
                .Select(x => x.SuKienId)
                .ToList();

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
                             .Where(x => !string.IsNullOrWhiteSpace(x))
                             .Distinct()),

                        DiaChi = string.Join("; ",
                            g.Select(x => x.DiaChi)
                             .Where(x => !string.IsNullOrWhiteSpace(x))
                             .Distinct())
                    });

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
                        g.Select(x => x.TenChuDe).Distinct()));

            var trucTuyenIds = (await _db.SuKienTrucTuyen
                .AsNoTracking()
                .Where(x => ids.Contains(x.SuKienId))
                .Select(x => x.SuKienId)
                .ToListAsync())
                .ToHashSet();

            var trucTiepIds = diaDiems
                .Select(x => x.SuKienId)
                .ToHashSet();

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

            var daBanMap = daBan.ToDictionary(
                x => x.LoaiVeId,
                x => x.SoLuongDaBan);

            var loaiVeMap = loaiVes
                .GroupBy(x => x.SuKienId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var diemCaNhan = new Dictionary<int, double>();

            if (nguoiDungId.HasValue && nguoiDungId.Value > 0)
            {
                var baoCao = await _goiY.LayBaoCaoAsync(
                    nguoiDungId.Value);

                diemCaNhan = baoCao.DanhSach
                    .ToDictionary(
                        x => x.SuKienId,
                        x => Math.Clamp(x.Cosine, 0, 1));
            }

            var chinhXac = new List<SuKienPhuHop>();
            var ganDung = new List<SuKienPhuHop>();
            var duPhong = new List<SuKienPhuHop>();

            foreach (var sk in suKiens)
            {
                bool coTrucTiep = trucTiepIds.Contains(sk.SuKienId);
                bool coTrucTuyen = trucTuyenIds.Contains(sk.SuKienId);

                bool khopHinhThuc =
                    string.IsNullOrWhiteSpace(nhuCau.HinhThuc) ||
                    (nhuCau.HinhThuc == "TrucTiep" && coTrucTiep) ||
                    (nhuCau.HinhThuc == "TrucTuyen" && coTrucTuyen) ||
                    (nhuCau.HinhThuc == "Hybrid" &&
                     coTrucTiep && coTrucTuyen);

                bool khopThoiGian =
                    (!tuNgay.HasValue ||
                     sk.ThoiGianKetThuc.Date >= tuNgay.Value.Date) &&
                    (!denNgay.HasValue ||
                     sk.ThoiGianBatDau.Date <= denNgay.Value.Date);

                string tenDiaDiem = "";
                string diaChi = "";

                if (diaDiemMap.TryGetValue(sk.SuKienId, out var dd))
                {
                    tenDiaDiem = dd.Ten;
                    diaChi = dd.DiaChi;
                }

                string diaDiemDayDu =
                    $"{tenDiaDiem} {diaChi}".Trim();

                bool khopDiaDiem =
                    diaDiemCanTim.Length == 0 ||
                    ChuaCumTu(
                        ChuanHoaDiaDiem(diaDiemDayDu),
                        diaDiemCanTim);

                string chuDe = chuDeMap.GetValueOrDefault(
                    sk.SuKienId, "");

                string tenChuanHoa = ChuanHoa(sk.TenSuKien);
                string chuDeChuanHoa = ChuanHoa(chuDe);
                string moTaChuanHoa = ChuanHoa(sk.MoTaChiTiet);

                string noiDungTimKiem = ChuanHoa(
                    $"{sk.TenSuKien} {chuDe} {sk.MoTaChiTiet}");

                bool coChuDe = chuDeCanTim.Length > 0;
                bool coTuKhoa = tuKhoa.Length > 0;

                bool khopChuDeChinhXac =
                    !coChuDe ||
                    ChuaCumTu(noiDungTimKiem, chuDeCanTim);

                double diemChuDe = coChuDe
                    ? TinhDiemLienQuan(
                        chuDeCanTim,
                        noiDungTimKiem)
                    : 0;

                bool khopTuKhoaChinhXac =
                    !coTuKhoa ||
                    ChuaCumTu(noiDungTimKiem, tuKhoa);

                double diemTuKhoa = coTuKhoa
                    ? TinhDiemLienQuan(
                        tuKhoa,
                        noiDungTimKiem)
                    : 0;

                bool khopTuKhoaGanDung =
                    coTuKhoa &&
                    diemTuKhoa > 0;

                bool khopChuDeGanDung =
                    coChuDe &&
                    diemChuDe >= 12.5;

                decimal? giaThapNhat = null;

                if (loaiVeMap.TryGetValue(
                    sk.SuKienId, out var cacLoaiVe))
                {
                    var veConSoLuong = cacLoaiVe
                        .Where(v =>
                            v.SoLuong >
                            daBanMap.GetValueOrDefault(v.LoaiVeId))
                        .ToList();

                    if (veConSoLuong.Count == 0)
                    {
                        continue;
                    }

                    giaThapNhat = veConSoLuong.Min(v => v.GiaVe);
                }
                else
                {
                    giaThapNhat = sk.GiaVeMacDinh;
                }

                bool khopNganSach =
                    !nhuCau.NganSachToiDa.HasValue ||
                    (giaThapNhat.HasValue &&
                     giaThapNhat.Value <=
                     nhuCau.NganSachToiDa.Value);

                bool khopNoiDungChinhXac =
                    khopChuDeChinhXac &&
                    khopTuKhoaChinhXac;

                bool khopTatCa =
                    khopNoiDungChinhXac &&
                    khopDiaDiem &&
                    khopThoiGian &&
                    khopHinhThuc &&
                    khopNganSach;

                bool coLienQuan;

                if (timTenRieng)
                {
                    coLienQuan =
                        khopTuKhoaChinhXac ||
                        khopTuKhoaGanDung;
                }
                else
                {
                    coLienQuan =
                        (coChuDe &&
                         (khopChuDeChinhXac ||
                          khopChuDeGanDung)) ||
                        (coTuKhoa &&
                         (khopTuKhoaChinhXac ||
                          khopTuKhoaGanDung)) ||
                        (!coChuDe && !coTuKhoa &&
                         (khopDiaDiem ||
                          khopThoiGian ||
                          khopHinhThuc ||
                          khopNganSach));
                }

                var dieuKienChuaKhop = new List<string>();
                var lyDo = new List<string>();

                double diemNoiDung = 0;

                if (coChuDe)
                {
                    if (khopChuDeChinhXac)
                    {
                        diemNoiDung += 50;
                        lyDo.Add("Khớp chủ đề/nội dung");

                        if (ChuaCumTu(tenChuanHoa, chuDeCanTim))
                        {
                            diemNoiDung += 20;
                        }
                        else if (ChuaCumTu(
                            chuDeChuanHoa,
                            chuDeCanTim))
                        {
                            diemNoiDung += 15;
                        }
                        else if (ChuaCumTu(
                            moTaChuanHoa,
                            chuDeCanTim))
                        {
                            diemNoiDung += 10;
                        }
                    }
                    else
                    {
                        diemNoiDung += Math.Min(diemChuDe, 25);
                        dieuKienChuaKhop.Add(
                            "Chủ đề chỉ liên quan một phần");
                    }
                }

                if (coTuKhoa)
                {
                    if (khopTuKhoaChinhXac)
                    {
                        diemNoiDung += timTenRieng ? 60 : 8;

                        lyDo.Add(
                            timTenRieng
                                ? "Có đầy đủ tên được tìm kiếm trong nội dung sự kiện"
                                : "Có từ khóa phù hợp");
                    }
                    else if (khopTuKhoaGanDung)
                    {
                        diemNoiDung += Math.Min(diemTuKhoa, 20);

                        dieuKienChuaKhop.Add(
                            timTenRieng
                                ? "Chỉ trùng một phần tên, chưa xác nhận đúng nghệ sĩ"
                                : "Từ khóa chỉ trùng một phần");
                    }
                    else
                    {
                        dieuKienChuaKhop.Add(
                            timTenRieng
                                ? "Không tìm thấy tên nghệ sĩ được yêu cầu trong nội dung"
                                : "Không khớp từ khóa");
                    }
                }

                if (diaDiemCanTim.Length > 0)
                {
                    if (khopDiaDiem)
                    {
                        diemNoiDung += 15;
                        lyDo.Add("Đúng địa điểm");
                    }
                    else
                    {
                        dieuKienChuaKhop.Add(
                            "Khác địa điểm yêu cầu");
                    }
                }

                if (tuNgay.HasValue || denNgay.HasValue)
                {
                    if (khopThoiGian)
                    {
                        diemNoiDung += 10;
                        lyDo.Add("Đúng thời gian");
                    }
                    else
                    {
                        dieuKienChuaKhop.Add(
                            "Ngoài khoảng thời gian yêu cầu");
                    }
                }

                if (nhuCau.NganSachToiDa.HasValue)
                {
                    if (khopNganSach)
                    {
                        diemNoiDung += 10;
                        lyDo.Add("Trong ngân sách");
                    }
                    else
                    {
                        dieuKienChuaKhop.Add(
                            "Vượt ngân sách yêu cầu");
                    }
                }

                if (!string.IsNullOrWhiteSpace(nhuCau.HinhThuc))
                {
                    if (khopHinhThuc)
                    {
                        diemNoiDung += 5;
                        lyDo.Add("Đúng hình thức");
                    }
                    else
                    {
                        dieuKienChuaKhop.Add(
                            "Khác hình thức tổ chức");
                    }
                }

                double cosine = Math.Clamp(
                    diemCaNhan.GetValueOrDefault(sk.SuKienId, 0),
                    0,
                    1);

                var item = new SuKienPhuHop
                {
                    SuKienId = sk.SuKienId,
                    TenSuKien = sk.TenSuKien,
                    AnhSuKien = sk.AnhSuKien,
                    MoTaChiTiet = sk.MoTaChiTiet,
                    ThoiGianBatDau = sk.ThoiGianBatDau,
                    ThoiGianKetThuc = sk.ThoiGianKetThuc,
                    TenDiaDiem = tenDiaDiem,
                    DiaChi = diaChi,

                    DiaDiem = diaDiemDayDu.Length > 0
                        ? diaDiemDayDu
                        : (coTrucTuyen
                            ? "Trực tuyến"
                            : "Chưa cập nhật"),

                    ChuDe = chuDe,
                    GiaVeThapNhat = giaThapNhat,
                    DiemNoiDung = diemNoiDung,
                    DiemCosine = cosine,
                    DiemPhuHop = diemNoiDung + cosine * 10,
                    LaGoiYMoRong = !khopTatCa,
                    DieuKienChuaKhop = string.Join(
                        "; ",
                        dieuKienChuaKhop),
                    LyDoPhuHop = lyDo.Count > 0
                        ? string.Join("; ", lyDo)
                        : "Sự kiện đang được mở bán"
                };

                if (khopTatCa)
                {
                    chinhXac.Add(item);
                }
                else if (coLienQuan)
                {
                    if (timTenRieng &&
                        !khopTuKhoaChinhXac &&
                        !khopTuKhoaGanDung)
                    {
                        duPhong.Add(item);
                    }
                    else
                    {
                        item.LaGoiYMoRong = true;
                        ganDung.Add(item);
                    }
                }
                else
                {
                    duPhong.Add(item);
                }
            }

            var danhSach = new List<SuKienPhuHop>();

            void ThemKetQua(IEnumerable<SuKienPhuHop> danhSachNguon)
            {
                foreach (var item in danhSachNguon)
                {
                    if (danhSach.Count >= gioiHan)
                    {
                        break;
                    }

                    if (danhSach.All(
                        x => x.SuKienId != item.SuKienId))
                    {
                        danhSach.Add(item);
                    }
                }
            }

            var xepHangChinhXac = chinhXac
                .OrderByDescending(x => x.DiemNoiDung)
                .ThenByDescending(x => x.DiemCosine)
                .ThenBy(x => x.ThoiGianBatDau)
                .ThenBy(x => x.SuKienId);

            ThemKetQua(xepHangChinhXac);

            var xepHangGanDung = ganDung
                .OrderByDescending(x => x.DiemNoiDung)
                .ThenByDescending(x => x.DiemCosine)
                .ThenBy(x => x.ThoiGianBatDau)
                .ThenBy(x => x.SuKienId);

            ThemKetQua(xepHangGanDung);

            int soGoiYDuPhong = 0;

            if (danhSach.Count < 2)
            {
                int soCanThem = Math.Min(
                    2 - danhSach.Count,
                    gioiHan - danhSach.Count);

                bool coHoSoCaNhan =
                    diemCaNhan.Values.Any(x => x > 0);

                var xepHangDuPhong = duPhong
                    .Where(x =>
                        !danhSach.Any(
                            y => y.SuKienId == x.SuKienId))
                    .OrderByDescending(x =>
                        coHoSoCaNhan ? x.DiemCosine : 0)
                    .ThenBy(x => x.ThoiGianBatDau)
                    .ThenBy(x => x.SuKienId)
                    .Take(soCanThem)
                    .ToList();

                foreach (var item in xepHangDuPhong)
                {
                    item.LaGoiYMoRong = true;

                    item.DieuKienChuaKhop =
                        coHoSoCaNhan
                            ? "Gợi ý dự phòng dựa trên sở thích và lịch sử tương tác; không xác nhận đáp ứng yêu cầu tìm kiếm"
                            : "Gợi ý khám phá sự kiện sắp diễn ra; không xác nhận đáp ứng yêu cầu tìm kiếm";

                    item.LyDoPhuHop =
                        coHoSoCaNhan && item.DiemCosine > 0
                            ? "Được lựa chọn theo mức độ phù hợp với sở thích tài khoản"
                            : "Sự kiện còn hiệu lực, được đề xuất để tham khảo";

                    danhSach.Add(item);
                    soGoiYDuPhong++;
                }
            }

            ketQua.DanhSach = danhSach;

#if DEBUG
            if (nguoiDungId.HasValue)
            {
                Console.WriteLine();
                Console.WriteLine(
                    "===== KIEM TRA COSINE TOP-3 =====");

                foreach (var sk in ketQua.DanhSach)
                {
                    Console.WriteLine(
                        $"SuKienId: {sk.SuKienId} | " +
                        $"Ten: {sk.TenSuKien} | " +
                        $"NoiDung: {sk.DiemNoiDung:F2} | " +
                        $"Cosine: {sk.DiemCosine:F4} | " +
                        $"Tong: {sk.DiemPhuHop:F2} | " +
                        $"MoRong: {sk.LaGoiYMoRong}");
                }

                Console.WriteLine(
                    "===============================");
            }
#endif

            if (ketQua.DanhSach.Count == 0)
            {
                ketQua.ThongBao =
                    "Hiện chưa có sự kiện đang mở bán phù hợp để giới thiệu. " +
                    "Bạn có thể thử một chủ đề khác nhé.";
            }
            else if (chinhXac.Count > 0)
            {
                ketQua.ThongBao =
                    $"Mình tìm được {Math.Min(chinhXac.Count, gioiHan)} " +
                    "sự kiện đáp ứng yêu cầu của bạn.";

                if (ketQua.DanhSach.Any(x => x.LaGoiYMoRong))
                {
                    ketQua.ThongBao +=
                        " Ngoài ra còn có những gợi ý liên quan để tham khảo.";
                }
            }
            else if (ganDung.Count > 0)
            {
                ketQua.ThongBao =
                    "Mình chưa tìm thấy sự kiện đáp ứng đầy đủ yêu cầu, " +
                    "nhưng có một số kết quả gần đúng để bạn tham khảo. " +
                    "Các kết quả này chưa được xác nhận khớp hoàn toàn.";
            }
            else if (soGoiYDuPhong > 0)
            {
                ketQua.ThongBao =
                    "Mình chưa tìm thấy sự kiện đáp ứng đúng yêu cầu. " +
                    "Dưới đây là một vài sự kiện khác có thể phù hợp " +
                    "với sở thích của bạn hoặc đáng để khám phá.";
            }
            else
            {
                ketQua.ThongBao =
                    "Mình chưa tìm thấy sự kiện đáp ứng đầy đủ yêu cầu.";
            }

            return ketQua;
        }

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

        private static bool LaTruyVanTenRieng(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            string text = value.Trim();
            string chuanHoa = ChuanHoa(text);

            string[] chuDeChung =
            {
                "su kien",
                "am nhac",
                "san khau",
                "workshop",
                "cong nghe",
                "thu cong",
                "lam banh",
                "nghe nghiep",
                "ca nhac",
                "hoa nhac",
                "hoi thao",
                "truc tiep",
                "truc tuyen"
            };

            if (chuDeChung.Any(x =>
                ChuaCumTu(chuanHoa, x)))
            {
                return false;
            }

            var words = text.Split(
                ' ',
                StringSplitOptions.RemoveEmptyEntries);

            if (words.Length < 2 || words.Length > 7)
            {
                return false;
            }

            int soTuVietHoa = words.Count(
                x => x.Length > 0 && char.IsUpper(x[0]));

            bool coTuVietHoaToanBo = words.Any(
                x => x.Length >= 2 &&
                     x.All(c =>
                         !char.IsLetter(c) ||
                         char.IsUpper(c)));

            return soTuVietHoa >= 2 ||
                   coTuVietHoaToanBo;
        }

        private static bool ChuaCumTu(
            string noiDung,
            string cumTu)
        {
            if (string.IsNullOrWhiteSpace(cumTu))
            {
                return true;
            }

            string a = " " + ChuanHoa(noiDung) + " ";
            string b = " " + ChuanHoa(cumTu) + " ";

            return a.Contains(b, StringComparison.Ordinal);
        }

        private static double TinhDiemLienQuan(
            string tuKhoa,
            string noiDungSuKien)
        {
            var tuChung = new HashSet<string>
            {
                "su", "kien", "tim", "kiem",
                "cho", "toi", "muon", "tham",
                "gia", "cac", "tai", "trong",
                "ve", "co", "mot", "workshop",
                "lam", "duoc", "nhung", "nay",
                "nao", "giup", "hoc", "nghe",
                "si", "dien", "vien", "ca",
                "hat", "ten", "nao"
            };

            var tuQuanTrong = ChuanHoa(tuKhoa)
                .Split(
                    ' ',
                    StringSplitOptions.RemoveEmptyEntries)
                .Where(x =>
                    x.Length >= 2 &&
                    !tuChung.Contains(x))
                .Distinct()
                .ToList();

            if (tuQuanTrong.Count == 0)
            {
                return 0;
            }

            var tuTrongSuKien = ChuanHoa(noiDungSuKien)
                .Split(
                    ' ',
                    StringSplitOptions.RemoveEmptyEntries)
                .ToHashSet();

            int soTuKhop = tuQuanTrong.Count(
                x => tuTrongSuKien.Contains(x));

            if (soTuKhop == 0)
            {
                return 0;
            }

            double tyLe =
                (double)soTuKhop / tuQuanTrong.Count;

            return Math.Round(25 * tyLe, 2);
        }

        private static string ChuanHoaDiaDiem(string? value)
        {
            string s = ChuanHoa(value);

            s = s.Replace(
                    "thanh pho ho chi minh",
                    "ho chi minh")
                 .Replace(
                    "tp ho chi minh",
                    "ho chi minh")
                 .Replace(
                    "tp hcm",
                    "ho chi minh")
                 .Replace(
                    "tphcm",
                    "ho chi minh")
                 .Replace(
                    "sai gon",
                    "ho chi minh");

            return (" " + s + " ")
                .Replace(" hcm ", " ho chi minh ")
                .Trim();
        }

        private static string ChuanHoa(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "";
            }

            string text = value
                .Trim()
                .ToLowerInvariant()
                .Replace('đ', 'd');

            string decomposed = text.Normalize(
                NormalizationForm.FormD);

            var sb = new StringBuilder();

            foreach (char c in decomposed)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) ==
                    UnicodeCategory.NonSpacingMark)
                {
                    continue;
                }

                sb.Append(
                    char.IsLetterOrDigit(c) ? c : ' ');
            }

            return string.Join(
                " ",
                sb.ToString().Split(
                    ' ',
                    StringSplitOptions.RemoveEmptyEntries));
        }
    }
}
