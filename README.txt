PATCH CAP NHAT VE SU KIEN - BAN 02/11/2026

Cach ap dung:
1. Sao luu project hien tai.
2. Giai nen goi nay vao thu muc goc cua project DOAN_ThucTap, cho phep ghi de file trung ten.
3. Xoa file cu Views/BanToChuc/YeuCauHoanHuy.cshtml (view BTC da doi ten thanh YeuCauHuyVe.cshtml).
4. Chay dotnet build .\VeSuKienWeb.csproj.

Cac thay doi chinh:
- VIP khong bat buoc. So hang VIP = 0 se tao toan bo ghe Thuong.
- Loai ve duoc tao theo khu ghe thuc te; khong tao khu VIP neu khong co ghe VIP.
- Workshop tao dung so ghe theo SoChoNgoi, phan loai VIP/Thuong theo so luong nhap; VIP co the bang 0.
- Luu chu de SuKienChuDe trong cung transaction voi su kien.
- Validate anh JPG/PNG/WEBP theo signature va gioi han 5 MB.
- Xoa su kien chua co ve: xoa cac lien ket chu de, so do, khu vuc, thanh phan, nhac su kien va danh gia trong transaction; neu co ve thi an su kien.
- Yeu cau chi la HuyVe, trang thai duyet thanh cong la DaHuy; khong xu ly hoan tien.
- Thong ke ve da ban chi tinh DaThanhToan/DaCheckIn.
- Chan giam so luong loai ve xuong duoi so ve da ban.

Luu y:
- Khong tao migration moi. Schema database hien tai duoc giu nguyen; HinhThuc van ton tai de tuong thich schema cu, nhung luong moi chi tao HuyVe.
- Can build va test tren may Windows co .NET SDK 8.0. Moi truong kiem tra hien tai khong co dotnet CLI nen chua the xac nhan build sau patch.
