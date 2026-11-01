using Microsoft.EntityFrameworkCore;
using VeSuKienWeb.Models;

namespace VeSuKienWeb.Data
{
    public class NguCanhSuKien : DbContext
    {
        public NguCanhSuKien(DbContextOptions<NguCanhSuKien> options)
            : base(options)
        {
        }

        public DbSet<NguoiDung> NguoiDung { get; set; } = null!;
        public DbSet<SuKien> SuKien { get; set; } = null!;
        public DbSet<ToChucSuKien> ToChucSuKien { get; set; } = null!;
        public DbSet<LoaiSuKien> LoaiSuKien { get; set; } = null!;
        public DbSet<DiaDiem> DiaDiem { get; set; }
        public DbSet<SuKienTrucTuyen> SuKienTrucTuyen { get; set; }
        public DbSet<LoaiVe> LoaiVe { get; set; }
        public DbSet<SoDoChoNgoi> SoDoChoNgoi { get; set; }

public DbSet<SoDoSuKien> SoDoSuKien { get; set; } = null!;
public DbSet<KhuVucChoNgoi> KhuVucChoNgoi { get; set; } = null!;
public DbSet<ThanhPhanSoDo> ThanhPhanSoDo { get; set; } = null!;

        public DbSet<Ve> Ve { get; set; } = null!;
        public DbSet<ThanhToan> ThanhToan { get; set; } = null!;
        public DbSet<ThamGiaSuKien> ThamGiaSuKien { get; set; } = null!;
        public DbSet<YeuCauHoanHuyVe> YeuCauHoanHuyVe { get; set; } = null!;
        public DbSet<ThongBao> ThongBao { get; set; } = null!;

        public DbSet<DonHang> DonHang { get; set; } = null!;




        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

modelBuilder.Entity<SoDoSuKien>(e =>
{
    e.ToTable("SoDoSuKien");
    e.HasKey(x => x.SoDoId);

    e.HasIndex(x => x.SuKienId).IsUnique();

    e.HasOne(x => x.SuKien)
        .WithMany()
        .HasForeignKey(x => x.SuKienId)
        .OnDelete(DeleteBehavior.NoAction);

    e.Property(x => x.TrangThai)
        .HasMaxLength(20);

    e.Property(x => x.NgayCapNhat)
        .HasColumnType("datetime2");
});

modelBuilder.Entity<KhuVucChoNgoi>(e =>
{
    e.ToTable("KhuVucChoNgoi");
    e.HasKey(x => x.KhuVucId);

    e.HasIndex(x => new { x.SoDoId, x.TenKhu })
        .IsUnique();

    e.HasOne(x => x.SoDoSuKien)
        .WithMany(x => x.KhuVucChoNgois)
        .HasForeignKey(x => x.SoDoId)
        .OnDelete(DeleteBehavior.NoAction);

    e.HasOne(x => x.LoaiVe)
        .WithMany()
        .HasForeignKey(x => x.LoaiVeId)
        .OnDelete(DeleteBehavior.NoAction);

    e.Property(x => x.TenKhu)
        .HasMaxLength(100);

    e.Property(x => x.MauSac)
        .HasColumnType("varchar(7)");
});

modelBuilder.Entity<ThanhPhanSoDo>(e =>
{
    e.ToTable("ThanhPhanSoDo");
    e.HasKey(x => x.ThanhPhanId);

    e.HasOne(x => x.SoDoSuKien)
        .WithMany(x => x.ThanhPhanSoDos)
        .HasForeignKey(x => x.SoDoId)
        .OnDelete(DeleteBehavior.NoAction);
});


modelBuilder.Entity<SoDoChoNgoi>(e =>
{
    e.ToTable("SoDoChoNgoi");

    e.HasOne(x => x.KhuVucChoNgoi)
        .WithMany(x => x.GheNgois)
        .HasForeignKey(x => x.KhuVucId)
        .OnDelete(DeleteBehavior.NoAction);

    e.HasIndex(x => x.DonHangGiuId);

    e.Property(x => x.ThoiGianHetHanGiu)
        .HasColumnType("datetime2");
});
modelBuilder.Entity<Ve>(e =>
{
    e.HasIndex(x => x.ChoNgoiId)
        .IsUnique()
        .HasFilter("[ChoNgoiId] IS NOT NULL AND [TrangThai] <> 'Huy'");
});

            // NguoiDung
            modelBuilder.Entity<NguoiDung>(e =>
            {
                e.ToTable("NguoiDung");
                e.HasKey(x => x.NguoiDungId);
                e.HasIndex(x => x.Email).IsUnique();

                e.Property(x => x.HoTen)
                    .HasMaxLength(100)
                    .IsRequired();

                e.Property(x => x.Email)
                    .HasMaxLength(100)
                    .IsRequired();

                e.Property(x => x.MatKhau)
                    .HasMaxLength(256)
                    .IsRequired();

                e.Property(x => x.DienThoai)
                    .HasMaxLength(10);

                e.Property(x => x.VaiTro)
                    .HasMaxLength(20);
                e.Property(x => x.TrangThai)
    .HasMaxLength(20)
    .HasDefaultValue("HoatDong");
            });

            // ToChucSuKien
            modelBuilder.Entity<ToChucSuKien>(e =>
            {
                e.ToTable("ToChucSuKien");
                e.HasKey(x => x.ToChucId);

                e.Property(x => x.TenToChuc)
                    .HasMaxLength(150);

                e.Property(x => x.EmailLienHe)
                    .HasMaxLength(100);

                e.HasOne(x => x.NguoiDung)
                    .WithMany()
                    .HasForeignKey(x => x.NguoiDungId);
            });

            // LoaiSuKien
            modelBuilder.Entity<LoaiSuKien>(e =>
            {
                e.ToTable("LoaiSuKien");
                e.HasKey(x => x.LoaiId);

                e.Property(x => x.TenLoai)
                    .HasMaxLength(50);
            });

            // SuKien
            modelBuilder.Entity<SuKien>(e =>
            {
                e.ToTable("SuKien");
                e.HasKey(x => x.SuKienId);

                e.Property(x => x.TenSuKien)
                    .HasMaxLength(200)
                    .IsRequired();

                e.Property(x => x.TrangThaiDuyet)
                    .HasMaxLength(20);

                e.HasOne(x => x.LoaiSuKien)
                    .WithMany()
                    .HasForeignKey(x => x.LoaiId);

                e.HasOne(x => x.ToChucSuKien)
                    .WithMany()
                    .HasForeignKey(x => x.ToChucId);
            });

            // 🔔 BÁO CHO EF BIẾT BẢNG Ve CÓ TRIGGER (để tránh lỗi OUTPUT + trigger)
            modelBuilder.Entity<Ve>().ToTable(tb =>
            {
                tb.HasTrigger("trg_Ve_CreateRemindersOnPaid");
                tb.HasTrigger("trg_Ve_Limit10PerOrder");
                tb.HasTrigger("trg_Ve_NoOverlappingEventsPerUser");
            });

            // (Sau này nếu em cần, mình có thể khai báo thêm trigger cho DanhGiaSuKien ở đây)
        }
    }
}
