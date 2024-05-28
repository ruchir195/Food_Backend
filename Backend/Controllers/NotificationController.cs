using Backend.Backend.Repository.IRepository;
using Backend.Backend.Service.IUtilityService;
using Backend.Context;
using Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;

namespace Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _notificationService;
        private readonly IRepository<User> _userRepository;


        public NotificationController(INotificationService notificationService, IRepository<User> userRepository)
        {
            _notificationService = notificationService;
            _userRepository = userRepository;
        }

        [Authorize]
        [HttpGet("getNotifications")]
        public async Task<IActionResult> GetNotifications([FromQuery] string email)
        {
            var user = await _userRepository.GetUserByEmailAsync(email);
            if (user == null)
            {
                return NotFound(new
                {
                    StatusCode = 404,
                    Message = "User not found"
                });
            }

            var notifications = await _notificationService.GetNotificationById(user.Id);

            return Ok(new
            {
                StatusCode = 200,
                Notifications = notifications
            });
        }
    }
}
