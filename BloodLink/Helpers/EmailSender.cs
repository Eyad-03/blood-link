using System.Net;
using System.Net.Mail;

namespace BloodLink.Helpers
{
    // Sends emails through an SMTP server (e.g. Gmail). Settings: appsettings.json → "Email".
    // Put the password in User Secrets ("Email:Password"), never in Git.
    //
    //   if (!emailSender.IsSetUp) ...
    //   emailSender.Send("donor@example.com", "Subject", "<p>Hello</p>");
    public class EmailSender
    {
        private readonly IConfiguration config;

        public EmailSender(IConfiguration config)
        {
            this.config = config;
        }

        public bool IsSetUp =>
            !string.IsNullOrEmpty(config["Email:Host"]) &&
            !string.IsNullOrEmpty(config["Email:UserName"]) &&
            !string.IsNullOrEmpty(config["Email:Password"]);

        public void Send(string to, string subject, string htmlBody)
        {
            string from = string.IsNullOrEmpty(config["Email:FromAddress"]) ? config["Email:UserName"]! : config["Email:FromAddress"]!;

            using var message = new MailMessage
            {
                From = new MailAddress(from, config["Email:FromName"] ?? "BloodLink"),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };
            message.To.Add(to);

            using var smtp = new SmtpClient(config["Email:Host"], int.Parse(config["Email:Port"] ?? "587"))
            {
                EnableSsl = config["Email:EnableSsl"] != "false" && config["Email:EnableSsl"] != "False",
                Credentials = new NetworkCredential(config["Email:UserName"], config["Email:Password"])
            };
            smtp.Send(message);
        }
    }
}
