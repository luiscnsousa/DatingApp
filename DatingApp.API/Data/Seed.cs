namespace DatingApp.API.Data
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using DatingApp.API.Models;
    using Microsoft.AspNetCore.Identity;
    using Newtonsoft.Json;

    public class Seed
    {
        public static async Task SeedUsersAsync(UserManager<User> userManager)
        {
            if (!userManager.Users.Any())
            {
                var userData = System.IO.File.ReadAllText("Data/UserSeedData.json");
                var users = JsonConvert.DeserializeObject<List<User>>(userData);
                
                foreach (var user in users)
                {
                    await userManager.CreateAsync(user, "password");
                }
            }
        }
    }
}