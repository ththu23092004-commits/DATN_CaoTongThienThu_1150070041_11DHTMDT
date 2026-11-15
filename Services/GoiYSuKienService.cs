
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VeSuKienWeb.Data;

namespace VeSuKienWeb.Services
{
    public class ChiTietDiemGoiY
    {
        public int ThuHang { get; set; }
        public int SuKienId { get; set; }
        public string TenSuKien { get; set; } = "";
        public string ChuDe { get; set; } = "";
        public DateTime ThoiGianBatDau { get; set; }

        public double Cosine { get; set; }
        public double DiemChuDe { get; set; }
        public double DiemHinhThuc { get; set; }
        public double DiemDiaDiem { get; set; }
        public double TongDiem { get; set; }

        public double DiemNoiDung { get; set; }
        public string NhomGia { get; set; } = "";
        public string KhungGio { get; set; } = "";
        public int LoaiId { get; set; }
    }

    public class BaoCaoDiemGoiY
    {
        public int NguoiDungId { get; set; }

        public Dictionary<int, double> HoSoChuDe { get; set; }
            = new Dictionary<int, double>();

        public List<ChiTietDiemGoiY> DanhSach { get; set; }
            = new List<ChiTietDiemGoiY>();
    }

    public class GoiYSuKienService
    {
        private readonly NguCanhSuKien _db;

        private const double TrongSoChuDe = 1.0;
        private const double TrongSoLoai = 0.45;
        private const double TrongSoGia = 0.35;
        private const double TrongSoGio = 0.30;

        private sealed class DuLieuSuKien
        {
            public int SuKienId { get; set; }
            public string TenSuKien { get; set; } = "";
            public int LoaiId { get; set; }
            public DateTime ThoiGianBatDau { get; set; }
            public decimal? GiaTu { get; set; }
        }

        public GoiYSuKienService(NguCanhSuKien db)
        {
            _db = db;
        }

        public async Task<List<int>> LayGoiYAsync(
            int nguoiDungId,
            int topN = 6)
        {
            if (topN <= 0)
                return new List<int>();

            var baoCao = await TinhToanAsync(nguoiDungId);

            return baoCao.DanhSach
                .Take(topN)
                .Select(x => x.SuKienId)
                .ToList();
        }

        public Task<BaoCaoDiemGoiY> LayBaoCaoAsync(
            int nguoiDungId)
        {
            return TinhToanAsync(nguoiDungId);
        }

        private static string NhomGia(decimal gia)
        {
            if (gia <= 0)
                return "MienPhi";

            if (gia <= 100000)
                return "Duoi100";

            if (gia <= 300000)
                return "Tu100Den300";

            if (gia <= 700000)
                return "Tu300Den700";

            return "Tren700";
        }

        private static string KhungGio(DateTime thoiGian)
        {
            int gio = thoiGian.Hour;

            if (gio < 12)
                return "Sang";

            if (gio < 18)
                return "Chieu";

            return "Toi";
        }

        private static Dictionary<string, double> TaoDacTrung(
            DuLieuSuKien suKien,
            IReadOnlyDictionary<int, List<int>> chuDeMap,
            bool suDungLoai,
            bool suDungGia,
            bool suDungGio)
        {
            var ketQua = new Dictionary<string, double>();

            if (chuDeMap.TryGetValue(
                suKien.SuKienId,
                out var chuDes))
            {
                foreach (int chuDeId in chuDes.Distinct())
                {
                    ketQua[$"CD:{chuDeId}"] = TrongSoChuDe;
                }
            }

            if (suDungLoai && suKien.LoaiId > 0)
            {
                ketQua[$"LOAI:{suKien.LoaiId}"] =
                    TrongSoLoai;
            }

            if (suDungGia && suKien.GiaTu.HasValue)
            {
                ketQua[$"GIA:{NhomGia(suKien.GiaTu.Value)}"] =
                    TrongSoGia;
            }

            if (suDungGio)
            {
                ketQua[$"GIO:{KhungGio(suKien.ThoiGianBatDau)}"] =
                    TrongSoGio;
            }

            return ketQua;
        }

