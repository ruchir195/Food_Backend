using Backend.Helpers;
using Backend.Context;
using Backend.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Backend.Models.Dto;
using Backend.UtilityServices;

namespace Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly AppDbContext _authContext;
        private readonly IConfiguration _configration;
        private readonly IEmailService _emailService;

        public UserController(AppDbContext appDbContext, IConfiguration configration, IEmailService emailService)
        {
            _authContext = appDbContext;
            _configration = configration;
            _emailService = emailService;
        }

        [HttpPost("authenticate")]
        public async Task<IActionResult> Authenticate([FromBody] User userObj)
        {
            if (userObj == null)
            {
                return BadRequest();
            }

            var user = await _authContext.Users.FirstOrDefaultAsync(x => x.Email == userObj.Username);

            if (user == null)
                return NotFound(new { message = "User Not Found!" });


            if (!PasswordHasher.VerifyPassword(userObj.Password, user.Password))
            {
                return BadRequest(new { Message = "Password is incorrect" });
            }
            user.Token = CreateJwtToken(user);
            var newAccessToken = user.Token;
            var newRefreshToken = CreateRefreshToken();
            user.RefreshToken = newRefreshToken;
            user.RefreshTokenExpireTime = DateTime.Now.AddDays(5);
            await _authContext.SaveChangesAsync();

            return Ok(new TokenApiDto()
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken
            });
        }


        [HttpPost("register")]
        public async Task<IActionResult> RegisterUser([FromBody] User userObj)
        {
            try
            {
                if (userObj == null)
                {
                    return BadRequest();
                }

                // Check username

                if (await CheckUserNameExistAsync(userObj.Username))
                {
                    return BadRequest(new { Message = "Username already Exist!" });
                }

                // Check Email

                if (await CheckEmailExistAsync(userObj.Email))
                {
                    return BadRequest(new { Message = "Email already Exist!" });
                }


                // Check Password Strength
                var pass = CheckPasswordStength(userObj.Password);
                if (!string.IsNullOrEmpty(pass))
                {
                    return BadRequest(new { Message = pass.ToString() });
                }


                userObj.Password = PasswordHasher.HashPassword(userObj.Password);
                userObj.Role = "User";
                userObj.Token = "";


                await _authContext.Users.AddAsync(userObj);
                await _authContext.SaveChangesAsync();

                return Ok(new
                {
                    message = "User Registered"
                });
            }
            catch (Exception e)
            {
                // Log the exception for debugging purposes
                Console.WriteLine($"An error occurred during user registration: {e.Message}");
                return StatusCode(500, new { message = "An error occurred during user registration" });
            }
        }



        private async Task<bool> CheckUserNameExistAsync(string username)
        {
            return await _authContext.Users.AnyAsync(x => x.Username == username);
        }


        private async Task<bool> CheckEmailExistAsync(string email)
        {
            return await _authContext.Users.AnyAsync(x => x.Email == email);
        }


        private string CheckPasswordStength(string password)
        {
            StringBuilder sb = new StringBuilder();
            if (password.Length < 8)
            {
                sb.Append("Minimum password length should be 8" + Environment.NewLine);
            }

            if (!(Regex.IsMatch(password, "[a-z]") && Regex.IsMatch(password, "[A-Z]")
                && Regex.IsMatch(password, "[0-9]")))
            {
                sb.Append("Password should be Alphanumerica" + Environment.NewLine);
            }

            if (!Regex.IsMatch(password, "[<>,@!#$%^&*()_+\\[\\]{}?:;|'\\./~`=]"))
            {
                sb.Append("Password should contain special character");
            }
            return sb.ToString();
        }



        private string CreateJwtToken(User user)
        {
            var jwtTokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes("veryverySecretKey12345678901234567890");
            var identity = new ClaimsIdentity(new Claim[]
            {
                   new Claim(ClaimTypes.Role, user.Role),
                   new Claim(ClaimTypes.Name, $"{user.Username}")
            });

            var credentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = identity,
                Expires = DateTime.Now.AddSeconds(10),
                SigningCredentials = credentials,
            };
            var token = jwtTokenHandler.CreateToken(tokenDescriptor);
            return jwtTokenHandler.WriteToken(token);

        }



        private string CreateRefreshToken()
        {
            var tokenBytes = RandomNumberGenerator.GetBytes(64);
            var refreshToken = Convert.ToBase64String(tokenBytes);

            var tokenInUser = _authContext.Users
                .Any(a => a.RefreshToken == refreshToken);

            if (tokenInUser)
            {
                return CreateRefreshToken();
            }

            return refreshToken;
        }



        private ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
        {
            var key = Encoding.ASCII.GetBytes("veryverySecretKey12345678901234567890");
            var tokenValidationParameter = new TokenValidationParameters
            {
                ValidateAudience = false,
                ValidateIssuer = false,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateLifetime = false
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            SecurityToken securityToken;

            // Attempt to validate the token
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameter, out securityToken);

            // Try to cast the validated token to JwtSecurityToken
            var jwtSecurityToken = securityToken as JwtSecurityToken;

            // If the token is null or its algorithm doesn't match the expected algorithm (HmacSha256Signature),
            // throw a SecurityTokenException indicating an invalid token
            if (jwtSecurityToken == null || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                throw new SecurityTokenException("This is an invalid token");
            }

            // If the token passed validation, return the principal (user identity) extracted from the token
            return principal;

        }

        [Authorize]
        [HttpGet]
        public async Task<ActionResult<User>> GetAllUser()
        {
            return Ok(await _authContext.Users.ToListAsync());
        }


        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh(TokenApiDto tokenApiDto)
        {
            if (tokenApiDto is null)
            {
                return BadRequest("Invalid Client Request");
            }

            string accessToken = tokenApiDto.AccessToken;
            string refreshToken = tokenApiDto.RefreshToken;
            var principal = GetPrincipalFromExpiredToken(accessToken);
            var username = principal.Identity.Name;
            var user = await _authContext.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user is null || user.RefreshToken != refreshToken || user.RefreshTokenExpireTime <= DateTime.Now)
            {
                return BadRequest("Invalid Request");
            }

            var newAccessToken = CreateJwtToken(user);
            var newRefreshToken = CreateRefreshToken();
            user.RefreshToken = newRefreshToken;
            await _authContext.SaveChangesAsync();
            return Ok(new TokenApiDto
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken
            });
        }



        [HttpPost("forgetpassword")]
        public async Task<IActionResult> SendEmail([FromBody] User userObj)
        {
            var email = userObj.Email;
                var user = await _authContext.Users.FirstOrDefaultAsync(a => a.Email == email);
                if(user is null)
                {
                    return NotFound(new
                    {
                        StatusCode = 404,
                        Message = "email doesn't exists"
                    });
                }
            // Generate a 6-digit OTP
            string otp = GenerateOTP();

            string from = _configration["EmailSettings:From"];
            var emailModel = new EmailModel(email, "Reset Password!!", EmailBody.EmailStringBody(email, otp));
            _emailService.SendEmail(emailModel);
            _authContext.Entry(user).State = EntityState.Modified;
            await _authContext.SaveChangesAsync();
            return Ok(new
            {
                statusCode = 200,
                Message = "Email sent!",
                OTP = otp

            });
        }


        // Method to generate a 6-digit OTP
        private string GenerateOTP()
        {
            Random rnd = new Random();
            int otpNumber = rnd.Next(100000, 999999); // Generates a random number between 100000 and 999999
            return otpNumber.ToString();
        }



        [HttpPost("newPassword")]
        public async Task<IActionResult> ConfirmPassword([FromBody] NewPasswordDto newPasswordDto)
        {
            // Check if the provided email exists in the database
            var user = await _authContext.Users.FirstOrDefaultAsync(u => u.Email == newPasswordDto.Email);
            if (user == null)
            {
                return NotFound(new
                {
                    StatusCode = 404,
                    Message = "Email not found"
                });
            }

            // Update the user's password with the new password provided
            user.Password = PasswordHasher.HashPassword(newPasswordDto.Password);

            // Save changes to the database
            await _authContext.SaveChangesAsync();

            return Ok(new
            {
                StatusCode = 200,
                Message = "Password reset successful"
            });
        }




    }
}
