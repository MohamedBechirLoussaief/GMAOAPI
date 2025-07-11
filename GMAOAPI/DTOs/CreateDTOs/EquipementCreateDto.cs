using System.ComponentModel.DataAnnotations;

namespace GMAOAPI.DTOs.CreateDTOs
{
    public class EquipementCreateDto
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Nom { get; set; }

        [Required]
        public string Reference { get; set; }

        [Required]
        public string Localisation { get; set; }

        public DateTime? DateInstallation { get; set; }

        [Required]
        public string Type { get; set; }
    }
}
