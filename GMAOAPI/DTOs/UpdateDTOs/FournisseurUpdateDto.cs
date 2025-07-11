using System.ComponentModel.DataAnnotations;

namespace GMAOAPI.DTOs.UpdateDTOs
{
    public class FournisseurUpdateDto
    {
        [Required]
        public string Nom { get; set; }

        [Required]
        public string Adresse { get; set; }

        [Required]
        public string Contact { get; set; }


    }
}