        private static double TinhCosine(
            IReadOnlyDictionary<string, double> nguoiDung,
            IReadOnlyDictionary<string, double> suKien)
        {
            if (nguoiDung.Count == 0 || suKien.Count == 0)
                return 0;

            double tichVoHuong = 0;

            foreach (var item in suKien)
            {
                if (nguoiDung.TryGetValue(
                    item.Key,
                    out double giaTri))
                {
                    tichVoHuong += giaTri * item.Value;
                }
            }

            double chuanNguoiDung = Math.Sqrt(
                nguoiDung.Values.Sum(x => x * x));

            double chuanSuKien = Math.Sqrt(
                suKien.Values.Sum(x => x * x));

            if (chuanNguoiDung <= 0 || chuanSuKien <= 0)
                return 0;

            double ketQua =
                tichVoHuong / (chuanNguoiDung * chuanSuKien);

            return Math.Clamp(ketQua, 0, 1);
        }

        private async Task<BaoCaoDiemGoiY> TinhToanAsync(
            int nguoiDungId)
        {
            var now = DateTime.Now;

            var baoCao = new BaoCaoDiemGoiY
            {
                NguoiDungId = nguoiDungId
            };

            var suKienKhaDung = await _db.SuKien
                .AsNoTracking()
                .Where(s =>
                    s.TrangThaiDuyet == "DaDuyet" &&
                    s.ThoiGianKetThuc > now)
                .Select(s => new DuLieuSuKien
                {
                    SuKienId = s.SuKienId,
                    TenSuKien = s.TenSuKien,
                    LoaiId = s.LoaiId,
                    ThoiGianBatDau = s.ThoiGianBatDau,
                    GiaTu = s.GiaVeMacDinh
                })
                .ToListAsync();

            if (suKienKhaDung.Count == 0)
                return baoCao;

            var hoSo = await _db.HoSoSoThich
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.NguoiDungId == nguoiDungId);

            var soThich = await _db.NguoiDungSoThich
                .AsNoTracking()
                .Where(x => x.NguoiDungId == nguoiDungId)
                .Select(x => x.ChuDeId)
                .ToListAsync();

            var tuNgay = now.AddDays(-90);

            var lichSuXem = await _db.LichSuXemSuKien
                .AsNoTracking()
                .Where(x =>
                    x.NguoiDungId == nguoiDungId &&
                    x.ThoiGianXem >= tuNgay)
                .Select(x => x.SuKienId)
                .ToListAsync();

            var suKienYeuThich = await _db.SuKienYeuThich
                .AsNoTracking()
                .Where(x =>
                    x.NguoiDungId == nguoiDungId)
                .Select(x => x.SuKienId)
                .Distinct()
                .ToListAsync();

            var veDaThanhToan = await _db.Ve
                .AsNoTracking()
                .Where(v =>
                    v.NguoiDungId == nguoiDungId &&
                    (v.TrangThai == "DaThanhToan" ||
                     v.TrangThai == "DaCheckIn"))
                .Select(v => v.SuKienId)
                .Distinct()
                .ToListAsync();

            var soLuotXem = lichSuXem
                .GroupBy(x => x)
                .ToDictionary(
                    g => g.Key,
                    g => Math.Min(g.Count(), 3));

            var idDaTuongTac = soLuotXem.Keys
                .Concat(suKienYeuThich)
                .Concat(veDaThanhToan)
                .Distinct()
                .ToList();

            var idKhaDung = suKienKhaDung
                .Select(x => x.SuKienId)
                .ToList();

            var tatCaIds = idKhaDung
                .Concat(idDaTuongTac)
                .Distinct()
                .ToList();

            var suKienLichSu = await _db.SuKien
                .AsNoTracking()
                .Where(x => idDaTuongTac.Contains(x.SuKienId))
                .Select(x => new DuLieuSuKien
                {
                    SuKienId = x.SuKienId,
                    TenSuKien = x.TenSuKien,
                    LoaiId = x.LoaiId,
                    ThoiGianBatDau = x.ThoiGianBatDau,
                    GiaTu = x.GiaVeMacDinh
                })
                .ToListAsync();

            var thongTinSuKien = suKienKhaDung
                .Concat(suKienLichSu)
                .GroupBy(x => x.SuKienId)
                .ToDictionary(g => g.Key, g => g.First());

