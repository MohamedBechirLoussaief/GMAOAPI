using GMAOAPI.Models.Enumerations;
using System.ComponentModel.DataAnnotations;

namespace GMAOAPI.DTOs.UpdateDTOs
{
    public class PlanificationUpdateDto
    {


        [Required]
        public DateTime DateDebut { get; set; }

        [Required]
        public DateTime DateFin { get; set; }

        [Required]
        public FrequencePlanification Frequence { get; set; }

    }
}
