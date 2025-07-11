using System.ComponentModel.DataAnnotations;

namespace GMAOAPI.DTOs.CreateDTOs
{
    public class FournisseurCreateDto
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Nom { get; set; }

        [Required]
        public string Adresse { get; set; }

        [Required]
        public string Contact { get; set; }
    }
}
