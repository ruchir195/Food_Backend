using Backend.Models;

namespace Backend.UtilityServices
{
    public interface IEmailService
    {
        void SendEmail(EmailModel emailModel);
    }
}
