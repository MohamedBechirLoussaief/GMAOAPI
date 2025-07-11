using GMAOAPI.Models.Enumerations;
using System.ComponentModel.DataAnnotations;

namespace GMAOAPI.DTOs.AuthDTO
{
    public class RegisterDto
    {
        [Required]
        public string Username {  get; set; }

        [Required]
        public string Nom { get; set; }

        [Required]
        public string Prenom { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }

        [Required]
        public RoleUtilisateur Role { get; set; }
        public SpecialiteTechnicien? Specialite { get; set; }

    }
}
