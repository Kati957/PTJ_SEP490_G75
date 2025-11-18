using System;
using PTJ_Service.Helpers.Interfaces;

namespace PTJ_Service.Helpers.Implementations
{
    public sealed class BcryptPasswordHasher : IPasswordHasher
    {
        // Tạo hash an toàn bằng BCrypt với work factor = 11
        public string Hash(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("Mật khẩu không được để trống.", nameof(password));

            return BCrypt.Net.BCrypt.HashPassword(password, workFactor: 11);
        }

        // So sánh mật khẩu người nhập và hash lưu trong DB
        public bool Verify(string password, string hash)
        {
            if (string.IsNullOrWhiteSpace(hash))
                throw new ArgumentException("Hash không được để trống.", nameof(hash));

            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
    }
}
