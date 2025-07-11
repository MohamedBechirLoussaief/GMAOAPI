using System.Net;
using System.Net.Mail;

namespace GMAOAPI.Services.EmailService
{
    public class SmtpEmailSender : IEmailSender
    {
    

        public async Task SendEmailAsync(string email, string subject, string message)
        {
            var mail = "gmaoSfax@gmail.com";
            var pw = "gmaoSfax123";

           var client = new SmtpClient("smtp.gmail.com", 587)
            {
                Credentials = new NetworkCredential(mail, pw),
                EnableSsl = true
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(mail),
                Subject = subject,
                Body = message,
                IsBodyHtml = true,
            };

            mailMessage.To.Add(email);
            await client.SendMailAsync(mailMessage);
        }
    }
}
