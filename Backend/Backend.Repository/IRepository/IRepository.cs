using Backend.Models;

namespace Backend.Backend.Repository.IRepository
{
    public interface IRepository<TEntity> where TEntity : class
    {
        Task<User> GetUserByEmailAsync(string email);
        Task<User> GetUserByIdAsync(int id);
        Task<User> UpdateUserAsync(User user);

        Task<bool> CheckUserNameExistAsync(string email);
        Task<bool> CheckEmailExistAsync(string email);
        Task<User> CreateUserAsync(User user);


        Task<User> GetUserByUsernameAsync(string? id);


        // IQueryable<BookingModel> GetBookingsByUserId(int userId);

        Task<List<BookingModel>> GetBookingsByUserId(int userId);
        void Insert(BookingModel objBooking);
        Task<BookingModel> GetBookingByID(int id);
        Task<bool> CancelBookingsByDateAsync(DateTime date, string bookingtype);
        Task<bool> CanStartNewBookingAsync(int userId, DateTime newBookingStartDate);
        Task<bool> CanUserBookMealAsync(int userId);

        Task<BookingModel> GetExistingBookingAsync(int userId, DateTime bookingStartDate, string BookingType);
        Task<(bool HasLunchBooking, bool HasDinnerBooking)> GetBookingsForTomorrowAsync(string email);
        Task AddNotificationAsync(Notification notification);
    }
}
