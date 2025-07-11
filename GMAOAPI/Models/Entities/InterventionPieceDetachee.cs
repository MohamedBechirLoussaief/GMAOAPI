using System.ComponentModel.DataAnnotations;

namespace GMAOAPI.Models.Entities
{
    public class InterventionPieceDetachee
    {
        [Required]

        public int InterventionId { get; set; }
        public Intervention? Intervention { get; set; }
        [Required]

        public int PieceDetacheeId { get; set; }
        public PieceDetachee? PieceDetachee { get; set; }

        [Required]
        public int Quantite { get; set; }
    }
}
