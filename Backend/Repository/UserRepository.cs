using Backend.Context;
using Backend.Helpers;
using Backend.Models;
using Backend.Repository.IRepository;
using Microsoft.EntityFrameworkCore;

namespace Backend.Repository
{
    public class UserRepository : IUserRepository
    {
        private readonly AppDbContext _authContext;

        public UserRepository(AppDbContext authContext)
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
    }
}
