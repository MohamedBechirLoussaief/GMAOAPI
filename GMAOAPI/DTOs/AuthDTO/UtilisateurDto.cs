using GMAOAPI.Models.Enumerations;

namespace GMAOAPI.DTOs.AuthDTO
{
    public class UtilisateurDto
    {
        public string Id { get; set; }
        public string UserName { get; set; }

        public string Email { get; set; }
        public string Nom { get; set; }
        public string Prenom { get; set; }
        public string Role { get; set; }
        public SpecialiteTechnicien? Specialite { get; set; }
        public bool IsArchived { get; set; }
        public string Token { get; set; } = "";

    }
}
