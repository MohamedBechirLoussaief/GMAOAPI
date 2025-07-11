using System.ComponentModel.DataAnnotations;

namespace GMAOAPI.Models.Entities
{
    public class Fournisseur
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Nom { get; set; }

        [Required]
        public string Adresse { get; set; }

        [Required]
        public string Contact { get; set; }
        public bool IsArchived { get; set; } = false;

        public ICollection<PieceDetachee>? PiecesDetachees { get; set; }
    }
}