            var lienKetChuDe = await _db.SuKienChuDe
                .AsNoTracking()
                .Where(x => tatCaIds.Contains(x.SuKienId))
                .Select(x => new
                {
                    x.SuKienId,
                    x.ChuDeId
                })
                .ToListAsync();

            var chuDeTheoSuKien = lienKetChuDe
                .GroupBy(x => x.SuKienId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(x => x.ChuDeId)
                          .Distinct()
                          .ToList());

            var tenChuDe = await _db.ChuDeSuKien
                .AsNoTracking()
                .ToDictionaryAsync(
                    x => x.ChuDeId,
                    x => x.TenChuDe);

            var giaVe = await _db.LoaiVe
                .AsNoTracking()
                .Where(x => tatCaIds.Contains(x.SuKienId))
                .Select(x => new
                {
                    x.SuKienId,
                    x.GiaVe
                })
                .ToListAsync();

            var giaThapNhat = giaVe
                .GroupBy(x => x.SuKienId)
                .ToDictionary(
                    g => g.Key,
                    g => (decimal?)g.Min(x => x.GiaVe));

            foreach (var item in thongTinSuKien.Values)
            {
                if (giaThapNhat.TryGetValue(
                    item.SuKienId,
                    out decimal? gia))
                {
                    item.GiaTu = gia;
                }
            }

            var profileChuDe = new Dictionary<int, double>();
            var profileNoiDung = new Dictionary<string, double>();

            void CongDiemChuDe(int chuDeId, double diem)
            {
                if (!profileChuDe.ContainsKey(chuDeId))
                    profileChuDe[chuDeId] = 0;

                profileChuDe[chuDeId] += diem;
            }

            void CongDiemNoiDung(
                string dacTrung,
                double diem)
            {
                if (!profileNoiDung.ContainsKey(dacTrung))
                    profileNoiDung[dacTrung] = 0;

                profileNoiDung[dacTrung] += diem;
            }

            foreach (int chuDeId in soThich.Distinct())
            {
                CongDiemChuDe(chuDeId, 4.0);
                CongDiemNoiDung($"CD:{chuDeId}", 4.0);
            }

            var diemTuongTac = new Dictionary<int, double>();

            void CongDiemSuKien(int suKienId, double diem)
            {
                if (!diemTuongTac.ContainsKey(suKienId))
                    diemTuongTac[suKienId] = 0;

                diemTuongTac[suKienId] += diem;
            }

            foreach (var item in soLuotXem)
                CongDiemSuKien(item.Key, item.Value);

            foreach (int suKienId in suKienYeuThich)
                CongDiemSuKien(suKienId, 3.0);

            foreach (int suKienId in veDaThanhToan)
                CongDiemSuKien(suKienId, 5.0);

            bool coDuLieuLoai = false;
            bool coDuLieuGia = false;
            bool coDuLieuGio = false;

            foreach (var item in diemTuongTac)
            {
                if (!thongTinSuKien.TryGetValue(
                    item.Key,
                    out var suKien))
                {
                    continue;
                }

                if (chuDeTheoSuKien.TryGetValue(
                    item.Key,
                    out var chuDes))
                {
                    foreach (int chuDeId in chuDes)
                    {
                        CongDiemChuDe(chuDeId, item.Value);
                        CongDiemNoiDung(
                            $"CD:{chuDeId}",
                            item.Value);
                    }
                }

                if (suKien.LoaiId > 0)
                {
                    coDuLieuLoai = true;
                    CongDiemNoiDung(
                        $"LOAI:{suKien.LoaiId}",
                        item.Value * TrongSoLoai);
                }

                if (suKien.GiaTu.HasValue)
                {
                    coDuLieuGia = true;
                    CongDiemNoiDung(
                        $"GIA:{NhomGia(suKien.GiaTu.Value)}",
                        item.Value * TrongSoGia);
                }

                coDuLieuGio = true;
                CongDiemNoiDung(
                    $"GIO:{KhungGio(suKien.ThoiGianBatDau)}",
                    item.Value * TrongSoGio);
            }

            baoCao.HoSoChuDe = profileChuDe;

