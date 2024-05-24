using Backend.Backend.Repository.IRepository;
using Backend.Context;
using Backend.Helpers;
using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Backend.Repository.Repository
{
    public class Repositiory<TEntity> : IRepository<TEntity> where TEntity : class
    {
        private readonly AppDbContext _authContext;

        public Repositiory(AppDbContext authContext)
        {
            _authContext = authContext;
        }


        // login user
        public async Task<User> GetUserByEmailAsync(string email)
        {
            return await _authContext.Users.FirstOrDefaultAsync(x => x.Email == email);
        }

        public async Task<User> UpdateUserAsync(User user)
        {
            _authContext.Update(user);
            await _authContext.SaveChangesAsync();
            return user;
        }



        // register user
        public async Task<bool> CheckUserNameExistAsync(string username)
        {
            return await _authContext.Users.AnyAsync(u => u.Username == username);
        }

        public async Task<bool> CheckEmailExistAsync(string email)
        {
            return await _authContext.Users.AnyAsync(u => u.Email == email);
        }

        public async Task<User> CreateUserAsync(User user)
        {
            user.Password = PasswordHasher.HashPassword(user.Password);
            user.Role = "User";
            user.Token = "";

            await _authContext.Users.AddAsync(user);
            await _authContext.SaveChangesAsync();

            return user;
        }





        public async Task<User> GetUserByUsernameAsync(string username)
        {
            return await _authContext.Users.FirstOrDefaultAsync(u => u.Username == username);
        }





        // show the first name in navbar
        public async Task<User> GetUserByUniqueName(string uniqueName)
        {
            return await _authContext.Users.FirstOrDefaultAsync(u => u.Username == uniqueName);
        }

        public IQueryable<BookingModel> GetBookingsByUserId(int userId)
        {
            return _authContext.Bookings.Where(b => b.UserID == userId);
        }

        public async Task<bool> CancelBookingsByDateAsync(DateTime date)
        {
            var tomorrowDate = DateTime.Today.AddDays(1);

            // Check if the requested date is for tomorrow
            if (date.Date != tomorrowDate)
            {
                return false; // Can only cancel bookings for tomorrow's date
            }

            // Check if the current time is before 10 PM
            var currentTime = DateTime.Now;
            var cutoffTime = new DateTime(currentTime.Year, currentTime.Month, currentTime.Day, 22, 0, 0); // 10:00 PM
            if (currentTime >= cutoffTime)
            {
                return false; // Cannot cancel tomorrow's bookings after 10 PM today
            }

            var bookingsToCancel = await _authContext.Bookings
                .Where(b => b.BookingStartDate.Date == date.Date)
                .ToListAsync();

            if (bookingsToCancel == null || !bookingsToCancel.Any())
            {
                return false; // No bookings found for tomorrow's date
            }

            foreach (var booking in bookingsToCancel)
            {
                // Check if it's a future booking
                if (DateTime.Now.Date >= booking.BookingStartDate.Date)
                {
                    continue; // Cannot cancel past or present bookings
                }

                // Update the booking status
                if (booking.BookingStartDate == booking.BookingEndDate)
                {
                    _authContext.Bookings.Remove(booking);
                }
                else
                {
                    booking.BookingEndDate = booking.BookingStartDate; // Set the end date to the start date to cancel the booking
                }
            }

            await _authContext.SaveChangesAsync();
            return true;
        }





        public void Insert(BookingModel objBooking)
        {
            _authContext.Bookings.Add(objBooking);
            _authContext.SaveChanges();
        }

        public async Task<BookingModel> GetBookingByID(int id)
        {
            return await _authContext.Bookings.FirstOrDefaultAsync(b => b.Id == id);
        }

        public async Task<bool> CanUserBookMealAsync(int userId)
        {
            bool hasBookings = await _authContext.Bookings.AnyAsync(m => m.UserID == userId && m.ISBooked);

            if (!hasBookings)
            {
                return true; // If no bookings exist, user can book a meal
            }

            // Get the user's most recent booking end date
            DateTime? recentBookingEndDate = await _authContext.Bookings
                .Where(m => m.UserID == userId && m.ISBooked)
                .OrderByDescending(m => m.BookingEndDate)
                .Select(m => m.BookingEndDate)
                .FirstOrDefaultAsync();

            // Check if the recent booking end date has passed
            return recentBookingEndDate.HasValue && recentBookingEndDate < DateTime.Now;
        }

        public async Task<bool> CanStartNewBookingAsync(int userId, DateTime newBookingStartDate)
        {
            // Get the end date of the user's most recent booking
            DateTime? recentBookingEndDate = await _authContext.Bookings
                .Where(m => m.UserID == userId && m.ISBooked)
                .OrderByDescending(m => m.BookingEndDate)
                .Select(m => m.BookingEndDate)
                .FirstOrDefaultAsync();

            // If there are no previous bookings, user can start a new booking
            if (!recentBookingEndDate.HasValue)
            {
                return true;
            }

            // Check if the start date of the new booking is after the end date of the recent booking
            return newBookingStartDate > recentBookingEndDate;
        }

    }
}



