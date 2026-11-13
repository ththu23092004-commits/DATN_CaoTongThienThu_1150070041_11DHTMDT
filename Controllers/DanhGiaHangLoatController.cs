
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using VeSuKienWeb.Services;

namespace VeSuKienWeb.Controllers
{
    // Kết quả của một người dùng tại một giá trị K.
    public class ChiSoDanhGiaHangLoat
    {
        public int K { get; set; }
        public double Precision { get; set; }
        public double Recall { get; set; }
        public double Ndcg { get; set; }
    }

    // Kết quả đánh giá từng người dùng.
    public class KetQuaNguoiDungHangLoat
    {
        public int NguoiDungId { get; set; }
        public int SoUngVien { get; set; }
        public int SoSuKienPhuHop { get; set; }

        public List<ChiSoDanhGiaHangLoat> ChiSo
            { get; set; } = new();
    }

    [Authorize(Roles = "Admin")]
    public class DanhGiaHangLoatController : Controller
    {
        private readonly GoiYSuKienService _goiYService;
        private readonly DanhGiaGoiYService _danhGiaService;
        private readonly IWebHostEnvironment _environment;

        // Quy định trước: loại sự kiện test #62
        // khỏi tập đánh giá ngoại tuyến.
        // Không xóa hoặc sửa sự kiện trong database.
        private static readonly HashSet<int> SuKienTest =
            new() { 62 };

        private static readonly int[] DanhSachK =
            { 3, 5, 6 };

        public DanhGiaHangLoatController(
            GoiYSuKienService goiYService,
            IWebHostEnvironment environment)
        {
            _goiYService = goiYService;
            _environment = environment;
            _danhGiaService = new DanhGiaGoiYService();
        }

