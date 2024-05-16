using Backend.Models;

namespace Backend.Repository.IRepository
{
    public interface IUserRepository
    {
        Task<User> GetUserByEmailAsync(string email);
        Task<User> UpdateUserAsync(User user);

        Task<bool> CheckUserNameExistAsync(string username);
        Task<bool> CheckEmailExistAsync(string email);
        Task<User> CreateUserAsync(User user);



        Task<User> GetUserByUsernameAsync(string username);



    }
}
