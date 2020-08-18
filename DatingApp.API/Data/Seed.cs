namespace DatingApp.API.Data
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using DatingApp.API.Models;
    using Microsoft.AspNetCore.Identity;
    using Newtonsoft.Json;

    public class Seed
    {
        public static async Task SeedUsersAsync(UserManager<User> userManager, RoleManager<Role> roleManager)
        {
            if (!userManager.Users.Any())
            {
                var userData = await File.ReadAllTextAsync("Data/UserSeedData.json");
                var users = JsonConvert.DeserializeObject<List<User>>(userData);

                var roles = new List<Role>
                {
                    new Role { Name = "Member" },
                    new Role { Name = "Admin" },
                    new Role { Name = "Moderator" },
                    new Role { Name = "VIP" }
                };

                foreach (var role in roles)
                {
                    await roleManager.CreateAsync(role);
                }
                
                foreach (var user in users)
                {
                    await userManager.CreateAsync(user, "password");
                    await userManager.AddToRoleAsync(user, "Member");
                }

                var adminUser = new User
                {
                    UserName = "Admin"
                };

                var result = await userManager.CreateAsync(adminUser, "password");

                if (result.Succeeded)
                {
                    var admin = await userManager.FindByNameAsync("Admin");
                    await userManager.AddToRolesAsync(admin, new[] { "Admin", "Moderator" });
                }
            }
        }
    }
}