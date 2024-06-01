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

        public async Task<User> GetUserByIdAsync(int id)
        {
            return await _authContext.Users.FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<User> UpdateUserAsync(User user)
        {
            _authContext.Update(user);
            await _authContext.SaveChangesAsync();
            return user;
        }



        // register user
        public async Task<bool> CheckUserNameExistAsync(string email)
        {
            return await _authContext.Users.AnyAsync(u => u.Email == email);
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





        public async Task<User> GetUserByUsernameAsync(string id)
        {
            if (int.TryParse(id, out int userId))
            {
                return await _authContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
            }
            return null; // or throw an appropriate exception or handle the error as needed
        }



        //public IQueryable<BookingModel> GetBookingsByUserId(int userId)
        //{
        //    return _authContext.Bookings.Where(b => b.UserID == userId);
        //}



        public async Task<List<BookingModel>> GetBookingsByUserId(int userId)
        {
            var bookings = await _authContext.Bookings
                 .Where(b => b.UserID == userId && b.BookingStartDate >= DateTime.Today)
                 .OrderBy(b => b.BookingStartDate)
                 .ToListAsync();
            return bookings;
        }

        public async Task<bool> CancelBookingsByDateAsync(DateTime date, string BookingType)
        {
            //var tomorrowDate = DateTime.Today.AddDays(1);

            // Check if the requested date is for tomorrow
            //if (date.Date != tomorrowDate)
            //{
            //    return false; // Can only cancel bookings for tomorrow's date
            //}

            // Check if the current time is before 10 PM
            var currentTime = DateTime.Now;
           // var cutoffTime = new DateTime(currentTime.Year, currentTime.Month, currentTime.Day, 22, 0, 0); // 10:00 PM
           // if (currentTime >= cutoffTime)
           // {
           //     return false; // Cannot cancel tomorrow's bookings after 10 PM today
           // }

            var bookingsToCancel = await _authContext.Bookings
                .Where(b => b.BookingStartDate.Date == date.Date && b.BookingType == BookingType)
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
                    booking.BookingEndDate = booking.BookingStartDate; // Set the end date to the start date to cancel the booking

                }


                booking.BookingEndDate = booking.BookingStartDate; // Set the end date to the start date to cancel the booking
                _authContext.Bookings.Remove(booking);

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


        public async Task<BookingModel> GetExistingBookingAsync(int userId, DateTime bookingStartDate, string BookingType)
        {
            // Assuming you have access to your database context or repository here
            // Query the database to check if the user has an existing booking for the given date
            var existingBooking = await _authContext.Bookings
                                                .FirstOrDefaultAsync(b => b.UserID == userId &&
                                                                          b.BookingStartDate.Date == bookingStartDate.Date && b.BookingType == BookingType);

            return existingBooking;
        }




        public async Task AddNotificationAsync(Notification notification)
        {
            _authContext.Notifications.Add(notification);
            await _authContext.SaveChangesAsync();
        }

        public async Task<(bool HasLunchBooking, bool HasDinnerBooking)> GetBookingsForTomorrowAsync(string email)
        {
            var user = await GetUserByEmailAsync(email);
            if (user == null) return (false, false);

            var tomorrowDate = DateTime.Today.AddDays(1);
            var lunchBooking = await _authContext.Bookings.AnyAsync(b => b.UserID == user.Id && b.BookingStartDate == tomorrowDate && b.BookingType == "lunch");
            var dinnerBooking = await _authContext.Bookings.AnyAsync(b => b.UserID == user.Id && b.BookingStartDate == tomorrowDate && b.BookingType == "dinner");

            return (lunchBooking, dinnerBooking);
        }
    }
}