        [HttpGet("/kiem-thu-chi-so/hang-loat")]
        public async Task<IActionResult> HangLoat()
        {
            // 1. Xác định đường dẫn CSV.
            string duongDan = Path.Combine(
                _environment.ContentRootPath,
                "DuLieuDanhGia",
                "NhanDanhGiaGoiY.csv"
            );

            if (!System.IO.File.Exists(duongDan))
            {
                return NotFound(new
                {
                    thongBao = "Khong tim thay file CSV.",
                    tenFile = "NhanDanhGiaGoiY.csv"
                });
            }

            string[] cacDong;

            try
            {
                cacDong = await System.IO.File
                    .ReadAllLinesAsync(duongDan);
            }
            catch (IOException)
            {
                return StatusCode(500, new
                {
                    thongBao = "Khong doc duoc file CSV."
                });
            }
            catch (UnauthorizedAccessException)
            {
                return StatusCode(500, new
                {
                    thongBao = "Khong co quyen doc file CSV."
                });
            }

            if (cacDong.Length < 2)
            {
                return BadRequest(new
                {
                    thongBao = "File CSV chua co du lieu."
                });
            }

            string tieuDe = cacDong[0]
                .Trim()
                .TrimStart('\uFEFF');

            if (tieuDe !=
                "NguoiDungId,SuKienId,MucDoPhuHop")
            {
                return BadRequest(new
                {
                    thongBao = "Sai tieu de file CSV.",
                    tieuDeYeuCau =
                        "NguoiDungId,SuKienId,MucDoPhuHop"
                });
            }

            // 2. Đọc nhãn đánh giá theo từng người dùng.
            var nhanTheoNguoiDung =
                new Dictionary<int, Dictionary<int, int>>();

            var loiCsv = new List<string>();

            for (int i = 1; i < cacDong.Length; i++)
            {
                string dong = cacDong[i].Trim();

                if (string.IsNullOrWhiteSpace(dong))
                    continue;

                string[] cot = dong.Split(',');

                if (cot.Length != 3 ||
                    !int.TryParse(
                        cot[0].Trim(), out int nguoiDungId) ||
                    !int.TryParse(
                        cot[1].Trim(), out int suKienId) ||
                    nguoiDungId <= 0 ||
                    suKienId <= 0)
                {
                    loiCsv.Add(
                        $"Dong {i + 1}: sai dinh dang ID.");
                    continue;
                }

                // Sự kiện #62 là dữ liệu test,
                // có thể có nhãn trống.
                if (SuKienTest.Contains(suKienId))
                    continue;

                if (!int.TryParse(
                        cot[2].Trim(), out int mucDo) ||
                    mucDo < 0 || mucDo > 2)
                {
                    loiCsv.Add(
                        $"Dong {i + 1}: nhan phai la 0, 1, 2.");
                    continue;
                }

                if (!nhanTheoNguoiDung.TryGetValue(
                        nguoiDungId, out var nhan))
                {
                    nhan = new Dictionary<int, int>();
                    nhanTheoNguoiDung[nguoiDungId] = nhan;
                }

                if (!nhan.TryAdd(suKienId, mucDo))
                {
                    loiCsv.Add(
                        $"Dong {i + 1}: trung nhan " +
                        $"nguoi dung {nguoiDungId}, " +
                        $"su kien {suKienId}.");
                }
            }

            if (loiCsv.Count > 0)
            {
                return BadRequest(new
                {
                    thongBao = "CSV co du lieu khong hop le.",
                    loiCsv
                });
            }

            if (nhanTheoNguoiDung.Count == 0)
            {
                return BadRequest(new
                {
                    thongBao =
                        "Chua co nguoi dung nao co nhan hop le."
                });
            }

            // 3. Đánh giá từng người dùng.
            var ketQuaNguoiDung =
                new List<KetQuaNguoiDungHangLoat>();

            var nguoiDungBiBoQua =
                new List<object>();

            foreach (var item in nhanTheoNguoiDung
                .OrderBy(x => x.Key))
            {
                int nguoiDungId = item.Key;
                Dictionary<int, int> nhan = item.Value;

                // Dùng thuật toán thật của website.
                var baoCao = await _goiYService
                    .LayBaoCaoAsync(nguoiDungId);

                var danhSach = baoCao.DanhSach
                    .OrderBy(x => x.ThuHang)
                    .Where(x =>
                        !SuKienTest.Contains(x.SuKienId))
                    .ToList();

                if (danhSach.Count == 0)
                {
                    nguoiDungBiBoQua.Add(new
                    {
                        nguoiDungId,
                        lyDo = "Khong co ung vien."
                    });

                    continue;
                }

                // Không tự coi nhãn còn thiếu là 0.
                var thieuNhan = danhSach
                    .Where(x => !nhan.ContainsKey(x.SuKienId))
                    .Select(x => x.SuKienId)
                    .ToList();

                if (thieuNhan.Count > 0)
                {
                    nguoiDungBiBoQua.Add(new
                    {
                        nguoiDungId,
                        lyDo = "Chua gan nhan cho tat ca ung vien.",
                        thieuNhan
                    });

                    continue;
                }

                var tapUngVien = danhSach
                    .Select(x => x.SuKienId)
                    .ToList();

                // Nhãn 1 và 2 được xem là phù hợp.
                // NDCG hiện dùng nhãn nhị phân.
                var suKienPhuHop = tapUngVien
                    .Where(id => nhan[id] >= 1)
                    .ToList();

                // Trường hợp không có nhãn phù hợp:
                // bỏ qua thay vì tính Recall/NDCG = 0
                // rồi làm sai lệch trung bình.
                if (suKienPhuHop.Count == 0)
                {
                    nguoiDungBiBoQua.Add(new
                    {
                        nguoiDungId,
                        lyDo =
                            "Khong co nhan phu hop de tinh Recall."
                    });

                    continue;
                }

                var ketQua = new KetQuaNguoiDungHangLoat
                {
                    NguoiDungId = nguoiDungId,
                    SoUngVien = tapUngVien.Count,
                    SoSuKienPhuHop = suKienPhuHop.Count
                };

                foreach (int k in DanhSachK)
                {
                    // Không đánh giá K lớn hơn tập ứng viên.
                    if (k > tapUngVien.Count)
                        continue;

                    var chiSo = _danhGiaService.TinhDanhGia(
                        tapUngVien,
                        suKienPhuHop,
                        tapUngVien,
                        k
                    );

                    ketQua.ChiSo.Add(
                        new ChiSoDanhGiaHangLoat
                        {
                            K = k,
                            Precision = chiSo.PrecisionAtK,
                            Recall = chiSo.RecallAtK,
                            Ndcg = chiSo.NdcgAtK
                        });
                }

                if (ketQua.ChiSo.Count == 0)
                {
                    nguoiDungBiBoQua.Add(new
                    {
                        nguoiDungId,
                        lyDo =
                            "So ung vien it hon K nho nhat."
                    });

                    continue;
                }

                ketQuaNguoiDung.Add(ketQua);
            }

            // 4. Tổng hợp trung bình theo từng K.
            // Trung bình cộng trên các người dùng hợp lệ.
            // Mỗi K có thể có số mẫu khác nhau.
            var trungBinhTheoK = DanhSachK
                .Select(k =>
                {
                    var cacKetQua = ketQuaNguoiDung
                        .SelectMany(x => x.ChiSo)
                        .Where(x => x.K == k)
                        .ToList();

                    return new
                    {
                        k,
                        soNguoiDung = cacKetQua.Count,

                        precisionTrungBinh =
                            cacKetQua.Count > 0
                                ? (double?)cacKetQua.Average(
                                    x => x.Precision)
                                : null,

                        recallTrungBinh =
                            cacKetQua.Count > 0
                                ? (double?)cacKetQua.Average(
                                    x => x.Recall)
                                : null,

                        ndcgTrungBinh =
                            cacKetQua.Count > 0
                                ? (double?)cacKetQua.Average(
                                    x => x.Ndcg)
                                : null
                    };
                })
                .ToList();

            // 5. Chỉ trả dữ liệu, không lưu xuống DB.
            return Json(new
            {
                loaiKiemThu =
                    "DanhGiaHangLoatNgoaiTuyen",

                nguonXepHang =
                    "GoiYSuKienService.LayBaoCaoAsync",

                nguonNhan =
                    "DuLieuDanhGia/NhanDanhGiaGoiY.csv",

                loaiNhan =
                    "Nhan mo phong - chua phai khao sat thuc",

                soNguoiDungCoNhan =
                    nhanTheoNguoiDung.Count,

                soNguoiDungDuocDanhGia =
                    ketQuaNguoiDung.Count,

                soNguoiDungBiBoQua =
                    nguoiDungBiBoQua.Count,

                trungBinhTheoK,

                ketQuaNguoiDung,

                nguoiDungBiBoQua,

                suKienTestDaLoai =
                    SuKienTest.OrderBy(x => x).ToList(),

                ghiChu =
                    "Cong cu chi tinh toan ngoai tuyen. " +
                    "Ket qua trung binh khi chi co 1 nguoi dung " +
                    "khong phai bang chung chat luong cho nhieu " +
                    "nguoi dung. Nhan hien tai la mo phong " +
                    "va co nguy co thien lech."
            });
        }
    }
}

