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
                    client.Connect(configuration["EmailSettings:SmtpServer"], 465, true);
                    client.Authenticate(configuration["EmailSettings:From"], configuration["EmailSettings:Password"]);
                    client.Send(emailMessage);
                }
                catch (Exception ex)
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
