
using System;
using System.Collections.Generic;

namespace VeSuKienWeb.Models.KhamPhaSuKien
{
    // Thông tin của từng sự kiện được tìm thấy
    public class SuKienPhuHop
    {
        public int SuKienId { get; set; }

        public string TenSuKien { get; set; } = "";

        public string? AnhSuKien { get; set; }

        // Nội dung chi tiết của sự kiện
        public string? MoTaChiTiet { get; set; }

        // Thời gian tổ chức
        public DateTime ThoiGianBatDau { get; set; }

        public DateTime ThoiGianKetThuc { get; set; }

        // Thông tin địa điểm
        public string DiaDiem { get; set; } = "";

        public string TenDiaDiem { get; set; } = "";

        public string DiaChi { get; set; } = "";

        // Chủ đề sự kiện
        public string ChuDe { get; set; } = "";

        // Giá vé thấp nhất
        public decimal? GiaVeThapNhat { get; set; }

        // Điểm dùng để xếp hạng sự kiện,
        // không phải phần trăm độ chính xác của AI
        public double DiemPhuHop { get; set; }

 // Điểm phù hợp với câu hỏi, chưa cộng Cosine.
 public double DiemNoiDung { get; set; }

 // Độ tương đồng Cosine từ 0 đến 1.
 public double DiemCosine { get; set; }

        // Lý do sự kiện phù hợp
        public string LyDoPhuHop { get; set; } = "";

        // Đánh dấu sự kiện chỉ liên quan một phần
        // đến điều kiện khách hàng yêu cầu
        public bool LaGoiYMoRong { get; set; }

        // Các điều kiện chưa đáp ứng
        public string DieuKienChuaKhop { get; set; } = "";
    }

    // Kết quả tìm kiếm tối đa 3 sự kiện
    public class KetQuaKhamPha
    {
        // Nhu cầu mà Gemini đã phân tích
        public NhuCauKhamPha NhuCau { get; set; } = new();

        // Danh sách sự kiện phù hợp
        public List<SuKienPhuHop> DanhSach { get; set; }
            = new();

        // Thông báo kết quả tìm kiếm
        public string ThongBao { get; set; } = "";
    }
}
