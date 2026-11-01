
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
                "Dịch vụ tự động giải phóng ghế đã khởi động.");

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
                        "Lỗi khi tự động giải phóng ghế hết hạn.");
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
                "Dịch vụ tự động giải phóng ghế đã dừng.");
        }

        private async Task XuLyDonHetHanAsync(
            CancellationToken cancellationToken)
        {
            await using var scope = _scopeFactory.CreateAsyncScope();

            var db = scope.ServiceProvider
                .GetRequiredService<NguCanhSuKien>();

            var now = DateTime.Now;

            await using var transaction =
                await db.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable,
                    cancellationToken);

            try
            {
                int soDonHetHan =
                    await db.Database.ExecuteSqlInterpolatedAsync($@"

                        DECLARE @DonHangHetHan TABLE
                        (
                            DonHangId INT PRIMARY KEY
                        );

                        INSERT INTO @DonHangHetHan (DonHangId)
                        SELECT DISTINCT DonHangGiuId
                        FROM dbo.SoDoChoNgoi
                        WHERE DonHangGiuId IS NOT NULL
                          AND (
                              ThoiGianHetHanGiu IS NULL
                              OR ThoiGianHetHanGiu <= {now}
                          );

                        DECLARE @SoDonHang INT;
                        SELECT @SoDonHang = COUNT(*)
                        FROM @DonHangHetHan;

                        -- Hủy các vé chưa thanh toán
                        UPDATE v
                        SET v.TrangThai = N'Huy'
                        FROM dbo.Ve v
                        INNER JOIN @DonHangHetHan h
                            ON v.DonHangId = h.DonHangId
                        WHERE v.TrangThai = N'ChuaThanhToan';

                        -- Hủy các đơn đang xử lý
                        UPDATE d
                        SET d.TrangThai = N'Huy'
                        FROM dbo.DonHang d
                        INNER JOIN @DonHangHetHan h
                            ON d.DonHangId = h.DonHangId
                        WHERE d.TrangThai = N'DangXuLy';

                        -- Giải phóng toàn bộ ghế của đơn hết hạn
                        UPDATE g
                        SET g.DonHangGiuId = NULL,
                            g.ThoiGianHetHanGiu = NULL
                        FROM dbo.SoDoChoNgoi g
                        INNER JOIN @DonHangHetHan h
                            ON g.DonHangGiuId = h.DonHangId;

                        SELECT @SoDonHang;
                    ", cancellationToken);

                await transaction.CommitAsync(cancellationToken);

                if (soDonHetHan > 0)
                {
                    _logger.LogInformation(
                        "Đã xử lý {SoDon} đơn hàng hết hạn.",
                        soDonHetHan);
                }
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }
    }
}

