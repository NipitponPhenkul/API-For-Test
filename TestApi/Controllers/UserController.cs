using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using TestApi.Models;

namespace TestApi.Controllers
{
    [ApiController]
    [Route("/api/[controller]")]
    public class UserController : ControllerBase
    {
        [HttpPost]
        [Route("[action]")]
        public IActionResult Login(UserModel userModel) {
            return Ok(userModel);
        }


        private string GenerateToken(UserModel userModel)
        {
            try
            {
                    var MyConfig = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build(); // อ่านค่าไฟล์ appsettings.json ไปใช้
                    var issuer = MyConfig.GetValue<string>("Jwt:Issuer");
                    var audience = MyConfig.GetValue<string>("Jwt:Audience");
                    var key = Encoding.ASCII.GetBytes(MyConfig.GetValue<string>("Jwt:Key")!);
                    var tokenDescripttor = new SecurityTokenDescriptor
                    {
                        Subject = new ClaimsIdentity(new[] {
                            new Claim("Id", Guid.NewGuid().ToString()),
                            new Claim(JwtRegisteredClaimNames.Sub, userModel.Usr!),
                            new Claim(JwtRegisteredClaimNames.Email, "user@mail.com"),
                            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
                        }),
                        Expires = DateTime.UtcNow.AddDays(30),
                        Issuer = issuer,
                        Audience = audience,
                        SigningCredentials = new SigningCredentials(
                            new SymmetricSecurityKey(key),
                            SecurityAlgorithms.HmacSha512Signature
                        )
                    };
                    var tokenHandler = new JwtSecurityTokenHandler();
                    var token = tokenHandler.CreateToken(tokenDescripttor);
                    return tokenHandler.WriteToken(token);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
    }
}
