using System.Net.Http.Headers;
using System.Text.Json;
using System.Text;

namespace GMAOAPI.Services.EmailService
{
    public class SendinblueEmailSender : IEmailSender
    {
        private readonly HttpClient _http;
        private readonly string _senderEmail = "gmaosfax@gmail.com";
        private readonly string _senderName = "GMAO App";

        public SendinblueEmailSender(string apiKey)
        {
            _http = new HttpClient { BaseAddress = new Uri("https://api.sendinblue.com/") };
            _http.DefaultRequestHeaders.Add("api-key", apiKey);
            _http.DefaultRequestHeaders.Accept.Add(
              new MediaTypeWithQualityHeaderValue("application/json")
            );
        }

        public async Task SendEmailAsync(string toEmail, string subject, string htmlContent)
        {
            var payload = new
            {
                sender = new { email = _senderEmail, name = _senderName },
                to = new[] { new { email = toEmail } },
                subject,
                htmlContent
            };

            using var content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json"
            );

            var resp = await _http.PostAsync("v3/smtp/email", content);
            resp.EnsureSuccessStatusCode();
        }
    }

}
