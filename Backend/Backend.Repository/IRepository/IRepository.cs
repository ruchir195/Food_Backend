using Backend.Models;

namespace Backend.Backend.Repository.IRepository
{
    public interface IRepository<TEntity> where TEntity : class
    {
        Task<User> GetUserByEmailAsync(string email);
        Task<User> UpdateUserAsync(User user);

        Task<bool> CheckUserNameExistAsync(string username);
        Task<bool> CheckEmailExistAsync(string email);
        Task<User> CreateUserAsync(User user);


        Task<User> GetUserByUsernameAsync(string username);


        IQueryable<BookingModel> GetBookingsByUserId(int userId);
        void Insert(BookingModel objBooking);
        Task<BookingModel> GetBookingByID(int id);
        Task<bool> CancelBookingsByDateAsync(DateTime date);
        Task<bool> CanStartNewBookingAsync(int userId, DateTime newBookingStartDate);
        Task<bool> CanUserBookMealAsync(int userId);

        Task<BookingModel> GetExistingBookingAsync(int userId, DateTime bookingStartDate);



        Task AddNotificationAsync(Notification notification);
    }
}
