using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using PTJ_Service.Helpers.Interfaces;

namespace PTJ_Service.Helpers.Implementations
{
    public sealed class SmtpEmailSender : IEmailSender
    {
        private readonly IConfiguration _cfg;

        public SmtpEmailSender(IConfiguration cfg)
        {
            _cfg = cfg;
        }

        public async Task SendEmailAsync(string to, string subject, string html)
        {
            var host = _cfg["Email:Host"]!;
            var port = int.Parse(_cfg["Email:Port"]!);
            var user = _cfg["Email:User"]!;
            var pass = _cfg["Email:Password"]!;
            var from = _cfg["Email:From"]!;
            var fromName = _cfg["Email:FromName"] ?? from;

            using var msg = new MailMessage(new MailAddress(from, fromName), new MailAddress(to))
            {
                Subject = subject,
                Body = html,
                IsBodyHtml = true
            };

            using var smtp = new SmtpClient(host, port)
            {
                EnableSsl = true,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(user, pass),
                DeliveryMethod = SmtpDeliveryMethod.Network
            };

            await smtp.SendMailAsync(msg);
        }
    }
}

