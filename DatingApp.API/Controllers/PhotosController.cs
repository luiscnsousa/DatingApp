namespace DatingApp.API.Controllers
{
    using System;
    using System.Linq;
    using System.Security.Claims;
    using System.Threading.Tasks;
    using AutoMapper;
    using CloudinaryDotNet;
    using CloudinaryDotNet.Actions;
    using DatingApp.API.Data;
    using DatingApp.API.Dtos;
    using DatingApp.API.Helpers;
    using DatingApp.API.Models;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Options;

    [ApiController]
    [Route("api/users/{userId}/photos")]
    public class PhotosController : ControllerBase
    {
        private readonly IDatingRepository repo;
        private readonly IMapper mapper;
        private readonly IOptions<CloudinarySettings> cloudinaryConfig;
        private Cloudinary cloudinary;

        public PhotosController(
            IDatingRepository repo,
            IMapper mapper,
            IOptions<CloudinarySettings> cloudinaryConfig)
        {
            this.repo = repo;
            this.mapper = mapper;
            this.cloudinaryConfig = cloudinaryConfig;
            
            var account = new Account(
                this.cloudinaryConfig.Value.CloudName,
                this.cloudinaryConfig.Value.ApiKey,
                this.cloudinaryConfig.Value.ApiSecret);
            
            this.cloudinary = new Cloudinary(account);
        }

        [HttpGet("{id}", Name = "GetPhoto")]
        public async Task<IActionResult> GetPhoto(int id)
        {
            var photoFromRepo = await this.repo.GetPhotoAsync(id);

            var photo = this.mapper.Map<PhotoForReturnDto>(photoFromRepo);

            return this.Ok(photo);
        }
        
        [HttpPost]
        public async Task<IActionResult> AddPhotoForUser(int userId, [FromForm] PhotoForCreationDto photoForCreationDto)
        {
            if (userId != int.Parse(this.User.FindFirst(ClaimTypes.NameIdentifier).Value))
            {
                return this.Unauthorized();
            }
            
            var userFromRepo = await this.repo.GetUserAsync(userId);

            var file = photoForCreationDto.File;
            
            var uploadResult = new ImageUploadResult();

            if (file.Length > 0)
            {
                await using var stream = file.OpenReadStream();
                var uploadParams = new ImageUploadParams
                {
                    File = new FileDescription(file.Name, stream),
                    Transformation = new Transformation().Width(500).Height(500).Crop("fill").Gravity("face")
                };

                uploadResult = await this.cloudinary.UploadAsync(uploadParams);
            }

            photoForCreationDto.Url = uploadResult.Url.ToString();
            photoForCreationDto.PublicId = uploadResult.PublicId;

            var photo = this.mapper.Map<Photo>(photoForCreationDto);

            if (!userFromRepo.Photos.Any(u => u.IsMain))
            {
                photo.IsMain = true;
            }
            
            userFromRepo.Photos.Add(photo);
            
            if (await this.repo.SaveAllAsync())
            {
                var photoToReturn = this.mapper.Map<PhotoForReturnDto>(photo);
                return this.CreatedAtRoute(nameof(this.GetPhoto), new { userId = userId, id = photo.Id }, photoToReturn);
            }

            return this.BadRequest("Could not add the photo");
        }

        [HttpPost("{id}/setMain")]
        public async Task<IActionResult> SetMain(int userId, int id)
        {
            if (userId != int.Parse(this.User.FindFirst(ClaimTypes.NameIdentifier).Value))
            {
                return this.Unauthorized();
            }
            
            var user = await this.repo.GetUserAsync(userId);

            if (!user.Photos.Any(p => p.Id == id))
            {
                return this.Unauthorized();
            }

            var photoFromRepo = await this.repo.GetPhotoAsync(id);

            if (photoFromRepo.IsMain)
            {
                return this.BadRequest("This is already the main photo");
            }

            var currentMainPhoto = await this.repo.GetMainPhotoForUserAsync(userId);
            currentMainPhoto.IsMain = false;

            photoFromRepo.IsMain = true;
            
            if (await this.repo.SaveAllAsync())
            {
                return this.NoContent();
            }

            return this.BadRequest("Could not set photo to main");
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePhoto(int userId, int id)
        {
            if (userId != int.Parse(this.User.FindFirst(ClaimTypes.NameIdentifier).Value))
            {
                return this.Unauthorized();
            }
            
            var user = await this.repo.GetUserAsync(userId);

            if (!user.Photos.Any(p => p.Id == id))
            {
                return this.Unauthorized();
            }

            var photoFromRepo = await this.repo.GetPhotoAsync(id);

            if (photoFromRepo.IsMain)
            {
                return this.BadRequest("You cannot delete your main photo");
            }

            if (photoFromRepo.PublicId != null)
            {
                var deleteParams = new DeletionParams(photoFromRepo.PublicId);

                var response = await this.cloudinary.DestroyAsync(deleteParams);

                if (response.Result.Equals("ok", StringComparison.InvariantCultureIgnoreCase))
                {
                    this.repo.Delete(photoFromRepo);
                }
            }

            if (photoFromRepo.PublicId != null)
            {
                this.repo.Delete(photoFromRepo);
            }

            if (await this.repo.SaveAllAsync())
            {
                return this.Ok();
            }

            return this.BadRequest("Failed to delete the photo");
        }
    }
}