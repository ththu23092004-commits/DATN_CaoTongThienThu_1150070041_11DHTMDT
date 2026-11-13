
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
    [Authorize(Roles = "Admin")]
    public class DanhGiaGoiYController : Controller
    {
        private readonly DanhGiaGoiYService _danhGia;
        private readonly GoiYSuKienService _goiYService;
        private readonly IWebHostEnvironment _environment;

        // Chỉ loại khỏi tập đánh giá ngoại tuyến.
        // Không xóa hay thay đổi sự kiện trong SQL Server.
        private static readonly HashSet<int> SuKienTest =
            new HashSet<int> { 62 };

        public DanhGiaGoiYController(
            GoiYSuKienService goiYService,
            IWebHostEnvironment environment)
        {
            _goiYService = goiYService;
            _environment = environment;
            _danhGia = new DanhGiaGoiYService();
        }

        // ==========================================
        // 1. KIỂM THỬ CÔNG THỨC BẰNG DỮ LIỆU MẪU
        // ==========================================

        [HttpGet("/kiem-thu-chi-so/mau")]
        public IActionResult KiemThuMau()
        {
            var tapUngVien = new List<int>
            {
                1, 2, 3, 4, 5, 6, 7, 8
            };

            var danhSachXepHang = new List<int>
            {
                1, 2, 3, 4, 5, 6, 8, 7
            };

            var suKienPhuHop = new List<int>
            {
                1, 3, 5, 7
            };

            const int k = 6;

            var ketQua = _danhGia.TinhDanhGia(
                danhSachXepHang,
                suKienPhuHop,
                tapUngVien,
                k
            );

            double dcgMongDoi =
                1.0 / Math.Log2(2) +
                1.0 / Math.Log2(4) +
                1.0 / Math.Log2(6);

            double idcgMongDoi =
                1.0 / Math.Log2(2) +
                1.0 / Math.Log2(3) +
                1.0 / Math.Log2(4) +
                1.0 / Math.Log2(5);

            double ndcgMongDoi =
                dcgMongDoi / idcgMongDoi;

            const double saiSo = 0.000001;

            bool precisionDung =
                Math.Abs(ketQua.PrecisionAtK - 0.5)
                < saiSo;

            bool recallDung =
                Math.Abs(ketQua.RecallAtK - 0.75)
                < saiSo;

            bool ndcgDung =
                Math.Abs(ketQua.NdcgAtK - ndcgMongDoi)
                < saiSo;

            bool tatCaDung =
                precisionDung && recallDung && ndcgDung;

            return Json(new
            {
                loaiKiemThu =
                    "KiemThuCongThucBangDuLieuMau",

                thongBao = tatCaDung
                    ? "PASS - Ca 3 chi so tinh dung."
                    : "FAIL - Can kiem tra lai cong thuc.",

                tatCaDung,

                duLieuMau = new
                {
                    tapUngVien,
                    danhSachXepHang,
                    suKienPhuHop,
                    k
                },

                ketQua = new
                {
                    precisionAt6 = ketQua.PrecisionAtK,
                    recallAt6 = ketQua.RecallAtK,
                    ndcgAt6 = ketQua.NdcgAtK,
                    dcgAt6 = ketQua.DcgAtK,
                    idcgAt6 = ketQua.IdcgAtK
                },

                kiemTra = new
                {
                    precisionDung,
                    recallDung,
                    ndcgDung
                },

                ghiChu =
                    "Du lieu mau chi de kiem tra cong thuc."
            });
        }

        // ==========================================
        // 2. XEM DANH SÁCH GỢI Ý THẬT
        // ==========================================

        [HttpGet("/kiem-thu-chi-so/danh-sach")]
        public async Task<IActionResult> DanhSach(
            int nguoiDungId = 5,
            int k = 6)
        {
            if (nguoiDungId <= 0 || k <= 0 || k > 100)
            {
                return BadRequest(new
                {
                    thongBao =
                        "NguoiDungId hoac K khong hop le."
                });
            }

            var baoCao = await _goiYService
                .LayBaoCaoAsync(nguoiDungId);

            var danhSach = baoCao.DanhSach
                .OrderBy(x => x.ThuHang)
                .ToList();

            var topK = danhSach
                .Take(k)
                .Select(x => new
                {
                    thuHang = x.ThuHang,
                    suKienId = x.SuKienId,
                    tenSuKien = x.TenSuKien,
                    chuDe = x.ChuDe,
                    thoiGianBatDau = x.ThoiGianBatDau,
                    cosine = Math.Round(x.Cosine, 6),
                    diemChuDe =
                        Math.Round(x.DiemChuDe, 4),
                    diemHinhThuc =
                        Math.Round(x.DiemHinhThuc, 4),
                    diemDiaDiem =
                        Math.Round(x.DiemDiaDiem, 4),
                    tongDiem =
                        Math.Round(x.TongDiem, 4)
                })
                .ToList();

            var tapUngVien = danhSach
                .Select(x => x.SuKienId)
                .ToList();

            var ungVienChiTiet = danhSach
                .Select(x => new
                {
                    suKienId = x.SuKienId,
                    tenSuKien = x.TenSuKien,
                    chuDe = x.ChuDe,
                    thoiGianBatDau = x.ThoiGianBatDau
                })
                .OrderBy(x => x.suKienId)
                .ToList();

            return Json(new
            {
                loaiKiemThu = "DanhSachGoiYThucTe",
                nguoiDungId,
                k,
                tongSoUngVien = tapUngVien.Count,
                soSuKienTrongTopK = topK.Count,
                hoSoChuDe = baoCao.HoSoChuDe,
                topK,
                tapUngVien,
                ungVienChiTiet,

                ghiChu = new
                {
                    nguonDuLieu =
                        "GoiYSuKienService.LayBaoCaoAsync",
                    daTinhChiSo = false,
                    thongBao =
                        "Da lay xep hang that; " +
                        "chua su dung nhan danh gia."
                }
            });
        }

        // ==========================================
        // 3. ĐÁNH GIÁ NGOẠI TUYẾN BẰNG CSV
        // ==========================================

        [HttpGet("/kiem-thu-chi-so/thuc-nghiem")]
        public async Task<IActionResult> ThucNghiem(
            int nguoiDungId = 5,
            int k = 3)
        {
            if (nguoiDungId <= 0 || k <= 0 || k > 100)
            {
                return BadRequest(new
                {
                    thongBao =
                        "NguoiDungId hoac K khong hop le."
                });
            }

            // Đọc file nhãn nằm ngoài wwwroot.
            string duongDan = Path.Combine(
                _environment.ContentRootPath,
                "DuLieuDanhGia",
                "NhanDanhGiaGoiY.csv"
            );

            if (!System.IO.File.Exists(duongDan))
            {
                return NotFound(new
                {
                    thongBao =
                        "Khong tim thay file nhan danh gia.",
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
                    thongBao =
                        "Khong the doc file CSV."
                });
            }

            if (cacDong.Length < 2)
            {
                return BadRequest(new
                {
                    thongBao =
                        "File CSV chua co du lieu."
                });
            }

            string header = cacDong[0]
                .Trim()
                .TrimStart('\uFEFF');

            if (header !=
                "NguoiDungId,SuKienId,MucDoPhuHop")
            {
                return BadRequest(new
                {
                    thongBao =
                        "Sai ten cot CSV. Can 3 cot: " +
                        "NguoiDungId,SuKienId,MucDoPhuHop."
                });
            }

            // Mỗi người dùng và sự kiện chỉ có một nhãn.
            var nhan = new Dictionary<int, int>();
            var loiCsv = new List<string>();

            for (int i = 1; i < cacDong.Length; i++)
            {
                string dong = cacDong[i].Trim();

                if (string.IsNullOrWhiteSpace(dong))
                    continue;

                string[] cot = dong.Split(',');

                if (cot.Length != 3 ||
                    !int.TryParse(cot[0].Trim(), out int idNguoiDung) ||
                    !int.TryParse(cot[1].Trim(), out int idSuKien))
                {
                    loiCsv.Add(
                        $"Dong {i + 1}: sai dinh dang.");
                    continue;
                }

                if (idNguoiDung != nguoiDungId)
                    continue;

                // Chỉ bỏ qua sự kiện thử nghiệm đã
                // công khai quy định trước.
                if (SuKienTest.Contains(idSuKien))
                    continue;

                if (!int.TryParse(
                        cot[2].Trim(),
                        out int mucDo) ||
                    mucDo < 0 || mucDo > 2)
                {
                    loiCsv.Add(
                        $"Dong {i + 1}: nhan phai la 0, 1 hoac 2.");
                    continue;
                }

                if (!nhan.TryAdd(idSuKien, mucDo))
                {
                    loiCsv.Add(
                        $"Dong {i + 1}: trung nhan su kien {idSuKien}.");
                }
            }

            if (loiCsv.Count > 0)
            {
                return BadRequest(new
                {
                    thongBao = "File CSV co loi.",
                    loiCsv
                });
            }

            // Lấy danh sách xếp hạng thật, không thay
            // đổi dữ liệu hoặc điểm của thuật toán.
            var baoCao = await _goiYService
                .LayBaoCaoAsync(nguoiDungId);

            // Tập đánh giá ngoại tuyến chỉ gồm
            // sự kiện hợp lệ, không chứa TEST #62.
            var xepHangDanhGia = baoCao.DanhSach
                .OrderBy(x => x.ThuHang)
                .Where(x => !SuKienTest.Contains(x.SuKienId))
                .ToList();

            if (xepHangDanhGia.Count == 0)
            {
                return BadRequest(new
                {
                    thongBao =
                        "Khong co su kien de danh gia."
                });
            }

            // Không âm thầm coi nhãn còn thiếu là 0.
            var thieuNhan = xepHangDanhGia
                .Where(x => !nhan.ContainsKey(x.SuKienId))
                .Select(x => new
                {
                    x.SuKienId,
                    x.TenSuKien
                })
                .ToList();

            if (thieuNhan.Count > 0)
            {
                return BadRequest(new
                {
                    thongBao =
                        "Con su kien chua duoc gan nhan.",
                    thieuNhan
                });
            }

            if (k > xepHangDanhGia.Count)
            {
                return BadRequest(new
                {
                    thongBao =
                        "K lon hon so su kien trong tap danh gia.",
                    k,
                    soUngVien = xepHangDanhGia.Count
                });
            }

            var tapUngVien = xepHangDanhGia
                .Select(x => x.SuKienId)
                .ToList();

            // Nhãn 1 hoặc 2 đều được tính là phù hợp.
            // Ba công thức hiện dùng relevance nhị phân.
            var suKienPhuHop = tapUngVien
                .Where(id => nhan[id] >= 1)
                .ToList();

            var ketQua = _danhGia.TinhDanhGia(
                tapUngVien,
                suKienPhuHop,
                tapUngVien,
                k
            );

            var danhSachDanhGia = xepHangDanhGia
                .Select((x, index) => new
                {
                    thuHangTrongTapDanhGia = index + 1,
                    thuHangGoc = x.ThuHang,
                    suKienId = x.SuKienId,
                    tenSuKien = x.TenSuKien,
                    mucDoPhuHopMoPhong = nhan[x.SuKienId],
                    phuHop = nhan[x.SuKienId] >= 1,
                    tongDiemGoiY =
                        Math.Round(x.TongDiem, 4)
                })
                .ToList();

            return Json(new
            {
                loaiKiemThu =
                    "DanhGiaNgoaiTuyenBangNhanMoPhong",

                nguoiDungId,
                k,

                loaiDuLieu =
                    "Nhan mo phong, khong phai khao sat thuc",

                soUngVien = tapUngVien.Count,
                soSuKienPhuHop = suKienPhuHop.Count,
                suKienTestDaLoai = SuKienTest
                    .OrderBy(x => x)
                    .ToList(),

                danhSachDanhGia,

                ketQua = new
                {
                    precisionAtK = ketQua.PrecisionAtK,
                    recallAtK = ketQua.RecallAtK,
                    ndcgAtK = ketQua.NdcgAtK,
                    soSuKienPhuHopTrongTopK =
                        ketQua.SoSuKienPhuHopTrongTopK
                },

                canhBao =
                    "Day la phep thu ngoai tuyen: " +
                    "loai su kien test, giu thu tu cua " +
                    "cac su kien con lai. Khong phai " +
                    "ket qua Top-K nguyen ban cua website. " +
                    "Nhan duoc mo phong sau khi da xem " +
                    "ho so nguoi dung, nen chua phu hop " +
                    "lam ket luan chat luong thuat toan."
            });
        }
    }
}
