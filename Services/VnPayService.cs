
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VeSuKienWeb.Config;

namespace VeSuKienWeb.Services
{
    public interface IVnPayService
    {
        string CreatePaymentUrl(
            HttpContext httpContext,
            int donHangId,
            decimal amount,
            string orderDesc);

        VnPayReturnResult ParseReturn(IQueryCollection query);
    }

    public class VnPayReturnResult
    {
        public bool IsSuccess { get; set; }
        public bool IsValidSignature { get; set; }

        public string OrderId { get; set; } = string.Empty;
        public string TransactionNo { get; set; } = string.Empty;
        public string ResponseCode { get; set; } = string.Empty;
        public string TransactionStatus { get; set; } = string.Empty;
        public string TmnCode { get; set; } = string.Empty;

        public decimal? Amount { get; set; }

        public string Message { get; set; } = string.Empty;
    }

    public class VnPayService : IVnPayService
    {
        private readonly VnPaySettings _settings;
        private readonly ILogger<VnPayService> _logger;

        public VnPayService(
            IOptions<VnPaySettings> options,
            ILogger<VnPayService> logger)
        {
            _settings = options.Value;
            _logger = logger;
        }

        public string CreatePaymentUrl(
            HttpContext httpContext,
            int donHangId,
            decimal amount,
            string orderDesc)
        {
            if (string.IsNullOrWhiteSpace(_settings.TmnCode) ||
                string.IsNullOrWhiteSpace(_settings.HashSecret) ||
                string.IsNullOrWhiteSpace(_settings.BaseUrl) ||
                string.IsNullOrWhiteSpace(_settings.ReturnUrl))
            {
                throw new InvalidOperationException(
                    "Cấu hình VNPay chưa đầy đủ.");
            }

            if (donHangId <= 0 || amount <= 0)
            {
                throw new ArgumentException(
                    "Thông tin đơn hàng không hợp lệ.");
            }

            long vnpAmount = checked(
                decimal.ToInt64(amount * 100m));

            var vnpParams = new SortedDictionary<string, string>(
                StringComparer.Ordinal)
            {
                { "vnp_Version", "2.1.0" },
                { "vnp_Command", "pay" },
                { "vnp_TmnCode", _settings.TmnCode },
                {
                    "vnp_Amount",
                    vnpAmount.ToString(CultureInfo.InvariantCulture)
                },
                {
                    "vnp_CreateDate",
                    DateTime.Now.ToString("yyyyMMddHHmmss")
                },
                { "vnp_CurrCode", "VND" },
                { "vnp_IpAddr", "127.0.0.1" },
                { "vnp_Locale", "vn" },
                { "vnp_OrderInfo", orderDesc },
                { "vnp_OrderType", "other" },
                { "vnp_ReturnUrl", _settings.ReturnUrl },
                { "vnp_TxnRef", donHangId.ToString() }
            };

            var hashData = BuildQueryString(vnpParams);

            var secureHash = HmacSHA512(
                _settings.HashSecret,
                hashData);

            var paymentUrl =
                $"{_settings.BaseUrl}?{hashData}&vnp_SecureHash={secureHash}";

            _logger.LogInformation(
                "[VNPAY] Tao yeu cau thanh toan. DonHang={DonHangId}, SoTien={Amount}",
                donHangId,
                amount);

            return paymentUrl;
        }

