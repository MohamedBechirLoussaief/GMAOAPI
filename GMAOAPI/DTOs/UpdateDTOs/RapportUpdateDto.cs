using GMAOAPI.Models.Enumerations;
using System.ComponentModel.DataAnnotations;

namespace GMAOAPI.DTOs.UpdateDTOs
{
    public class RapportUpdateDto
    {
        [Required]
        public string Titre { get; set; }

        [Required]
        public string Contenu { get; set; }
        [Required]
        public ResultatIntervention Resultat { get; set; }

    }
}
