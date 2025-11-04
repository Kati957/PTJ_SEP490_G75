using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PTJ_Service.Helpers.Interfaces;

namespace PTJ_Service.Helpers.Implementations
{
    public sealed class BcryptPasswordHasher : IPasswordHasher
    {
        // Tạo hash an toàn bằng BCrypt với work factor = 11
        public string Hash(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("Password cannot be empty.", nameof(password));

            return BCrypt.Net.BCrypt.HashPassword(password, workFactor: 11);
        }

        // So sánh mật khẩu người nhập và hash lưu trong DB
        public bool Verify(string password, string hash)
        {
            if (string.IsNullOrWhiteSpace(hash))
                throw new ArgumentException("Hash cannot be empty.", nameof(hash));

            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
    }
}
