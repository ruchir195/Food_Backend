using Backend.Context;
using Backend.Models;
using Backend.Models.Dto;
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


        [HttpPost("coupon")]
        public async Task<IActionResult> CreateCoupon([FromBody] CoupenRequestDto request)
        {
            var coupon = new CoupenDb
            {
                coupenCode = GenerateRandomAlphanumericCode(16),
                createdTime = DateTime.Now,
                ExpirationTime = DateTime.Now.AddMinutes(1)
            };

            _authContext.CoupenDbs.Add(coupon);
            await _authContext.SaveChangesAsync();

            //return CreatedAtAction(nameof(GetCouponById), new { id = coupon.Id }, coupon);

            return Ok(coupon);

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
