using System.ComponentModel.DataAnnotations;

namespace GMAOAPI.DTOs.CreateDTOs
{
    public class InterventionPieceDetacheeCreateDto
    {
        public int? InterventionId { get; set; }
        [Required]

        public int PieceDetacheeId { get; set; }

        [Required]
        public int Quantite { get; set; }
    }
}
