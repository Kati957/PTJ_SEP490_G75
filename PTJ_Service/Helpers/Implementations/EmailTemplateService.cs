using PTJ_Service.Helpers.Interfaces;

namespace PTJ_Service.Helpers.Implementations
{
    public sealed class EmailTemplateService : IEmailTemplateService
    {
        private string BaseEmailLayout(string title, string content)
        {
            return $@"
<table width='100%' cellpadding='0' cellspacing='0' 
       style='font-family: Arial, sans-serif; background:#f5f5f5; padding:20px 0;'>
  <tr>
    <td align='center'>
      <table width='100%' cellpadding='0' cellspacing='0' 
             style='max-width:600px; background:#ffffff; border-radius:8px; padding:20px;'>

        <tr>
          <td align='center' style='padding-bottom:20px;'>
            <h2 style='margin:0; color:#2c3e50;'>PTJ - Part-Time Job Finder</h2>
            <p style='margin:8px 0 0 0; color:#7f8c8d; font-size:14px;'>{title}</p>
          </td>
        </tr>

        <tr>
          <td style='font-size:15px; color:#2c3e50; line-height:1.6;'>
            {content}
          </td>
        </tr>

        <tr>
          <td align='center' 
              style='padding-top:30px; font-size:12px; color:#95a5a6; line-height:1.5;'>
            <p style='margin:0;'>© {DateTime.UtcNow.Year} PTJ System</p>
            <p style='margin:4px 0;'>Đây là email tự động, vui lòng không trả lời.</p>
            <p style='margin:0;'>Hỗ trợ: support@ptj.vn</p>
          </td>
        </tr>

      </table>
    </td>
  </tr>
</table>";
        }

        public string CreateVerifyEmailTemplate(string verifyLink)
        {
            var content = $@"
<p>Xin chào,</p>
<p>Nhấn vào nút dưới đây để xác minh tài khoản:</p>

<p align='center'>
  <a href='{verifyLink}' 
     style='background:#3498db; color:#ffffff; padding:12px 20px;
            text-decoration:none; border-radius:5px;'>
     Xác minh tài khoản
  </a>
</p>

<p>Nếu bạn không yêu cầu thao tác này, vui lòng bỏ qua email.</p>";

            return BaseEmailLayout("Xác minh tài khoản", content);
        }

        public string CreateResetPasswordTemplate(string resetLink)
        {
            var content = $@"
<p>Bạn đã yêu cầu đặt lại mật khẩu.</p>
<p>Nhấn vào nút dưới đây để tiếp tục:</p>

<p align='center'>
  <a href='{resetLink}' 
     style='background:#e67e22; color:#ffffff; padding:12px 20px;
            text-decoration:none; border-radius:5px;'>
     Đặt lại mật khẩu
  </a>
</p>

<p>Nếu không phải bạn yêu cầu, vui lòng bỏ qua email này.</p>";

            return BaseEmailLayout("Đặt lại mật khẩu", content);
        }

        public string CreateApplicationAcceptedTemplate(string name, string postTitle, string employerName)
        {
            var content = $@"
<p>Xin chào <b>{name}</b>,</p>
<p>Bạn đã được <b>{employerName}</b> chấp nhận cho vị trí:</p>
<p style='font-size:18px; font-weight:bold; color:#27ae60;'>{postTitle}</p>
<p>Hãy chú ý email/điện thoại của bạn để cập nhật thông tin tiếp theo.</p>";

            return BaseEmailLayout("Kết quả tuyển dụng", content);
        }

        public string CreateApplicationRejectedTemplate(string name, string postTitle)
        {
            var content = $@"
<p>Xin chào <b>{name}</b>,</p>
<p>Cảm ơn bạn đã ứng tuyển vị trí:</p>
<p style='font-size:18px; color:#c0392b;'><b>{postTitle}</b></p>
<p>Rất tiếc bạn chưa phù hợp cho vị trí này.</p>
<p>Chúc bạn thành công trong những cơ hội tiếp theo.</p>";

            return BaseEmailLayout("Kết quả ứng tuyển", content);
        }

        public string CreateInterviewInviteTemplate(string name, string postTitle, string employerName, string interviewNote)
        {
            var content = $@"
<p>Xin chào <b>{name}</b>,</p>
<p>Bạn được mời tham gia phỏng vấn cho vị trí:</p>
<p style='font-size:18px; color:#2980b9;'><b>{postTitle}</b></p>
<p><b>Nhà tuyển dụng:</b> {employerName}</p>
<p><b>Thông tin phỏng vấn:</b></p>
<div style='background:#f0f3f4; padding:12px; border-radius:5px;'>{interviewNote}</div>";

            return BaseEmailLayout("Thư mời phỏng vấn", content);
        }

        public string CreateEmployerRejectedTemplate(string companyName, string reason)
        {
            var content = $@"
<p>Xin chào <b>{companyName}</b>,</p>
<p>Hồ sơ đăng ký tài khoản nhà tuyển dụng của bạn đã <b>bị từ chối</b>.</p>
<p><b>Lý do:</b> {reason}</p>
<p>Vui lòng cập nhật thông tin và gửi lại yêu cầu nếu cần.</p>";

            return BaseEmailLayout("Hồ sơ bị từ chối", content);
        }

        public string CreateEmployerApprovedTemplate(string companyName, string verifyLink)
        {
            var content = $@"
<p>Xin chào <b>{companyName}</b>,</p>
<p>Hồ sơ đăng ký nhà tuyển dụng của bạn đã được <b>phê duyệt</b>.</p>
<p>Vui lòng xác minh email để kích hoạt tài khoản:</p>

<p align='center'>
  <a href='{verifyLink}'
     style='background:#27ae60; color:#ffffff; padding:12px 20px;
            text-decoration:none; border-radius:5px;'>
     Xác minh email
  </a>
</p>";

            return BaseEmailLayout("Tài khoản được duyệt", content);
        }

        public string CreateGoogleEmployerApprovedTemplate(string companyName)
        {
            var content = $@"
<p>Xin chào <b>{companyName}</b>,</p>
<p>Tài khoản Google Employer của bạn đã được <b>phê duyệt</b>.</p>
<p>Bạn có thể đăng nhập và sử dụng chức năng Nhà tuyển dụng ngay bây giờ.</p>";

            return BaseEmailLayout("Google Employer được duyệt", content);
        }

        public string CreateGoogleEmployerRejectedTemplate(string companyName, string reason)
        {
            var content = $@"
<p>Xin chào <b>{companyName}</b>,</p>
<p>Tài khoản Google Employer của bạn đã bị <b>từ chối</b>.</p>
<p><b>Lý do:</b> {reason}</p>
<p>Vui lòng kiểm tra và gửi lại yêu cầu nếu cần.</p>";

            return BaseEmailLayout("Google Employer bị từ chối", content);
        }
    }
}
