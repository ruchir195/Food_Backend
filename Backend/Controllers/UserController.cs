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

using Backend.Dto;
using Backend.Backend.Service.IUtilityService;
using Backend.Backend.Repository.IRepository;

namespace Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly AppDbContext _authContext;
        private readonly IConfiguration _configration;
        private readonly IEmailService _emailService;

      
        private readonly IRepository<User> _userRepository;

        public UserController(AppDbContext appDbContext, IRepository<User> userRepository, IConfiguration configration, IEmailService emailService)
        {

            _authContext = appDbContext;
            _userRepository = userRepository;
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

            var user = await _userRepository.GetUserByEmailAsync(userObj.Email);

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

            await _userRepository.UpdateUserAsync(user);

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

               

                if (await _userRepository.CheckEmailExistAsync(userObj.Email))
                {
                    return BadRequest(new { Message = "Email already exists!" });
                }

                var pass = CheckPasswordStength(userObj.Password);
                if (!string.IsNullOrEmpty(pass))
                {
                    return BadRequest(new { Message = pass.ToString() });
                }

                await _userRepository.CreateUserAsync(userObj);

                return Ok(new { message = "User Registered" });
            }
            catch (Exception e)
            {
                // Log the exception for debugging purposes
                Console.WriteLine($"An error occurred during user registration: {e.Message}");
                return StatusCode(500, new { message = "An error occurred during user registration" });
            }
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
                   new Claim(ClaimTypes.Name, $"{user.Id}"),
                   new Claim(ClaimTypes.Email, user.Email)
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
            var id = principal.Identity.Name;

            var user = await _userRepository.GetUserByUsernameAsync(id);

            if (user is null || user.RefreshToken != refreshToken || user.RefreshTokenExpireTime <= DateTime.Now)
            {
                return BadRequest("Invalid Request");
            }

            var newAccessToken = CreateJwtToken(user);
            var newRefreshToken = CreateRefreshToken();
            user.RefreshToken = newRefreshToken;

            await _userRepository.UpdateUserAsync(user);

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
            var user = await _userRepository.GetUserByEmailAsync(email);

            if (user is null)
            {
                return NotFound(new
                {
                    StatusCode = 404,
                    Message = "Email doesn't exist"
                });
            }

            // Generate a 6-digit OTP
            string otp = GenerateOTP();

            string from = _configration["EmailSettings:From"];
            var emailModel = new EmailModel(email, "Reset Password!!", EmailBody.EmailStringBody(email, otp));
            _emailService.SendEmail(emailModel);

            _authContext.Entry(user).State = EntityState.Modified;
            await _userRepository.UpdateUserAsync(user);

            return Ok(new
            {
                StatusCode = 200,
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
            var user = await _userRepository.GetUserByEmailAsync(newPasswordDto.Email);
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
            await _userRepository.UpdateUserAsync(user);

            return Ok(new
            {
                StatusCode = 200,
                Message = "Password reset successful"
            });
        }





        [Authorize]
        [HttpPost("changePassword")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto changePasswordDto)
        {
            // Check if the provided email exists in the database
            var user = await _userRepository.GetUserByEmailAsync(changePasswordDto.Email);
            if (user == null)
            {   
                return NotFound(new
                {
                    StatusCode = 404,
                    Message = "Email not found"
                });
            }

            if (!PasswordHasher.VerifyPassword(changePasswordDto.opassword, user.Password))
            {
                return BadRequest(new { Message = "Password is incorrect" });
            }

            // Update the user's password with the new password provided
            user.Password = PasswordHasher.HashPassword(changePasswordDto.Password);

            // Save changes to the database
            await _userRepository.UpdateUserAsync(user);


            // Create and save the notification
            var notification = new Notification
            {
                UserId = user.Id,
                Message = "Password updated successfully.",
                TimeStamp = DateTime.UtcNow
            };
            await _userRepository.AddNotificationAsync(notification);


            return Ok(new
            {
                StatusCode = 200,
                Message = "Password change successfully"
            });
        }



        [HttpGet("{email}")]
        public async Task<IActionResult> GetUserByUniqueName(string email)
        {
            var user = await _userRepository.GetUserByEmailAsync(email);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }
            return Ok(user);
        }
    }
}