        public VnPayReturnResult ParseReturn(
            IQueryCollection query)
        {
            var result = new VnPayReturnResult();

            if (string.IsNullOrWhiteSpace(_settings.HashSecret) ||
                string.IsNullOrWhiteSpace(_settings.TmnCode))
            {
                result.Message = "Cấu hình VNPay không hợp lệ.";
                return result;
            }

            var vnpData = new SortedDictionary<string, string>(
                StringComparer.Ordinal);

            foreach (var key in query.Keys)
            {
                if (!key.StartsWith(
                    "vnp_",
                    StringComparison.Ordinal))
                {
                    continue;
                }

                var value = query[key].ToString();

                if (!string.IsNullOrEmpty(value))
                {
                    vnpData[key] = value;
                }
            }

            if (!vnpData.TryGetValue(
                "vnp_SecureHash",
                out var receivedHash))
            {
                result.Message = "Thiếu chữ ký VNPay.";
                return result;
            }

            vnpData.Remove("vnp_SecureHash");
            vnpData.Remove("vnp_SecureHashType");

            var hashData = BuildQueryString(vnpData);

            var calculatedHash = HmacSHA512(
                _settings.HashSecret,
                hashData);

            byte[] receivedBytes;
            byte[] calculatedBytes;

            try
            {
                receivedBytes = Convert.FromHexString(receivedHash);
                calculatedBytes = Convert.FromHexString(calculatedHash);
            }
            catch (FormatException)
            {
                result.Message = "Định dạng chữ ký không hợp lệ.";
                return result;
            }

            if (!CryptographicOperations.FixedTimeEquals(
                receivedBytes,
                calculatedBytes))
            {
                _logger.LogWarning(
                    "[VNPAY] Chu ky phan hoi khong hop le.");

                result.Message = "Chữ ký VNPay không hợp lệ.";
                return result;
            }

            result.IsValidSignature = true;

            result.OrderId = GetValue(vnpData, "vnp_TxnRef");
            result.TransactionNo = GetValue(
                vnpData, "vnp_TransactionNo");

            result.ResponseCode = GetValue(
                vnpData, "vnp_ResponseCode");

            result.TransactionStatus = GetValue(
                vnpData, "vnp_TransactionStatus");

            result.TmnCode = GetValue(
                vnpData, "vnp_TmnCode");

            if (!string.Equals(
                result.TmnCode,
                _settings.TmnCode,
                StringComparison.Ordinal))
            {
                result.Message = "Mã website VNPay không khớp.";
                return result;
            }

            var amountText = GetValue(
                vnpData, "vnp_Amount");

            if (!long.TryParse(
                amountText,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out var amountValue) ||
                amountValue <= 0)
            {
                result.Message = "Số tiền VNPay không hợp lệ.";
                return result;
            }

            result.Amount = amountValue / 100m;

            if (string.IsNullOrWhiteSpace(result.OrderId) ||
                string.IsNullOrWhiteSpace(result.TransactionNo))
            {
                result.Message = "Thiếu thông tin giao dịch VNPay.";
                return result;
            }

            result.IsSuccess =
                result.ResponseCode == "00" &&
                result.TransactionStatus == "00";

            result.Message = result.IsSuccess
                ? "Thanh toán VNPay thành công."
                : "Giao dịch VNPay chưa thành công hoặc chưa hoàn tất.";

            _logger.LogInformation(
                "[VNPAY] Nhan ket qua. DonHang={OrderId}, MaGD={TransactionNo}, ResponseCode={ResponseCode}, TransactionStatus={TransactionStatus}",
                result.OrderId,
                result.TransactionNo,
                result.ResponseCode,
                result.TransactionStatus);

            return result;
        }

        private static string GetValue(
            SortedDictionary<string, string> data,
            string key)
        {
            return data.TryGetValue(key, out var value)
                ? value
                : string.Empty;
        }

        private static string BuildQueryString(
            SortedDictionary<string, string> data)
        {
            var parts = new List<string>();

            foreach (var item in data)
            {
                if (string.IsNullOrEmpty(item.Value))
                {
                    continue;
                }

                parts.Add(
                    WebUtility.UrlEncode(item.Key) +
                    "=" +
                    WebUtility.UrlEncode(item.Value));
            }

            return string.Join("&", parts);
        }

        private static string HmacSHA512(
            string key,
            string inputData)
        {
            var keyBytes = Encoding.UTF8.GetBytes(key);
            var inputBytes = Encoding.UTF8.GetBytes(inputData);

            using var hmac = new HMACSHA512(keyBytes);

            var hashBytes = hmac.ComputeHash(inputBytes);

            return Convert.ToHexString(hashBytes).ToLowerInvariant();
        }
    }
}
