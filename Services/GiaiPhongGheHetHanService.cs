
using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VeSuKienWeb.Data;

namespace VeSuKienWeb.Services
{
    public class GiaiPhongGheHetHanService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<GiaiPhongGheHetHanService> _logger;

        public GiaiPhongGheHetHanService(
            IServiceScopeFactory scopeFactory,
            ILogger<GiaiPhongGheHetHanService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(
            CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "Dịch vụ tự động giải phóng ghế và hủy đơn hết hạn đã khởi động.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await XuLyDonHetHanAsync(stoppingToken);
                }
                catch (OperationCanceledException)
                    when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Lỗi khi tự động xử lý đơn hàng hết hạn.");
                }

                try
                {
                    await Task.Delay(
                        TimeSpan.FromSeconds(15),
                        stoppingToken);
                }
                catch (OperationCanceledException)
                    when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }

            _logger.LogInformation(
                "Dịch vụ tự động giải phóng ghế và hủy đơn hết hạn đã dừng.");
        }

        private async Task XuLyDonHetHanAsync(
            CancellationToken cancellationToken)
        {
            await using var scope = _scopeFactory.CreateAsyncScope();

            var db = scope.ServiceProvider
                .GetRequiredService<NguCanhSuKien>();

            var now = DateTime.Now;
            var thoiDiemHetHan = now.AddMinutes(-5);

            await using var transaction =
                await db.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable,
                    cancellationToken);

            try
            {
                await db.Database.ExecuteSqlInterpolatedAsync($@"

                    SET NOCOUNT ON;

                    DECLARE @DonHangHetHan TABLE
                    (
                        DonHangId INT PRIMARY KEY
                    );

                    INSERT INTO @DonHangHetHan (DonHangId)
                    SELECT d.DonHangId
                    FROM dbo.DonHang d WITH (UPDLOCK, HOLDLOCK)
                    WHERE d.TrangThai = N'DangXuLy'
                      AND d.NgayDat <= {thoiDiemHetHan};

                    INSERT INTO @DonHangHetHan (DonHangId)
                    SELECT DISTINCT g.DonHangGiuId
                    FROM dbo.SoDoChoNgoi g
                    INNER JOIN dbo.DonHang d
                        ON d.DonHangId = g.DonHangGiuId
                    WHERE g.DonHangGiuId IS NOT NULL
                      AND d.TrangThai = N'DangXuLy'
                      AND (
                          g.ThoiGianHetHanGiu IS NULL
                          OR g.ThoiGianHetHanGiu <= {now}
                      )
                      AND NOT EXISTS
                      (
                          SELECT 1
                          FROM @DonHangHetHan h
                          WHERE h.DonHangId = g.DonHangGiuId
                      );

                    UPDATE v
                    SET v.TrangThai = N'Huy'
                    FROM dbo.Ve v
                    INNER JOIN @DonHangHetHan h
                        ON v.DonHangId = h.DonHangId
                    WHERE v.TrangThai = N'ChuaThanhToan';

                    UPDATE d
                    SET d.TrangThai = N'Huy'
                    FROM dbo.DonHang d
                    INNER JOIN @DonHangHetHan h
                        ON d.DonHangId = h.DonHangId
                    WHERE d.TrangThai = N'DangXuLy';

                    UPDATE g
                    SET g.DonHangGiuId = NULL,
                        g.ThoiGianHetHanGiu = NULL
                    FROM dbo.SoDoChoNgoi g
                    INNER JOIN @DonHangHetHan h
                        ON g.DonHangGiuId = h.DonHangId;

                        UPDATE v
SET v.TrangThai = N'Huy'
FROM dbo.Ve v
INNER JOIN dbo.DonHang d
    ON v.DonHangId = d.DonHangId
WHERE d.TrangThai = N'Huy'
  AND v.TrangThai = N'ChuaThanhToan'
  AND NOT EXISTS
  (
      SELECT 1
      FROM dbo.ThanhToan tt
      WHERE tt.VeId = v.VeId
        AND tt.TrangThai = N'ThanhCong'
  );

UPDATE g
SET g.DonHangGiuId = NULL,
    g.ThoiGianHetHanGiu = NULL
FROM dbo.SoDoChoNgoi g
INNER JOIN dbo.DonHang d
    ON g.DonHangGiuId = d.DonHangId
WHERE d.TrangThai = N'Huy';

                ", cancellationToken);

                await transaction.CommitAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);

                _logger.LogError(
                    ex,
                    "Không thể xử lý đơn hàng hết hạn.");

                throw;
            }
        }
    }
}
