using System.Security.Cryptography;
using System.Text.Json;
using System.Text;

namespace Core.Helper.Helper
{

    public class SignatureHelper
    {
        public static string GenerateSignature(object requestBody, string secretKey)
        {
            // Chuyển object thành JSON có định dạng chuẩn
            string jsonBody = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions { WriteIndented = false });

            // Chuyển JSON thành byte array
            byte[] keyBytes = Encoding.UTF8.GetBytes(secretKey);
            byte[] bodyBytes = Encoding.UTF8.GetBytes(jsonBody);

            // Tạo HMAC-SHA256
            using var hmac = new HMACSHA256(keyBytes);
            byte[] hash = hmac.ComputeHash(bodyBytes);

            // Chuyển đổi sang Base64
            return Convert.ToBase64String(hash);
        }
    }
}
