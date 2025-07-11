using GMAOAPI.Models.Enumerations;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace GMAOAPI.DTOs.CreateDTOs
{
    public class InterventionCreateDto
    {
        [Required]
        public string Description { get; set; }

        [Required]
        public int EquipementId { get; set; }

        [Required]
        public TypeIntervention Type { get; set; }

        public List<string>? TechnicienIds { get; set; } = new List<string>();

        public List<InterventionPieceDetacheeCreateDto>? PiecesDetachees { get; set; }
            = new List<InterventionPieceDetacheeCreateDto>();
    }
}
