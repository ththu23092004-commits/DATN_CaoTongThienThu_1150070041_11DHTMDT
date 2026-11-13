
using System;
using System.Collections.Generic;
using System.Linq;

namespace VeSuKienWeb.Services
{
    // Kết quả đánh giá chất lượng danh sách Top-K.
    public class KetQuaDanhGiaGoiY
    {
        public int K { get; set; }

        public int SoSuKienDuocGoiY { get; set; }

        public int SoSuKienPhuHopTrongTopK { get; set; }

        public int TongSuKienPhuHop { get; set; }

        public double PrecisionAtK { get; set; }

        public double RecallAtK { get; set; }

        public double NdcgAtK { get; set; }

        public double DcgAtK { get; set; }

        public double IdcgAtK { get; set; }
    }

    // Dịch vụ chỉ thực hiện tính toán.
    // Không đọc, ghi hay chỉnh sửa cơ sở dữ liệu.
    public class DanhGiaGoiYService
    {
        /// <summary>
        /// Tính Precision@K, Recall@K và NDCG@K.
        ///
        /// danhSachXepHang:
        ///   ID sự kiện theo đúng thứ tự thuật toán gợi ý.
        ///
        /// suKienPhuHop:
        ///   Tập ID sự kiện được gán nhãn phù hợp từ
        ///   dữ liệu đánh giá độc lập.
        ///
        /// tapUngVien:
        ///   Toàn bộ ID sự kiện thuộc tập đánh giá.
        ///
        /// K:
        ///   Số vị trí đầu tiên được đánh giá.
        /// </summary>
        public KetQuaDanhGiaGoiY TinhDanhGia(
            IReadOnlyList<int> danhSachXepHang,
            IReadOnlyCollection<int> suKienPhuHop,
            IReadOnlyCollection<int> tapUngVien,
            int k = 6)
        {
            if (danhSachXepHang == null)
                throw new ArgumentNullException(
                    nameof(danhSachXepHang));

            if (suKienPhuHop == null)
                throw new ArgumentNullException(
                    nameof(suKienPhuHop));

            if (tapUngVien == null)
                throw new ArgumentNullException(
                    nameof(tapUngVien));

            if (k <= 0)
                throw new ArgumentOutOfRangeException(
                    nameof(k),
                    "K phải lớn hơn 0.");

            var ungVien = tapUngVien.ToHashSet();
            var phuHop = suKienPhuHop.ToHashSet();

            if (ungVien.Count == 0)
                throw new ArgumentException(
                    "Tập ứng viên đánh giá không được rỗng.");

            if (!phuHop.IsSubsetOf(ungVien))
                throw new ArgumentException(
                    "Sự kiện được gán nhãn phù hợp " +
                    "phải thuộc tập ứng viên đánh giá.");

            if (danhSachXepHang.Count !=
                danhSachXepHang.Distinct().Count())
            {
                throw new ArgumentException(
                    "Danh sách xếp hạng không được trùng ID.");
            }

            if (danhSachXepHang.Any(id =>
                !ungVien.Contains(id)))
            {
                throw new ArgumentException(
                    "Danh sách xếp hạng có sự kiện " +
                    "ngoài tập ứng viên đánh giá.");
            }

            // Lấy K sự kiện đứng đầu.
            var topK = danhSachXepHang
                .Take(k)
                .ToList();

            // Số sự kiện thực sự phù hợp trong Top-K.
            int soPhuHop = topK.Count(
                id => phuHop.Contains(id));

            // Precision@K = số phù hợp trong Top-K / K.
            // Dùng K cố định để so sánh giữa các lần thử.
            double precision = (double)soPhuHop / k;

            // Recall@K = số phù hợp trong Top-K
            // chia cho tổng số sự kiện phù hợp.
            // Trường hợp không có nhãn dương:
            // quy ước kết quả bằng 0 và cần
            // xử lý riêng khi tổng hợp thực nghiệm.
            double recall = phuHop.Count > 0
                ? (double)soPhuHop / phuHop.Count
                : 0;

            // DCG@K với mức liên quan nhị phân:
            // phù hợp = 1, không phù hợp = 0.
            double dcg = 0;

            for (int i = 0; i < topK.Count; i++)
            {
                int relevance = phuHop.Contains(topK[i])
                    ? 1
                    : 0;

                dcg += relevance / Math.Log2(i + 2);
            }

            // IDCG@K: thứ tự lý tưởng khi tất cả
            // các sự kiện phù hợp được đặt lên đầu.
            int soViTriLyTuong = Math.Min(
                k,
                phuHop.Count);

            double idcg = 0;

            for (int i = 0; i < soViTriLyTuong; i++)
            {
                idcg += 1.0 / Math.Log2(i + 2);
            }

            // NDCG@K = DCG@K / IDCG@K.
            double ndcg = idcg > 0
                ? dcg / idcg
                : 0;

            return new KetQuaDanhGiaGoiY
            {
                K = k,

                SoSuKienDuocGoiY = topK.Count,

                SoSuKienPhuHopTrongTopK = soPhuHop,

                TongSuKienPhuHop = phuHop.Count,

                PrecisionAtK = precision,

                RecallAtK = recall,

                NdcgAtK = Math.Clamp(ndcg, 0, 1),

                DcgAtK = dcg,

                IdcgAtK = idcg
            };
        }
    }
}

