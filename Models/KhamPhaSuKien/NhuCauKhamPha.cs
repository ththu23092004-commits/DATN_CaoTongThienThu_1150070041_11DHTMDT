
using System;

namespace VeSuKienWeb.Models.KhamPhaSuKien
{
    public class NhuCauKhamPha
    {
        // Chủ đề, ví dụ: Âm nhạc, Công nghệ & AI, Làm bánh
        public string? ChuDe { get; set; }

        // Thành phố hoặc khu vực khách muốn tìm
        public string? DiaDiem { get; set; }

        // Ngân sách tối đa (VND)
        public decimal? NganSachToiDa { get; set; }

        // Khoảng thời gian dưới dạng yyyy-MM-dd
        public string? TuNgay { get; set; }
        public string? DenNgay { get; set; }

        // Trực tiếp, Trực tuyến, Hybrid hoặc không xác định
        public string? HinhThuc { get; set; }

        // Các từ khóa bổ sung
        public string? TuKhoa { get; set; }

        // Chỉ dùng để hỏi rõ khi yêu cầu quá mơ hồ
        public string? CauHoiBoSung { get; set; }
    }
}

