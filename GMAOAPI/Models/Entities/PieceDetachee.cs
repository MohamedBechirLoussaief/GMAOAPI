using System.ComponentModel.DataAnnotations;

namespace GMAOAPI.Models.Entities
{
    public class PieceDetachee
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Nom { get; set; }

        [Required]
        public string Reference { get; set; }

        [Required]
        public int QuantiteStock { get; set; }

        [Required]
        public int FournisseurId { get; set; }

        public Fournisseur? Fournisseur { get; set; }


        [Required]
        public double Cout { get; set; }
        public bool IsArchived { get; set; } = false;

        public ICollection<InterventionPieceDetachee>? InterventionPieceDetachees { get; set; }
    }
}