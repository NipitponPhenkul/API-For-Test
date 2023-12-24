using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
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
            try {
                using NpgsqlConnection conn = new Connect().GetConnection();
                using NpgsqlCommand cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    SELECT id FROM tb_user 
                    WHERE usr = @usr 
                    AND pwd = @pwd
                ";
                cmd.Parameters.AddWithValue("usr", userModel.Usr!);
                cmd.Parameters.AddWithValue("pwd", userModel.Pwd!);

                using NpgsqlDataReader reader = cmd.ExecuteReader();
                if (reader.Read()) {
                    userModel.Id = Convert.ToInt32(reader["id"]);
                    string token = GenerateToken(userModel);

                    return Ok(new { token = token, message = "success" });
                }

                return Unauthorized();
            } catch (Exception ex) {
                return StatusCode(StatusCodes.Status500InternalServerError, new {
                    message = ex.Message
                });
            }
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
                            new Claim(JwtRegisteredClaimNames.Sub, userModel.Id.ToString()),
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

        [HttpPost]
        [Route("[action]")]
        [Authorize]
        public IActionResult GetInfo() {
            try {
                int userId = GetUserIdFromAuth(HttpContext);
                if (userId > 0) {
                    using NpgsqlConnection conn = new Connect().GetConnection();
                    using NpgsqlCommand cmd = conn.CreateCommand();
                    cmd.CommandText = "SELECT name, level, usr FROM tb_user WHERE id = @id";
                    cmd.Parameters.AddWithValue("id", userId);

                    using NpgsqlDataReader reader = cmd.ExecuteReader();
                    if (reader.Read()) {
                        return Ok(new {
                            name = reader["name"].ToString(),
                            level = reader["level"].ToString(),
                            usr = reader["usr"].ToString()
                        });
                    }
                }
                return Unauthorized();
            } catch (Exception ex) {
                return StatusCode(StatusCodes.Status500InternalServerError, new {
                    message = ex.Message
                });
            }
        }

        private int GetUserIdFromAuth(HttpContext context) {
            context.Request.Headers.TryGetValue("Authorization", out var token);
            token = token.ToString().Replace("Bearer ", "");
            Dictionary<string, string> dic = GetTokenInfo(token!);
            return Convert.ToInt32(dic["sub"]);
        }

        private Dictionary<string, string> GetTokenInfo(string token) {
            Dictionary<string, string> tokenInfo = new Dictionary<string, string>();
            JwtSecurityTokenHandler handler = new JwtSecurityTokenHandler();
            JwtSecurityToken jwtSecurityToken = handler.ReadJwtToken(token);
            List<Claim> claims = jwtSecurityToken.Claims.ToList();

            foreach (Claim claim in claims) {
                tokenInfo.Add(claim.Type, claim.Value);
            }

            return tokenInfo;
        }
    }
}
