using GMAOAPI.DTOs.AuthDTO;
using GMAOAPI.Models.Entities;

namespace GMAOAPI.DTOs.ReadDTOs
{
    public class InterventionTechnicienDto
    {
        public int InterventionId { get; set; }

        public UtilisateurDto TechnicienDto { get; set; }


    }
}
