using Backend.Models;

namespace Backend.UtilityServices
{
    public interface INotificationService
    {
        Task<Notification> CreateNotification(Notification notification);
        Task<IEnumerable<Notification>> GetAllNotifications();
        Task<Notification> GetNotificationById(int id);
        Task DeleteNotification(int id);
        Task DeleteAllNotifications();
    }
}
