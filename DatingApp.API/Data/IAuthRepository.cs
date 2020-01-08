namespace DatingApp.API.Data
{
    using System.Threading.Tasks;
    using DatingApp.API.Models;

    public interface IAuthRepository
    {
        Task<User> RegisterAsync(User user, string password);

        Task<User> LoginAsync(string username, string password);

        Task<bool> UserExistsAsync(string username);
    }
}