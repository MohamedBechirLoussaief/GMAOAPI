using System.ComponentModel.DataAnnotations;

namespace GMAOAPI.DTOs.CreateDTOs
{
    public class PieceDetacheeCreateDto
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
        [Required]
        public double Cout { get; set; }

    }
}
