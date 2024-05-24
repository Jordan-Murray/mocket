using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Mocket.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using User = Mocket.Models.User;


namespace Mocket.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthenticationController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly ICosmosDbService _cosmosDbService;

        public AuthenticationController(IConfiguration config, ICosmosDbService cosmosDbService)
        {
            _config = config;
            _cosmosDbService = cosmosDbService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] User login)
        {
            var user = await _cosmosDbService.GetUserAsync(login.Username);
            if (user == null || user.Password != login.Password)
            {
                return Unauthorized();
            }

            var tokenString = GenerateJWT(user);
            return Ok(new { token = tokenString });
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] User user)
        {
            user.Id = Guid.NewGuid().ToString();
            await _cosmosDbService.AddUserAsync(user);
            return Ok();
        }

        private string GenerateJWT(User user)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
            new Claim(JwtRegisteredClaimNames.Sub, user.Username),
            new Claim("UserId", user.Id)
        };

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddMinutes(30),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
