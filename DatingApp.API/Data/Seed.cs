namespace DatingApp.API.Data
{
    using System.Collections.Generic;
    using System.Security.Cryptography;
    using DatingApp.API.Models;
    using Microsoft.EntityFrameworkCore.Internal;
    using Newtonsoft.Json;

    public class Seed
    {
        public static void SeedUsers(DataContext context)
        {
            if (!context.Users.Any())
            {
                var userData = System.IO.File.ReadAllText("Data/UserSeedData.json");
                var users = JsonConvert.DeserializeObject<List<User>>(userData);
                foreach (var user in users)
                {
                    var (passwordSalt, passwordHash) = CreatePasswordHash("password");
                    user.PasswordSalt = passwordSalt;
                    user.PasswordHash = passwordHash;
                    user.Username = user.Username.ToLower();
                    context.Users.Add(user);
                }

                context.SaveChanges();
            }
        }

        private static (byte[], byte[]) CreatePasswordHash(string password)
        {
            using (var hmac = new HMACSHA512())
            {
                return (hmac.Key, hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password)));
            }
        }
    }
}