namespace DatingApp.API.Controllers
{
    using System.Linq;
    using System.Threading.Tasks;
    using CloudinaryDotNet;
    using CloudinaryDotNet.Actions;
    using DatingApp.API.Data;
    using DatingApp.API.Dtos;
    using DatingApp.API.Helpers;
    using DatingApp.API.Models;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Options;

    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly DataContext context;
        private readonly UserManager<User> userManager;
        private readonly IOptions<CloudinarySettings> cloudinaryConfig;
        private readonly Cloudinary cloudinary;

        public AdminController(
            DataContext context,
            UserManager<User> userManager,
            IOptions<CloudinarySettings> cloudinaryConfig)
        {
            this.context = context;
            this.userManager = userManager;
            this.cloudinaryConfig = cloudinaryConfig;
            
            var acc = new Account(
                this.cloudinaryConfig.Value.CloudName,
                this.cloudinaryConfig.Value.ApiKey,
                this.cloudinaryConfig.Value.ApiSecret);
            
            this.cloudinary = new Cloudinary(acc);
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
        public async Task<IActionResult> GetPhotosForModeration()
        {
            var photos = await this.context.Photos
                .Include(p => p.User)
                .IgnoreQueryFilters()
                .Where(p => !p.IsApproved)
                .Select(p => new
                {
                    p.Id,
                    p.User.UserName,
                    p.Url,
                    p.IsApproved
                }).ToListAsync();
            
            return this.Ok(photos);
        }
        
        [Authorize(Policy = "ModeratePhotoRole")]
        [HttpPost("approvePhoto/{photoId}")]
        public async Task<IActionResult> ApprovePhoto(int photoId)
        {
            var photo = await this.context.Photos
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(p => p.Id == photoId);

            photo.IsApproved = true;

            await this.context.SaveChangesAsync();
            
            return this.Ok();
        }
        
        [Authorize(Policy = "ModeratePhotoRole")]
        [HttpPost("rejectPhoto/{photoId}")]
        public async Task<IActionResult> RejectPhoto(int photoId)
        {
            var photo = await this.context.Photos
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(p => p.Id == photoId);

            if (photo.IsMain)
            {
                return this.BadRequest("You cannot reject the main photo");
            }

            var cloudinaryPhoto = photo.PublicId != null;
            if (!cloudinaryPhoto)
            {
                this.context.Photos.Remove(photo);
            }
            else
            {
                var deleteParams = new DeletionParams(photo.PublicId);

                var result = await this.cloudinary.DestroyAsync(deleteParams);

                if (result.Result == "ok")
                {
                    this.context.Photos.Remove(photo);
                }
            }

            await this.context.SaveChangesAsync();
            
            return this.Ok();
        }
    }
}