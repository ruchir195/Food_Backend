using Backend.Backend.Repository.IRepository;
using Backend.Context;
using Backend.Dto;
using Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CoupenController : ControllerBase
    {

        private readonly AppDbContext _authContext;
        private readonly IRepository<User> _userRepository;
        public CoupenController(AppDbContext appDbContext, IRepository<User> userRepository)
        {
            _authContext = appDbContext;
            _userRepository = userRepository;
        }


        [Authorize]
        [HttpPost("AddData")]
        public async Task<IActionResult> CreateCoupon([FromBody] int userID)
        {
            if (userID == null)
            {
                return BadRequest("Invalid request data");
            }


              var user = await _userRepository.GetUserByIdAsync(userID);

             if (user == null)
             {
                  return NotFound("User not found");
             }


            var coupon = new CoupenDb
            {
                coupenCode = GenerateRandomAlphanumericCode(16),
                createdTime = DateTime.Now,
                ExpirationTime = DateTime.Now.AddMinutes(1),
                userID = (int)user.Id
            };

            _authContext.CoupenDbs.Add(coupon);
            await _authContext.SaveChangesAsync();

            // return CreatedAtAction(nameof(GetCouponById), new { id = coupon.Id }, coupon);
            return Ok(new
            {
                StatusCode = 200,
                Message = "Coupon send successful",
                coupon = coupon
            });

        }




        private static string GenerateRandomAlphanumericCode(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            var result = new StringBuilder(length);

            for (int i = 0; i < length; i++)
            {

                result.Append(chars[random.Next(chars.Length)]);
            }

            return result.ToString();
        }
    }
}
