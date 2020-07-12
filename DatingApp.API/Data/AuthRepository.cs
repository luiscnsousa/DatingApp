namespace DatingApp.API.Data
{
    using System.Security.Cryptography;
    using System.Threading.Tasks;
    using DatingApp.API.Models;
    using Microsoft.EntityFrameworkCore;

    public class AuthRepository : IAuthRepository
    {
        private readonly DataContext context;

        public AuthRepository(DataContext context)
        {
            this.context = context;
        }
        
        public async Task<User> RegisterAsync(User user, string password)
        {
            byte[] passwordHash, passwordSalt;

            this.CreatePasswordHash(password, out passwordHash, out passwordSalt);
            user.PasswordHash = passwordHash;
            user.PasswordSalt = passwordSalt;

            await this.context.Users.AddAsync(user);
            await this.context.SaveChangesAsync();

            return user;
        }

        public async Task<User> LoginAsync(string username, string password)
        {
            var user = await this.context.Users
                .Include(u => u.Photos)
                .FirstOrDefaultAsync(x => x.Username == username);
            if (user == null)
            {
                return null;
            }

            if (!this.VerifyPasswordHash(password, user.PasswordHash, user.PasswordSalt))
            {
                return null;
            }

            return user;
        }

        public Task<bool> UserExistsAsync(string username)
        {
            return this.context.Users.AnyAsync(x => x.Username == username);
        }
        
        private void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
        {
            using (var hmac = new HMACSHA512())
            {
                passwordSalt = hmac.Key;
                passwordHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
            }
        }
        
        private bool VerifyPasswordHash(string password, byte[] passwordHash, byte[] passwordSalt)
        {
            using (var hmac = new HMACSHA512(passwordSalt))
            {
                var computedHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
                if (passwordHash == null || passwordHash.Length < computedHash.Length)
                {
                    return false;
                }
                
                for (var i = 0; i < computedHash.Length; i++)
                {
                    if (computedHash[i] != passwordHash[i])
                    {
                        return false;
                    }
                }

                return true;
            }
        }
    }
}