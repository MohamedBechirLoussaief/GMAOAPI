using GMAOAPI.Models.Entities;

namespace GMAOAPI.Services.Token
{
    public interface ITokenService
    {
        string CreateToken(Utilisateur utilisateur);
    }
}
