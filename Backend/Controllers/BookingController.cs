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

            if (booking != null)
            {

                var user = await repository.GetUserByEmailAsync(booking.Email);

                if (user == null)
                {
                    return NotFound("User not found");
                }
                //  Check if the user exists and can book a meal
                bool canBookMeal = repository.CanUserBookMealAsync(user.Id).Result;

                //if (!canBookMeal)
                //{
                //    return BadRequest("User has already booked a meal.");
                //}

                bool canStartNewBooking = await repository.CanStartNewBookingAsync(user.Id, booking.BookingStartDate);

                if (!canStartNewBooking)
                {
                    return BadRequest("User cannot start a new booking before the end date of the recent booking.");
                }

                DateTime bookingStartDate = booking.BookingStartDate;
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

                if (booking.BookingEndDate.Date <= booking.BookingStartDate.Date)
                {
                    return BadRequest("booking end date is not smaller than booking start date.");
                }



                var mealboooking = mapper.Map<BookingModel>(booking);
                mealboooking.UserID = user.Id;
                mealboooking.User = user;

                repository.Insert(mealboooking);
                

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


        [HttpGet("ViewBooking")]
        public async Task<IActionResult> ViewBooking([FromQuery] string email)
        {
            var user = await repository.GetUserByEmailAsync(email);

            if (user == null)
            {
                return NotFound("User not found");
            }

            var booking = repository.GetBookingsByUserId(user.Id);
            if (booking == null)
            {
                return NotFound("No booking Found for specific userID");
            }
            return Ok(booking);
        }

        [HttpDelete]
        public async Task<IActionResult> CancelBooking(DateTime date)
        {
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

            return Ok("Bookings for tomorrow cancelled successfully.");
        }






        //[HttpPost("QuickBooking")]
        //public async Task<IActionResult> QuickBooking(BookingDTO booking)
        //{

        //    if (booking != null)
        //    {

        //        bool canStartNewBooking = await repository.CanStartNewBookingAsync(booking.UserID, booking.BookingStartDate);

        //        if (!canStartNewBooking)
        //        {
        //            return BadRequest("User cannot start a new booking before the end date of the recent booking.");
        //        }

        //        DateTime bookingStartDate = booking.BookingStartDate;
        //        DateTime threeMonthsFromNow = DateTime.Today.AddMonths(3);

        //        if (bookingStartDate > threeMonthsFromNow)
        //        {
        //            return BadRequest("User can only book within three months from the current date.");
        //        }


        //        if (booking.BookingStartDate.Date <= DateTime.Today)
        //        {
        //            if (booking.BookingStartDate.Hour >= 20)
        //            {
        //                return BadRequest("You cannot book lunch or dinner after 8 PM.");
        //            }
        //            return BadRequest("User Can not book meal today or any past dates.");
        //        }

        //        var mealbooking = mapper.Map<BookingDTO, BookingModel>(booking);

        //        repository.Insert(mealbooking);
        //        return Ok("Meal Booked SucessFully");

        //    }
        //    return Ok();

        //}
    }
}
