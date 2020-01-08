namespace DatingApp.API.Controllers
{
    using System;
    using System.IdentityModel.Tokens.Jwt;
    using System.Security.Claims;
    using System.Text;
    using System.Threading.Tasks;
    using DatingApp.API.Data;
    using DatingApp.API.Dtos;
    using DatingApp.API.Models;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Configuration;
    using Microsoft.IdentityModel.Tokens;

    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthRepository authRepository;
        private readonly IConfiguration configuration;

        public AuthController(IAuthRepository authRepository, IConfiguration configuration)
        {
            this.authRepository = authRepository;
            this.configuration = configuration;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterUserDto registerUser)//string username, string password)
        {
            registerUser.Username = registerUser.Username.ToLower();
            if (await this.authRepository.UserExistsAsync(registerUser.Username))
            {
                return this.BadRequest("Username already taken");
            }

            var userToCreate = new User
            {
                Username = registerUser.Username
            };

            var createdUser = await this.authRepository.RegisterAsync(userToCreate, registerUser.Password);

            // TODO: created at route
            return this.StatusCode(201);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginUserDto loginUser)
        {
            var user = await this.authRepository.LoginAsync(loginUser.Username.ToLower(), loginUser.Password);
            if (user == null)
            {
                return this.Unauthorized();
            }

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username)
            };

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(
                    this.configuration.GetSection("AppSettings:TokenKey").Value));
            
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha512);
            
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.Now.AddDays(1),
                SigningCredentials = credentials
            };
            
            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);

            return this.Ok(new
            {
                token = tokenHandler.WriteToken(token)
            });
        }
    }
}