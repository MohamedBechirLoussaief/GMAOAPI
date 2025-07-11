using GMAOAPI.Models.Entities;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace GMAOAPI.Services.Token
{
    public class TokenService : ITokenService
    {
        private readonly IConfiguration _configuration;
        private readonly SymmetricSecurityKey _key;

        public TokenService(IConfiguration configuration)
        {
            _configuration = configuration;
            _key = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(_configuration["JWT:SigninKey"]));
        }
        public string CreateToken(Utilisateur utilisateur)
        {
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Email, utilisateur.Email),
                new Claim(JwtRegisteredClaimNames.GivenName, utilisateur.UserName),
                new Claim(ClaimTypes.Role, utilisateur.RoleUtilisateur.ToString()),
                new Claim(JwtRegisteredClaimNames.Sub, utilisateur.Id),
            };


            var creds = new SigningCredentials(_key, SecurityAlgorithms.HmacSha512Signature);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.Now.AddDays(7),
                SigningCredentials = creds,
                Issuer = _configuration["JWT:Issuer"],
                Audience = _configuration["JWT:Audience"]
            };
            var tokenHandler = new JwtSecurityTokenHandler();

            var token = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(token);
        }
    }
}
