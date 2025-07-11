using System.ComponentModel.DataAnnotations;
using GMAOAPI.Models.Enumerations;

namespace GMAOAPI.DTOs.UpdateDTOs
{
    public class UpdateUserDto
    {
        [Required]
        public string Id { get; set; }
        public string Nom { get; set; }
        public string Prenom { get; set; }

        public string? Email { get; set; }
        public string? UserName { get; set; }

        public SpecialiteTechnicien? Specialite { get; set; }
    }
}
