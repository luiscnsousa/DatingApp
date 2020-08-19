namespace DatingApp.API.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Security.Claims;
    using System.Threading.Tasks;
    using AutoMapper;
    using DatingApp.API.Data;
    using DatingApp.API.Dtos;
    using DatingApp.API.Helpers;
    using DatingApp.API.Models;
    using Microsoft.AspNetCore.Mvc;

    [ApiController]
    [Route("api/[controller]")]
    [ServiceFilter(typeof(LogUserActivity))]
    public class UsersController : ControllerBase
    {
        private readonly IDatingRepository repo;
        private readonly IMapper mapper;

        public UsersController(
            IDatingRepository repo,
            IMapper mapper)
        {
            this.repo = repo;
            this.mapper = mapper;
        }

        [HttpGet]
        public async Task<IActionResult> GetUsers([FromQuery]UserParams userParams)
        {
            var currentUserId = int.Parse(this.User.FindFirst(ClaimTypes.NameIdentifier).Value);
            
            var userFromRepo = await this.repo.GetUserAsync(currentUserId, true);

            userParams.UserId = currentUserId;

            if (string.IsNullOrEmpty(userParams.Gender))
            {
                userParams.Gender = userFromRepo.Gender == "male"
                    ? "female"
                    : "male";
            }
            
            var users = await this.repo.GetUsersAsync(userParams);

            var listUsers = this.mapper.Map<IEnumerable<UserForListDto>>(users);

            this.Response.AddPagination(
                users.CurrentPage,
                users.PageSize,
                users.TotalCount,
                users.TotalPages);

            return this.Ok(listUsers);
        }

        [HttpGet("{id}", Name = "GetUser")]
        public async Task<IActionResult> GetUser(int id)
        {
            var isCurrentUser = int.Parse(this.User.FindFirst(ClaimTypes.NameIdentifier).Value) == id;
            
            var user = await this.repo.GetUserAsync(id, isCurrentUser);

            var detailedUser = this.mapper.Map<UserForDetailedDto>(user);
            
            return this.Ok(detailedUser);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(int id, UserForUpdateDto userForUpdateDto)
        {
            if (id != int.Parse(this.User.FindFirst(ClaimTypes.NameIdentifier).Value))
            {
                return this.Unauthorized();
            }
            
            var userFromRepo = await this.repo.GetUserAsync(id, true);
            
            this.mapper.Map(userForUpdateDto, userFromRepo);

            if (await this.repo.SaveAllAsync())
            {
                return this.NoContent();
            }

            throw new Exception($"Updating user {id} failed on save");
        }

        [HttpPost("{id}/like/{recipientId}")]
        public async Task<IActionResult> LikeUser(int id, int recipientId)
        {
            if (id != int.Parse(this.User.FindFirst(ClaimTypes.NameIdentifier).Value))
            {
                return this.Unauthorized();
            }

            var like = await this.repo.GetLikeAsync(id, recipientId);
            if (like != null)
            {
                return this.BadRequest("You already like this user");
            }

            if (await this.repo.GetUserAsync(recipientId, false) == null)
            {
                return this.NotFound();
            }

            like = new Like
            {
                LikerId = id,
                LikeeId = recipientId
            };
            
            this.repo.Add(like);
            
            if (await this.repo.SaveAllAsync())
            {
                return this.Ok();
            }

            return this.BadRequest("Failed to like user");
        }
    }
}