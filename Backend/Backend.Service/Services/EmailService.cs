using Backend.Models;
using MimeKit;
using System.Net.Mail;
using MailKit.Net.Smtp;
using Backend.Backend.Service.IUtilityService;

namespace Backend.Backend.Service.UtilityServices
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration configuration;

        public EmailService(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public void SendEmail(EmailModel emailModel)
        {
            var emailMessage = new MimeMessage();

            var from = configuration["EmailSettings:From"];
            emailMessage.From.Add(new MailboxAddress("Meal Facility", from));
            emailMessage.To.Add(new MailboxAddress(emailModel.To, emailModel.To));
            emailMessage.Subject = emailModel.Subject;
            emailMessage.Body = new TextPart(MimeKit.Text.TextFormat.Html)
            {
                Text = string.Format(emailModel.Content)
            };

            using (var client = new MailKit.Net.Smtp.SmtpClient())
            {
                try
                {
                    var smtpServer = configuration["EmailSettings:SmtpServer"];
                    var port = configuration.GetValue("EmailSettings:Port", 465);
                    var username = configuration["EmailSettings:Username"];
                    var password = configuration["EmailSettings:Password"];

                    client.Connect(smtpServer, port, true);
                    client.Authenticate(username, password);
                    client.Send(emailMessage);
                }
                catch
                {
                    throw;
                }
                finally
                {
                    client.Disconnect(true);
                    client.Dispose();
                }
            }

        }
    }
}
