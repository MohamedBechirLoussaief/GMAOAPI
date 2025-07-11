using GMAOAPI.Models.Enumerations;
using System.ComponentModel.DataAnnotations;

namespace GMAOAPI.DTOs.UpdateDTOs
{
    public class EquipementUpdateDto
    {
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

