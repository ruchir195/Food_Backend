using AutoMapper;
using Backend.Backend.Repository.IRepository;
using Backend.Context;
using Backend.Dto;
using Backend.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BookingController : ControllerBase
    {
        private readonly IRepository<BookingDTO> repository;
        private readonly IMapper mapper;

        public BookingController(IRepository<BookingDTO> repository, IMapper mapper)
        {
            this.repository = repository;
            this.mapper = mapper;
        }


        [HttpPost("MealBooking")]
        public async Task<IActionResult> MealBooking(BookingDTO booking)
        {
            if (booking == null)
            {
                return BadRequest("Booking not found");
            }

            var user = await repository.GetUserByEmailAsync(booking.Email);

            if (user == null)
            {
                return NotFound("User not found");
            }


            // Increment booking start date by one day
            DateTime incrementedBookingStartDate = booking.BookingStartDate.AddDays(1);

            // Check if the user has an existing meal booking
            var existingBooking = await repository.GetExistingBookingAsync(user.Id, incrementedBookingStartDate, booking.BookingType);
            if (existingBooking != null)
            {
                return BadRequest($"User already has a {booking.BookingType} booked for this date.");
            }

          
            //bool canStartNewBooking = await repository.CanStartNewBookingAsync(user.Id, incrementedBookingStartDate);

            //if (!canStartNewBooking)
            //{
            //    return BadRequest("User cannot start a new booking before the end date of the recent booking.");
            //}

            DateTime bookingStartDate = incrementedBookingStartDate;
            DateTime bookingEndDate = (booking.BookingEndDate ?? incrementedBookingStartDate).AddDays(1).Date; // Use start date if end date is not provided
            DateTime threeMonthsFromNow = DateTime.Today.AddMonths(3);

            if (bookingStartDate > threeMonthsFromNow)
            {
                return BadRequest("User can only book within three months from the current date.");
            }

            if (booking.BookingStartDate.Date < DateTime.Today)
            {
                return BadRequest("User cannot book a meal for past dates.");
            }
            else if (booking.BookingStartDate.Date == DateTime.Today)
            {
                if (DateTime.Now.Hour > 20)
                {
                    return BadRequest("You cannot book lunch or dinner after 8 PM.");
                }
            }

            //// Iterate over each day in the booking period and check for existing bookings
            //for (DateTime date = bookingStartDate.Date; date <= bookingEndDate.Date; date = date.AddDays(1))
            //{
            //    var isAlreadyBooked = await repository.GetExistingBookingAsync(user.Id, date , booking.BookingType);
            //    if (isAlreadyBooked != null)
            //    {
            //        return BadRequest($"Meal already booked on {date.ToShortDateString()}.");
            //    }
            //}

            // Iterate over each day in the booking period and save separate entries for each day
            for (DateTime date = bookingStartDate.Date; date <= bookingEndDate.Date; date = date.AddDays(1))
            {
                var mealBooking = mapper.Map<BookingModel>(booking);
                mealBooking.UserID = user.Id;
                mealBooking.User = user;
                mealBooking.BookingStartDate = date; // Update booking start date to the current date of iteration
                mealBooking.BookingEndDate = date;
                repository.Insert(mealBooking);
            }

            return Ok(new
            {
                StatusCode = 200,
                Message = "Meal Booked Successfully!"
            });
        }


        [HttpGet("ViewBooking")]
        public async Task<IActionResult> ViewBooking([FromQuery] string email)
        {
            var user = await repository.GetUserByEmailAsync(email);

            if (user == null)
            {
                return NotFound("User not found");
            }

            var booking = await repository.GetBookingsByUserId(user.Id);
            if (booking == null)
            {
                return NotFound("No booking Found for specific userID");
            }


            return Ok(booking);
        }

        [HttpDelete("{date}")]
        public async Task<IActionResult> CancelBooking(DateTime date, [FromQuery] string email, [FromQuery] string bookingtype)
        {

            var user = await repository.GetUserByEmailAsync(email);

            if (user == null)
            {
                return NotFound("User not found");
            }

            // Increment booking start date by one day
            DateTime incrementedBookingDate = date.AddDays(1);
            //var tomorrowDate = DateTime.Today.AddDays(1);

            //// Check if the requested date is for tomorrow
            //if (date.Date != tomorrowDate)
            //{
            //    return BadRequest("Can only cancel bookings for tomorrow's date.");
            //}
            var currentTime = DateTime.Now;
            var cutoffTime = new DateTime(currentTime.Year, currentTime.Month, currentTime.Day, 22, 0, 0); // 10:00 PM
            if (currentTime >= cutoffTime)
            {
                return BadRequest("Cannot cancel tomorrow's bookings after 10 PM today.");
            }



            // Validate the bookingtype parameter
            if (bookingtype != "Lunch" && bookingtype != "Dinner" && bookingtype != "Both")
            {
                return BadRequest("Invalid booking type. Valid values are 'lunch', 'dinner', or 'both'.");
            }

            bool isLunchCancelled = false;
            bool isDinnerCancelled = false;

            // Cancel bookings based on the booking type
            if (bookingtype == "Lunch" || bookingtype == "Both")
            {
                isLunchCancelled = await repository.CancelBookingsByDateAsync(incrementedBookingDate, "Lunch");
            }
            if (bookingtype == "Dinner" || bookingtype == "Both")
            {
                isDinnerCancelled = await repository.CancelBookingsByDateAsync(incrementedBookingDate, "Dinner");
            }

            if (!isLunchCancelled && !isDinnerCancelled)
            {
                return NotFound($"No bookings found for the specified {incrementedBookingDate.ToShortDateString()} and {bookingtype}");
            }

            // Create and save the notification
            var notificationMessage = "Your booking(s) have been cancelled for " + incrementedBookingDate.ToShortDateString() + ":";
            if (isLunchCancelled) notificationMessage += " Lunch";
            if (isDinnerCancelled) notificationMessage += " Dinner";


            //var isCancelled = await repository.CancelBookingsByDateAsync(date, bookingtype);
            //if (!isCancelled)
            //{
            //    return NotFound("No bookings found for tomorrow's date.");
            //}
            // Create and save the notification
            var notification = new Notification
            {
                UserId = user.Id,
                Message = $"Your {bookingtype} meal has been cancelled for {incrementedBookingDate.ToShortDateString()}",
                TimeStamp = DateTime.UtcNow
            };
            await repository.AddNotificationAsync(notification);

            return Ok(new
            {
                StatusCode = 200,
                Message = $"Bookings for {incrementedBookingDate.ToShortDateString()} cancelled successfully."
            });
        }






        [HttpPost("QuickBooking")]
        public async Task<IActionResult> QuickBooking(BookingDTO booking)
        {


            if (booking == null)
            {
                return BadRequest("Booking not found");
            }

            var user = await repository.GetUserByEmailAsync(booking.Email);

            if (user == null)
            {
                return NotFound("User not found");
            }

   
            // Check if the user has an existing meal booking
            var existingBooking = await repository.GetExistingBookingAsync(user.Id, booking.BookingStartDate.Date, booking.BookingType);
            if (existingBooking != null)
            {
                return BadRequest($"User already has a {booking.BookingType} booked for this date.");
            }

            // Check if the user exists and can book a meal
            //bool canBookMeal = await repository.CanUserBookMealAsync(user.Id);
            //if (!canBookMeal)
            //{
            //    return BadRequest("User is not allowed to book a meal.");
            //}

            var bookingStartDate = booking.BookingStartDate.Date;
            DateTime threeMonthsFromNow = DateTime.Today.AddMonths(3);

            if (bookingStartDate > threeMonthsFromNow)
            {
                return BadRequest("User can only book within three months from the current date.");
            }

            // Check if the booking date is today
            if (booking.BookingStartDate.Date == DateTime.Today)
            {
                // Allow booking if current time is before 8 PM
                if (DateTime.Now.Hour >= 20)
                {
                    return BadRequest("You cannot book lunch or dinner after 8 PM.");
                }
            }
           


            var mealBooking = mapper.Map<BookingModel>(booking);
            mealBooking.UserID = user.Id;
            mealBooking.User = user;
            mealBooking.BookingStartDate = bookingStartDate;
            mealBooking.BookingEndDate = bookingStartDate;
            repository.Insert(mealBooking);

            return Ok(new
            {
                StatusCode = 200,
                Message = "Quick Meal Booked Successfully!"
            });
        }
    }
}
