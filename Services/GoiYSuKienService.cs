
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

        public GoiYSuKienService(NguCanhSuKien db)
        {
            _db = db;
        }

        // Hàm Trang chủ và Trang Sự kiện vẫn sử dụng.
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

        // Hàm dùng để kiểm thử nội bộ.
        public Task<BaoCaoDiemGoiY> LayBaoCaoAsync(
            int nguoiDungId)
        {
            return TinhToanAsync(nguoiDungId);
        }

        // Một hàm tính chung để bảng kiểm thử
        // và Top-N không bị lệch thuật toán.
        private async Task<BaoCaoDiemGoiY> TinhToanAsync(
            int nguoiDungId)
        {
            var now = DateTime.Now;

            var baoCao = new BaoCaoDiemGoiY
            {
                NguoiDungId = nguoiDungId
            };

            // 1. Lấy sự kiện còn khả dụng.
            var suKiens = await _db.SuKien
                .AsNoTracking()
                .Where(s =>
                    s.TrangThaiDuyet == "DaDuyet" &&
                    s.ThoiGianKetThuc > now)
                .Select(s => new
                {
                    s.SuKienId,
                    s.TenSuKien,
                    s.ThoiGianBatDau
                })
                .ToListAsync();

            if (suKiens.Count == 0)
                return baoCao;

            var ids = suKiens
                .Select(x => x.SuKienId)
                .ToList();

            // 2. Lấy chủ đề sự kiện.
            var lienKetChuDe = await _db.SuKienChuDe
                .AsNoTracking()
                .Where(x => ids.Contains(x.SuKienId))
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
                          .ToList()
                );

            // 3. Đọc danh sách chủ đề và tên chủ đề.
            var danhSachChuDe = await _db.ChuDeSuKien
                .AsNoTracking()
                .Select(x => new
                {
                    x.ChuDeId,
                    x.TenChuDe
                })
                .ToListAsync();

            var tenChuDe = danhSachChuDe
                .ToDictionary(
                    x => x.ChuDeId,
                    x => x.TenChuDe
                );

            // 4. Khảo sát sở thích.
            var hoSo = await _db.HoSoSoThich
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.NguoiDungId == nguoiDungId);

            var soThich = await _db.NguoiDungSoThich
                .AsNoTracking()
                .Where(x => x.NguoiDungId == nguoiDungId)
                .Select(x => x.ChuDeId)
                .ToListAsync();

            // 5. Lịch sử xem trong 90 ngày.
            var tuNgay = now.AddDays(-90);

            var lichSuXem = await _db.LichSuXemSuKien
                .AsNoTracking()
                .Where(x =>
                    x.NguoiDungId == nguoiDungId &&
                    x.ThoiGianXem >= tuNgay)
                .Select(x => x.SuKienId)
                .ToListAsync();

            // 6. Lịch sử yêu thích.
            var suKienYeuThich = await _db.SuKienYeuThich
                .AsNoTracking()
                .Where(x => x.NguoiDungId == nguoiDungId)
                .Select(x => x.SuKienId)
                .ToListAsync();

            // 7. Các sự kiện có vé ở trạng thái thanh toán
            // mà hệ thống hiện tại đang sử dụng.
            var veDaThanhToan = await _db.Ve
                .AsNoTracking()
                .Where(v =>
                    v.NguoiDungId == nguoiDungId &&
                    (v.TrangThai == "DaThanhToan" ||
                     v.TrangThai == "DaCheckIn"))
                .Select(v => v.SuKienId)
                .Distinct()
                .ToListAsync();

            // 8. Chủ đề của tất cả sự kiện từng tương tác.
            var idDaTuongTac = lichSuXem
                .Concat(suKienYeuThich)
                .Concat(veDaThanhToan)
                .Distinct()
                .ToList();

            var chuDeLichSu = await _db.SuKienChuDe
                .AsNoTracking()
                .Where(x =>
                    idDaTuongTac.Contains(x.SuKienId))
                .Select(x => new
                {
                    x.SuKienId,
                    x.ChuDeId
                })
                .ToListAsync();

            var chuDeLichSuMap = chuDeLichSu
                .GroupBy(x => x.SuKienId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(x => x.ChuDeId)
                          .Distinct()
                          .ToList()
                );

            // 9. Xây dựng vector sở thích.
            var profile = new Dictionary<int, double>();

            void CongDiem(int chuDeId, double diem)
            {
                if (!profile.ContainsKey(chuDeId))
                    profile[chuDeId] = 0;

                profile[chuDeId] += diem;
            }

            // Khảo sát: +4
            foreach (var chuDeId in soThich.Distinct())
            {
                CongDiem(chuDeId, 4.0);
            }

            // Lượt xem: tối đa 3 lượt/sự kiện
            var soLuotXem = lichSuXem
                .GroupBy(x => x)
                .ToDictionary(
                    g => g.Key,
                    g => Math.Min(g.Count(), 3)
                );

            foreach (var item in soLuotXem)
            {
                if (!chuDeLichSuMap.TryGetValue(
                    item.Key, out var chuDes))
                    continue;

                foreach (var chuDeId in chuDes)
                {
                    CongDiem(chuDeId, item.Value);
                }
            }

            // Yêu thích: +3
            foreach (var suKienId in suKienYeuThich)
            {
                if (!chuDeLichSuMap.TryGetValue(
                    suKienId, out var chuDes))
                    continue;

                foreach (var chuDeId in chuDes)
                {
                    CongDiem(chuDeId, 3.0);
                }
            }

            // Vé đã thanh toán: +5
            foreach (var suKienId in veDaThanhToan)
            {
                if (!chuDeLichSuMap.TryGetValue(
                    suKienId, out var chuDes))
                    continue;

                foreach (var chuDeId in chuDes)
                {
                    CongDiem(chuDeId, 5.0);
                }
            }

            baoCao.HoSoChuDe = profile;

            // 10. Tính độ dài vector người dùng.
            double doDaiNguoiDung = Math.Sqrt(
                profile.Values.Sum(x => x * x)
            );

            // 11. Dữ liệu địa điểm.
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
                    g => string.Join(" ",
                        g.Select(x =>
                            (x.TenDiaDiem ?? "") + " " +
                            (x.DiaChi ?? "")))
                );

            // 12. Dữ liệu sự kiện trực tuyến.
            var onlineIds = (await _db.SuKienTrucTuyen
                .AsNoTracking()
                .Where(x => ids.Contains(x.SuKienId))
                .Select(x => x.SuKienId)
                .ToListAsync())
                .ToHashSet();

            // 13. Tính điểm chi tiết từng sự kiện.
            var ketQua = suKiens.Select(s =>
            {
                var chuDes = chuDeTheoSuKien.TryGetValue(
                    s.SuKienId, out var value)
                    ? value
                    : new List<int>();

                // Vector sự kiện nhị phân theo chủ đề.
                double tichVoHuong = chuDes.Sum(id =>
                    profile.GetValueOrDefault(id, 0)
                );

                double doDaiSuKien = Math.Sqrt(
                    chuDes.Count
                );

                double cosine =
                    doDaiNguoiDung > 0 && doDaiSuKien > 0
                        ? tichVoHuong /
                          (doDaiNguoiDung * doDaiSuKien)
                        : 0;

                double diemHinhThuc = 0;
                double diemDiaDiem = 0;

                var hinhThuc =
                    hoSo?.HinhThucUuTien ?? "KhongGioiHan";

                bool coTrucTiep =
                    diaDiemMap.ContainsKey(s.SuKienId);

                bool coTrucTuyen =
                    onlineIds.Contains(s.SuKienId);

                if (hinhThuc == "TrucTiep" && coTrucTiep)
                    diemHinhThuc = 2;

                if (hinhThuc == "TrucTuyen" && coTrucTuyen)
                    diemHinhThuc = 2;

                if (hinhThuc == "Hybrid" &&
                    coTrucTiep && coTrucTuyen)
                    diemHinhThuc = 2;

                var diaDiemUuTien =
                    hoSo?.DiaDiemUuTien?.Trim();

                if (!string.IsNullOrWhiteSpace(diaDiemUuTien) &&
                    diaDiemMap.TryGetValue(
                        s.SuKienId, out var diaChi) &&
                    diaChi.Contains(
                        diaDiemUuTien,
                        StringComparison.OrdinalIgnoreCase))
                {
                    diemDiaDiem = 1.5;
                }

                double diemChuDe = cosine * 10.0;

                double tongDiem =
                    diemChuDe +
                    diemHinhThuc +
                    diemDiaDiem;

                string chuDeText = chuDes.Count == 0
                    ? "Chưa có chủ đề"
                    : string.Join(", ",
                        chuDes.Select(id =>
                            tenChuDe.TryGetValue(
                                id, out var ten)
                                ? ten
                                : $"Chủ đề #{id}")
                    );

                return new ChiTietDiemGoiY
                {
                    SuKienId = s.SuKienId,
                    TenSuKien = s.TenSuKien,
                    ChuDe = chuDeText,
                    ThoiGianBatDau = s.ThoiGianBatDau,
                    Cosine = cosine,
                    DiemChuDe = diemChuDe,
                    DiemHinhThuc = diemHinhThuc,
                    DiemDiaDiem = diemDiaDiem,
                    TongDiem = tongDiem
                };
            });

            // 14. Xếp hạng.
            // Nếu chưa có hồ sơ sở thích:
            // ưu tiên sự kiện sắp diễn ra như code cũ.
            var danhSach = profile.Count == 0
                ? ketQua
                    .OrderBy(x => x.ThoiGianBatDau)
                    .ThenBy(x => x.SuKienId)
                    .ToList()
                : ketQua
                    .OrderByDescending(x => x.TongDiem)
                    .ThenBy(x => x.ThoiGianBatDau)
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