            var diaDiems = await _db.DiaDiem
                .AsNoTracking()
                .Where(x => idKhaDung.Contains(x.SuKienId))
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
                    g => string.Join(" ",
                        g.Select(x =>
                            (x.TenDiaDiem ?? "") + " " +
                            (x.DiaChi ?? ""))));

            var onlineIds = (await _db.SuKienTrucTuyen
                .AsNoTracking()
                .Where(x => idKhaDung.Contains(x.SuKienId))
                .Select(x => x.SuKienId)
                .ToListAsync())
                .ToHashSet();

            var ketQua = suKienKhaDung.Select(s =>
            {
                var dacTrung = TaoDacTrung(
                    s,
                    chuDeTheoSuKien,
                    coDuLieuLoai,
                    coDuLieuGia,
                    coDuLieuGio);

                double cosine = TinhCosine(
                    profileNoiDung,
                    dacTrung);

                double diemNoiDung = cosine * 10.0;

                double diemHinhThuc = 0;
                double diemDiaDiem = 0;

                string hinhThuc =
                    hoSo?.HinhThucUuTien ?? "KhongGioiHan";

                bool coTrucTiep =
                    diaDiemMap.ContainsKey(s.SuKienId);

                bool coTrucTuyen =
                    onlineIds.Contains(s.SuKienId);

                if (hinhThuc == "TrucTiep" && coTrucTiep)
                    diemHinhThuc = 2;

                if (hinhThuc == "TrucTuyen" && coTrucTuyen)
                    diemHinhThuc = 2;



                string? diaDiemUuTien =
                    hoSo?.DiaDiemUuTien?.Trim();

                if (!string.IsNullOrWhiteSpace(diaDiemUuTien) &&
                    diaDiemMap.TryGetValue(
                        s.SuKienId,
                        out string? diaChi) &&
                    diaChi.Contains(
                        diaDiemUuTien,
                        StringComparison.OrdinalIgnoreCase))
                {
                    diemDiaDiem = 1.5;
                }

                double tongDiem =
                    diemNoiDung +
                    diemHinhThuc +
                    diemDiaDiem;

                var chuDes = chuDeTheoSuKien.TryGetValue(
                    s.SuKienId,
                    out var dsChuDe)
                    ? dsChuDe
                    : new List<int>();

                string chuDeText = chuDes.Count == 0
                    ? "Chưa có chủ đề"
                    : string.Join(", ",
                        chuDes.Select(id =>
                            tenChuDe.TryGetValue(
                                id,
                                out string? ten)
                                ? ten
                                : $"Chủ đề #{id}"));

                return new ChiTietDiemGoiY
                {
                    SuKienId = s.SuKienId,
                    TenSuKien = s.TenSuKien,
                    ChuDe = chuDeText,
                    ThoiGianBatDau = s.ThoiGianBatDau,
                    LoaiId = s.LoaiId,
                    NhomGia = s.GiaTu.HasValue
                        ? NhomGia(s.GiaTu.Value)
                        : "ChuaCoGia",
                    KhungGio = KhungGio(s.ThoiGianBatDau),
                    Cosine = cosine,
                    DiemNoiDung = diemNoiDung,
                    DiemChuDe = diemNoiDung,
                    DiemHinhThuc = diemHinhThuc,
                    DiemDiaDiem = diemDiaDiem,
                    TongDiem = tongDiem
                };
            });

            bool coHoSoCaNhan =
                profileNoiDung.Count > 0 ||
                !string.IsNullOrWhiteSpace(
                    hoSo?.DiaDiemUuTien) ||
                (hoSo?.HinhThucUuTien != null &&
                 hoSo.HinhThucUuTien != "KhongGioiHan");

            var danhSach = coHoSoCaNhan
                ? ketQua
                    .OrderByDescending(x => x.TongDiem)
                    .ThenBy(x => x.ThoiGianBatDau)
                    .ThenBy(x => x.SuKienId)
                    .ToList()
                : ketQua
                    .OrderBy(x => x.ThoiGianBatDau)
                    .ThenBy(x => x.SuKienId)
                    .ToList();

            for (int i = 0; i < danhSach.Count; i++)
            {
                danhSach[i].ThuHang = i + 1;
            }

            baoCao.DanhSach = danhSach;

            return baoCao;
        }
    }
}
