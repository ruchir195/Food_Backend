using AutoMapper;
using Backend.Backend.Repository.IRepository;
using Backend.Context;
using Backend.Dto;
using Backend.Models;
using Microsoft.AspNetCore.Authorization;
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

        [Authorize]
        [HttpPost("MealBooking")]
        public async Task<IActionResult> MealBooking(BookingDTO booking)
        {

            if (booking != null)
            {

                var user = await repository.GetUserByEmailAsync(booking.Email);

                if (user == null)
                {
                    return NotFound("User not found");
                }
                


                // Increment booking start date by one day
                DateTime incrementedBookingStartDate = booking.BookingStartDate.AddDays(1);
                bool canStartNewBooking = await repository.CanStartNewBookingAsync(user.Id, incrementedBookingStartDate);

                if (!canStartNewBooking)
                {
                    return BadRequest("User cannot start a new booking before the end date of the recent booking.");
                }

                DateTime bookingStartDate = incrementedBookingStartDate;
                DateTime bookingEndDate = (booking.BookingEndDate ?? incrementedBookingStartDate).AddDays(1).Date; // Use start date if end date is not provided
                DateTime threeMonthsFromNow = DateTime.Today.AddMonths(3);

                if (bookingStartDate > threeMonthsFromNow)
                {
                    return BadRequest("User can only book within three months from the current date.");
                }

                if (booking.BookingStartDate.Date <= DateTime.Today)
                {
                    if (booking.BookingStartDate.Hour <= 20)
                    {
                        return BadRequest("You cannot book lunch or dinner after 8 PM.");
                    }
                    return BadRequest("User Can not book meal today or any past dates.");
                }

               

                // Iterate over each day in the booking period and check for existing bookings
                for (DateTime date = bookingStartDate.Date; date <= bookingEndDate.Date; date = date.AddDays(1))
                {
                    var isAlreadyBooked = await repository.GetExistingBookingAsync(user.Id, date);
                    if (isAlreadyBooked != null)
                    {
                        return BadRequest($"Meal already booked on {date.ToShortDateString()}.");
                    }
                }




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
                    Message = "Meal Booked SucessFully..!"
                });
            }

            else
            {
                BadRequest("Booking not found");
            }
            return Ok(booking);
        }


        [Authorize]
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

        [Authorize]
        [HttpDelete("{date}")]
        public async Task<IActionResult> CancelBooking(DateTime date, [FromQuery] string email)
        {
            var user = await repository.GetUserByEmailAsync(email);

            if (user == null)
            {
                return NotFound("User not found");
            }

            var tomorrowDate = DateTime.Today.AddDays(1);

            // Check if the requested date is for tomorrow
            if (date.Date != tomorrowDate)
            {
                return BadRequest("Can only cancel bookings for tomorrow's date.");
            }
            var currentTime = DateTime.Now;
            var cutoffTime = new DateTime(currentTime.Year, currentTime.Month, currentTime.Day, 22, 0, 0); // 10:00 PM
            if (currentTime >= cutoffTime)
            {
                return BadRequest("Cannot cancel tomorrow's bookings after 10 PM today.");
            }

            var isCancelled = await repository.CancelBookingsByDateAsync(date);
            if (!isCancelled)
            {
                return NotFound("No bookings found for tomorrow's date.");
            }


            // Create and save the notification
            var notification = new Notification
            {
                UserId = user.Id,
                Message = $"Your meal has been cancelled for {date.ToShortDateString()}",
                TimeStamp = DateTime.UtcNow
            };
            await repository.AddNotificationAsync(notification);

            return Ok(new
            {
                StatusCode = 200,
                Message = "Bookings for tomorrow cancelled successfully."
            });
        }





        [Authorize]
        [HttpPost("QuickBooking")]
        public async Task<IActionResult> QuickBooking(BookingDTO booking)
        {

            if (booking != null)
            {
                var user = await repository.GetUserByEmailAsync(booking.Email);

                if (user == null)
                {
                    return NotFound("User not found");
                }

                // Check if the user has an existing meal booking
                var existingBooking = await repository.GetExistingBookingAsync(user.Id, booking.BookingStartDate);
                if (existingBooking != null)
                {
                    return BadRequest("User already has a meal booked for this date.");
                }

                //  Check if the user exists and can book a meal
                bool canBookMeal = repository.CanUserBookMealAsync(user.Id).Result;

                DateTime incrementedBookingStartDate = booking.BookingStartDate.AddDays(1);
                bool canStartNewBooking = await repository.CanStartNewBookingAsync(user.Id, incrementedBookingStartDate);

                if (!canStartNewBooking)
                {
                    return BadRequest("User cannot start a new booking before the end date of the recent booking.");
                }

                DateTime bookingStartDate = booking.BookingStartDate.AddDays(1).Date;
                DateTime threeMonthsFromNow = DateTime.Today.AddMonths(3);

                if (bookingStartDate > threeMonthsFromNow)
                {
                    return BadRequest("User can only book within three months from the current date.");
                }


                if (booking.BookingStartDate.Date <= DateTime.Today)
                {
                    if (booking.BookingStartDate.Hour >= 20)
                    {
                        return BadRequest("You cannot book lunch or dinner after 8 PM.");
                    }
                    return BadRequest("User Can not book meal today or any past dates.");
                }

                var mealboooking = mapper.Map<BookingModel>(booking);
                mealboooking.UserID = user.Id;
                mealboooking.User = user;

                repository.Insert(mealboooking);
                return Ok(new
                {
                    StatusCode = 200,
                    Message = "Quick Meal Booked SucessFully..!"
                });

            }
            return Ok(booking);

        }
    }
}
