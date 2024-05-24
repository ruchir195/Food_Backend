﻿using Backend.Models;

namespace Backend.Backend.Service.IUtilityService
{
    public interface IEmailService
    {
        void SendEmail(EmailModel emailModel);
        void SendEmail(Dto.EmailModel emailModel);
    }
}
