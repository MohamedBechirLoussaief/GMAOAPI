using System.ComponentModel.DataAnnotations;

namespace GMAOAPI.DTOs.UpdateDTOs
{
    public class InterventionPieceDetacheeUpdateDto
    {
        [Required]
        public int Quantite { get; set; }
    }
}
