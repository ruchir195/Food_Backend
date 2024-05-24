using Backend.Context;
using Backend.Dto;
using Backend.Models;
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
        public CoupenController(AppDbContext appDbContext)
        {
            _authContext = appDbContext;
        }


        [HttpPost("AddData")]
        public async Task<IActionResult> CreateCoupon([FromBody] CoupenRequestDto request)
        {
            if (request == null)
            {
                return BadRequest("Invalid request data");
            }


              var user = await _authContext.Users.FirstOrDefaultAsync(x => x.Email == request.Email);

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
