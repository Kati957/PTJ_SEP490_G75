using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PTJ_API.Controllers
    {
    [Route("api/[controller]")]
    [ApiController]
    public class AdminLogController : ControllerBase
        {
        private readonly string _logDir = Path.Combine(AppContext.BaseDirectory, "logs");
        private readonly string _logFile = "AdminDailyReport.log";

        /// <summary>
        /// Lấy toàn bộ log (mới nhất trước)
        /// GET: /api/adminlog
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAllLogs()
            {
            var filePath = Path.Combine(_logDir, _logFile);

            if (!System.IO.File.Exists(filePath))
                return NotFound(new { message = "❌ Chưa có log nào được ghi." });

            var lines = await System.IO.File.ReadAllLinesAsync(filePath);
            // đảo ngược cho log mới nhất lên đầu
            var logs = lines.Reverse().ToList();

            return Ok(new { total = logs.Count, logs });
            }

        /// <summary>
        /// Lấy log theo ngày cụ thể (yyyy-MM-dd)
        /// GET: /api/adminlog/date/2025-11-29
        /// </summary>
        [HttpGet("date/{date}")]
        public async Task<IActionResult> GetLogsByDate(string date)
            {
            if (!DateTime.TryParse(date, out var targetDate))
                return BadRequest(new { message = "❌ Định dạng ngày không hợp lệ. Ví dụ: 2025-11-29" });

            var filePath = Path.Combine(_logDir, _logFile);

            if (!System.IO.File.Exists(filePath))
                return NotFound(new { message = "❌ Chưa có log nào được ghi." });

            var lines = await System.IO.File.ReadAllLinesAsync(filePath);
            var logs = lines
                .Where(l => l.Contains(targetDate.ToString("dd/MM/yyyy")))
                .Reverse()
                .ToList();

            return Ok(new { date = targetDate.ToString("yyyy-MM-dd"), total = logs.Count, logs });
            }

        /// <summary>
        /// Xóa log file (nếu admin muốn dọn dẹp)
        /// DELETE: /api/adminlog/clear
        /// </summary>
        [HttpDelete("clear")]
        public IActionResult ClearLogs()
            {
            var filePath = Path.Combine(_logDir, _logFile);

            if (!System.IO.File.Exists(filePath))
                return NotFound(new { message = "❌ Không có file log để xóa." });

            System.IO.File.WriteAllText(filePath, string.Empty);
            return Ok(new { message = "🧹 Đã xóa toàn bộ log." });
            }
        }
    }
