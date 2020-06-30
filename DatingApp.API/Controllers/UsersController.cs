namespace DatingApp.API.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Security.Claims;
    using System.Threading.Tasks;
    using AutoMapper;
    using DatingApp.API.Data;
    using DatingApp.API.Dtos;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;

    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
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
        public async Task<IActionResult> GetUsers()
        {
            var users = await this.repo.GetUsersAsync();

            var listUsers = this.mapper.Map<IEnumerable<ListUserDto>>(users);

            return this.Ok(listUsers);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUser(int id)
        {
            var user = await this.repo.GetUserAsync(id);

            var detailedUser = this.mapper.Map<DetailedUserDto>(user);
            
            return this.Ok(detailedUser);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(int id, UserForUpdateDto userForUpdateDto)
        {
            if (id != int.Parse(this.User.FindFirst(ClaimTypes.NameIdentifier).Value))
            {
                return this.Unauthorized();
            }
            
            var userFromRepo = await this.repo.GetUserAsync(id);
            
            this.mapper.Map(userForUpdateDto, userFromRepo);

            if (await this.repo.SaveAllAsync())
            {
                return this.NoContent();
            }

            throw new Exception($"Updating user {id} failed on save");
        }
    }
}