namespace DatingApp.API.Controllers
{
    using System.Linq;
    using System.Threading.Tasks;
    using DatingApp.API.Data;
    using DatingApp.API.Dtos;
    using DatingApp.API.Models;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;

    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly DataContext context;
        private readonly UserManager<User> userManager;

        public AdminController(
            DataContext context,
            UserManager<User> userManager)
        {
            this.context = context;
            this.userManager = userManager;
        }
        
        [Authorize(Policy = "RequireAdminRole")]
        [HttpGet("usersWithRoles")]
        public async Task<IActionResult> GetUsersWithRoles()
        {
            var userList = await this.context.Users
                .OrderBy(user => user.UserName)
                .Select(user => new
                {
                    Id = user.Id,
                    UserName = user.UserName,
                    Roles = (
                        from userRole in user.UserRoles
                        join role in this.context.Roles
                            on userRole.RoleId equals role.Id
                        select role.Name).ToList()
                })
                .ToListAsync();
            
            return this.Ok(userList);
        }

        [Authorize(Policy = "RequireAdminRole")]
        [HttpPost("editRoles/{userName}")]
        public async Task<IActionResult> EditRoles(string userName, RoleEditDto roleEditDto)
        {
            var user = await this.userManager.FindByNameAsync(userName);

            var userRoles = await this.userManager.GetRolesAsync(user);

            var selectedRoles = roleEditDto.RoleNames;

            selectedRoles ??= new string[] { };

            var result = await this.userManager.AddToRolesAsync(user, selectedRoles.Except(userRoles));
            if (!result.Succeeded)
            {
                return this.BadRequest("Failed to add to roles");
            }

            result = await this.userManager.RemoveFromRolesAsync(user, userRoles.Except(selectedRoles));
            if (!result.Succeeded)
            {
                return this.BadRequest("Failed to remove from roles");
            }

            return this.Ok(await this.userManager.GetRolesAsync(user));
        }
        
        [Authorize(Policy = "ModeratePhotoRole")]
        [HttpGet("photosForModeration")]
        public IActionResult GetPhotosForModeration()
        {
            return this.Ok("Admins or moderators can see this");
        }
    }
}