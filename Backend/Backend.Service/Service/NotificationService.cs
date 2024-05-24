using Backend.Backend.Service.IUtilityService;
using Backend.Context;
using Backend.Dto;
using Microsoft.EntityFrameworkCore;

namespace Backend.Backend.Service.Service
{
    public class NotificationService : INotificationService
    {
        private readonly AppDbContext _appDbContext;

        public NotificationService(AppDbContext appDbContext)
        {
            _appDbContext = appDbContext;
        }

        public async Task<Notification> CreateNotification(Notification notification)
        {
            Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<Models.Notification> entityEntry  = _appDbContext.Notifications.Add(notification);
            await _appDbContext.SaveChangesAsync();
            return notification;
        }

        public async Task<IEnumerable<Notification>> GetAllNotifications()
        {
            return (IEnumerable<Notification>)await _appDbContext.Notifications.ToListAsync();
        }

        public async Task<Notification> GetNotificationById(int id)
        {
            return await _appDbContext.Notifications.FindAsync(id);
        }

        public async Task DeleteNotification(int id)
        {
            var notification = await _appDbContext.Notifications.FindAsync(id);
            if (notification != null)
            {
                _appDbContext.Notifications.Remove(notification);
                await _appDbContext.SaveChangesAsync();
            }
        }

        public async Task DeleteAllNotifications()
        {
            _appDbContext.Notifications.RemoveRange(_appDbContext.Notifications);
            await _appDbContext.SaveChangesAsync();
        }

        public Task<Models.Notification> CreateNotification(Models.Notification notification)
        {
            throw new NotImplementedException();
        }

        Task<IEnumerable<Models.Notification>> INotificationService.GetAllNotifications()
        {
            throw new NotImplementedException();
        }

        Task<Models.Notification> INotificationService.GetNotificationById(int id)
        {
            throw new NotImplementedException();
        }
    }
}
